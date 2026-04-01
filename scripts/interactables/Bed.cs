using Godot;

/// <summary>
/// Bed interactable — walk near, press [E] to sleep and advance to the next day.
/// Shows a "Sleep?" confirmation prompt.
/// Attach to any Node2D in a scene (farmhouse interior, etc.).
/// </summary>
public partial class Bed : Node2D
{
	private bool   _playerNear;
	private bool   _confirming;

	// Floating UI
	private Node2D  _anchor;
	private Control _uiRoot;
	private Label   _promptLabel;
	private Label   _confirmLabel;

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		BuildPrompt();

		var area   = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape  = new CollisionShape2D();
		var circle = new CircleShape2D { Radius = 24f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);

		area.BodyEntered += OnEnter;
		area.BodyExited  += OnExit;
	}

	public override void _Process(double delta)
	{
		_uiRoot.GlobalPosition = GlobalPosition + new Vector2(-70, -44);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_playerNear) return;

		if (@event.IsActionPressed("interact"))
		{
			if (!_confirming)
			{
				// First press: show confirmation
				_confirming = true;
				_promptLabel.Visible  = false;
				_confirmLabel.Visible = true;
			}
			else
			{
				// Second press: sleep!
				Sleep();
			}
			GetViewport().SetInputAsHandled();
			return;
		}

		// Cancel confirm with Esc
		if (_confirming && @event.IsActionPressed("ui_cancel"))
		{
			CancelConfirm();
			GetViewport().SetInputAsHandled();
		}
	}

	// ── Sleep logic ────────────────────────────────────────────────────────
	private void Sleep()
	{
		_confirming = false;
		CancelConfirm();

		// Disable player input during sleep
		var player = GetTree().GetFirstNodeInGroup("player") as Player;

		// Create a fade overlay if SceneTransition isn't available
		var overlay = new ColorRect();
		overlay.Color = new Color(0, 0, 0, 0);
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		overlay.ZIndex = 200;

		var fadeLayer = new CanvasLayer { Layer = 99 };
		fadeLayer.AddChild(overlay);
		GetTree().Root.AddChild(fadeLayer);

		var tween = GetTree().CreateTween();
		// Fade to black
		tween.TweenProperty(overlay, "color:a", 1.0f, 0.6f);
		// Hold black, advance day + heal
		tween.TweenCallback(Callable.From(() =>
		{
			DaySystem.Instance?.AdvanceDay();

			// Heal player on sleep (Stardew-style: full heal overnight)
			if (player != null)
				player.Heal(player.MaxHealth);
		})).SetDelay(0.3f);
		// Show notification while black
		tween.TweenCallback(Callable.From(() =>
		{
			NotificationManager.Instance?.Show("Good morning!", new Color(0.95f, 0.85f, 0.45f));
		})).SetDelay(0.4f);
		// Fade back in
		tween.TweenProperty(overlay, "color:a", 0.0f, 0.6f);
		// Cleanup
		tween.TweenCallback(Callable.From(() => fadeLayer.QueueFree()));
	}

	private void CancelConfirm()
	{
		_confirming           = false;
		_promptLabel.Visible  = _playerNear;
		_confirmLabel.Visible = false;
	}

	// ── Area callbacks ─────────────────────────────────────────────────────
	private void OnEnter(Node2D body)
	{
		if (!body.IsInGroup("player")) return;
		_playerNear = true;
		if (!_confirming) _promptLabel.Visible = true;
	}

	private void OnExit(Node2D body)
	{
		if (!body.IsInGroup("player")) return;
		_playerNear = false;
		_promptLabel.Visible  = false;
		_confirmLabel.Visible = false;
		_confirming = false;
	}

	// ── Build helpers ──────────────────────────────────────────────────────
	private void BuildPrompt()
	{
		_anchor = new Node2D { TopLevel = true };
		AddChild(_anchor);

		_uiRoot = new Control();
		_anchor.AddChild(_uiRoot);

		_promptLabel = MakeLabel("[E]  Sleep", Colors.White, 12);
		_promptLabel.Visible = false;
		_uiRoot.AddChild(_promptLabel);

		_confirmLabel = MakeLabel("[E] Confirm  •  [Esc] Cancel", new Color(0.9f, 0.82f, 0.45f), 11);
		_confirmLabel.Position = new Vector2(0, 16);
		_confirmLabel.Visible  = false;
		_uiRoot.AddChild(_confirmLabel);
	}

	private static Label MakeLabel(string text, Color color, int size)
	{
		var lbl = new Label { Text = text };
		lbl.AddThemeColorOverride("font_color", color);
		lbl.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		lbl.AddThemeFontSizeOverride("font_size", size);
		lbl.AddThemeConstantOverride("shadow_offset_x", 1);
		lbl.AddThemeConstantOverride("shadow_offset_y", 1);
		return lbl;
	}
}
