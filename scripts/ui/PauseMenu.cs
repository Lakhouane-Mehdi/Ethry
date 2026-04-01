using Godot;

/// <summary>
/// Pause menu — shown when player presses Esc while no other menu is open.
/// Builds its entire UI in code (no .tscn needed — it's an autoload CanvasLayer).
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	public static PauseMenu Instance { get; private set; }

	private Control _screen;
	private bool    _open;

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance    = this;
		Layer       = 50;  // above game world, below inventory (layer 10 is inventory, 50 is this)
		ProcessMode = ProcessModeEnum.Always;
		BuildUI();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel")) return;

		if (_open)
		{
			Close();
		}
		else
		{
			// Only open if no other menu (inventory, shop, crafting) is pausing the tree
			// If the game is already paused by something else, don't hijack
			if (!GetTree().Paused)
				Open();
		}
		GetViewport().SetInputAsHandled();
	}

	// ── Open / Close ──────────────────────────────────────────────────────
	public void Open()
	{
		_open           = true;
		_screen.Visible = true;
		GetTree().Paused = true;
	}

	public void Close()
	{
		_open           = false;
		_screen.Visible = false;
		GetTree().Paused = false;
	}

	public bool IsOpen => _open;

	// ── Build UI ───────────────────────────────────────────────────────────
	private void BuildUI()
	{
		_screen = new Control();
		_screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screen.ProcessMode = ProcessModeEnum.Always;
		_screen.Visible = false;
		AddChild(_screen);

		// Dim background
		var dim = new ColorRect();
		dim.Color = new Color(0, 0, 0, 0.55f);
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screen.AddChild(dim);

		// Frosted panel
		var atlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(4, 49, 40, 46) };
		var style = new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 8, TextureMarginTop    = 8,
			TextureMarginRight  = 8, TextureMarginBottom = 10,
			ContentMarginLeft   = 30, ContentMarginTop   = 24,
			ContentMarginRight  = 30, ContentMarginBottom = 24,
		};
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", style);
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft   = -140; panel.OffsetRight  = 140;
		panel.OffsetTop    = -180; panel.OffsetBottom = 180;
		_screen.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		panel.AddChild(vbox);

		// Title
		var title = new Label { Text = "PAUSED" };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeColorOverride("font_color", new Color(0.75f, 0.52f, 0.18f));
		title.AddThemeFontSizeOverride("font_size", 20);
		vbox.AddChild(title);

		var sep = new HSeparator();
		sep.Modulate = new Color(0.55f, 0.38f, 0.18f, 0.5f);
		vbox.AddChild(sep);

		// Buttons
		AddMenuBtn(vbox, "Resume",    Colors.White,                   Close);
		AddMenuBtn(vbox, "Save Game", new Color(0.6f, 0.9f, 0.55f),  () => { SaveSystem.Save(); NotificationManager.Instance?.Show("Game saved!", new Color(0.5f, 0.9f, 0.5f)); });
		AddMenuBtn(vbox, "Quit to Desktop", new Color(0.9f, 0.4f, 0.35f), () => GetTree().Quit());

		// Day/Gold info footer
		var footer = new Label();
		footer.HorizontalAlignment = HorizontalAlignment.Center;
		footer.AddThemeColorOverride("font_color", new Color(0.5f, 0.38f, 0.2f, 0.8f));
		footer.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(footer);

		// Update footer each frame (deferred)
		footer.Text = $"{DaySystem.Instance?.DayString ?? "Day 1 · Spring · Year 1"}\n{PlayerData.Instance?.Gold ?? 0} g";
	}

	private void AddMenuBtn(VBoxContainer parent, string text, Color color, System.Action action)
	{
		var btn = new Button { Text = text };
		btn.AddThemeColorOverride("font_color", color);
		btn.AddThemeFontSizeOverride("font_size", 14);
		btn.FocusMode = Control.FocusModeEnum.None;
		btn.Pressed  += () => action();
		parent.AddChild(btn);
	}
}
