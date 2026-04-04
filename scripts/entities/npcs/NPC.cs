using Godot;
using FSM;

/// <summary>
/// Interactable NPC with a proper bottom-screen dialogue box (Stardew-style).
/// Refactored to use a StateMachine for modular logic.
/// </summary>
public partial class NPC : CharacterBody2D
{
	[Export] public string NpcName = "Villager";
	[Export] public NpcRole Role = NpcRole.Dialogue;
	[Export(PropertyHint.MultilineText)]
	public string[] DialogueLines = new[] { "Hello, traveller!" };

	[Export] public int[] ShopItems = System.Array.Empty<int>();

	private FSM.StateMachine _stateMachine;
	private bool _playerInRange;
	private int  _pageIndex;      // current line index within THIS session
	private int  _cycleSeed;      // which starting line in the array for next session

	// Floating prompt above NPC head
	private Control _uiRoot;
	private Label   _prompt;

	// Bottom-screen dialogue box
	private CanvasLayer   _dlgLayer;
	private Control       _dlgScreen;
	private Label         _dlgNameLabel;
	private Label         _dlgTextLabel;
	private Label         _dlgContinueHint;
	private Tween         _blinkTween;

	public bool PlayerInRange => _playerInRange;

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	public override void _Ready()
	{
		_stateMachine = GetNode<FSM.StateMachine>("StateMachine");

		// ── Floating prompt ──
		var anchor = new Node2D { TopLevel = true };
		AddChild(anchor);

		_uiRoot = new Control();
		anchor.AddChild(_uiRoot);

		_prompt = new Label();
		_prompt.Text = "Press  [E]  to talk";
		_prompt.AddThemeColorOverride("font_color", Colors.White);
		_prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		_prompt.AddThemeFontSizeOverride("font_size", 12);
		_prompt.AddThemeConstantOverride("shadow_offset_x", 1);
		_prompt.AddThemeConstantOverride("shadow_offset_y", 1);
		_prompt.HorizontalAlignment = HorizontalAlignment.Center;
		_prompt.Size     = new Vector2(160, 20);
		_prompt.Position = new Vector2(-80, 14);
		_prompt.Visible  = false;
		_uiRoot.AddChild(_prompt);

		BuildDialogueBox();

		// ── Proximity detection ──
		var area   = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape  = new CollisionShape2D { Shape = new CircleShape2D { Radius = 36f } };
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += OnBodyEntered;
		area.BodyExited  += OnBodyExited;

		_stateMachine.TransitionTo("Idle");
	}

	private void BuildDialogueBox()
	{
		_dlgLayer = new CanvasLayer { Layer = 60 };
		_dlgLayer.ProcessMode = ProcessModeEnum.Always;
		AddChild(_dlgLayer);

		_dlgScreen = new Control();
		_dlgScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_dlgScreen.ProcessMode = ProcessModeEnum.Always;
		_dlgScreen.Visible = false;
		_dlgLayer.AddChild(_dlgScreen);

		var boxAtlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(52, 49, 40, 46) };
		var boxStyle = new StyleBoxTexture
		{
			Texture             = boxAtlas,
			TextureMarginLeft   = 8, TextureMarginTop    = 8,
			TextureMarginRight  = 8, TextureMarginBottom = 10,
			ContentMarginLeft   = 20, ContentMarginTop   = 12,
			ContentMarginRight  = 20, ContentMarginBottom= 12,
		};
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", boxStyle);
		panel.AnchorLeft = 0f; panel.AnchorRight = 1f;
		panel.AnchorTop = 1f; panel.AnchorBottom = 1f;
		panel.OffsetLeft = 20f; panel.OffsetRight = -20f;
		panel.OffsetTop = -130f; panel.OffsetBottom = -12f;
		_dlgScreen.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);
		panel.AddChild(vbox);

		_dlgNameLabel = new Label();
		_dlgNameLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.38f, 0.1f));
		_dlgNameLabel.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(_dlgNameLabel);

		_dlgTextLabel = new Label();
		_dlgTextLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_dlgTextLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_dlgTextLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.07f, 0.02f));
		_dlgTextLabel.AddThemeFontSizeOverride("font_size", 15);
		vbox.AddChild(_dlgTextLabel);

		var hintRow = new HBoxContainer();
		var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		hintRow.AddChild(spacer);
		_dlgContinueHint = new Label();
		_dlgContinueHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.32f, 0.1f));
		_dlgContinueHint.AddThemeFontSizeOverride("font_size", 12);
		hintRow.AddChild(_dlgContinueHint);
		vbox.AddChild(hintRow);
	}

	public override void _Process(double delta)
	{
		_uiRoot.GlobalPosition = GlobalPosition + new Vector2(0, -52);
		_stateMachine?.Update(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		_stateMachine?.HandleInput(@event);
	}

	// ── State Interface ───────────────────────────────────────────────────
	public void UpdatePromptVisibility()
	{
		_prompt.Visible = _playerInRange && _stateMachine.CurrentState is NPCIdleState;
	}

	public void StartDialogue()
	{
		if (DialogueLines.Length == 0) return;
		_pageIndex = _cycleSeed;
		_stateMachine.TransitionTo("Talking");
	}

	public void OpenDialogueUI()
	{
		_dlgScreen.Visible = true;
		_prompt.Visible = false;
		ShowPage(_pageIndex);
	}

	public void AdvanceDialogue()
	{
		_pageIndex++;
		if (_pageIndex < DialogueLines.Length)
		{
			ShowPage(_pageIndex);
		}
		else
		{
			_cycleSeed = (_cycleSeed + 1) % DialogueLines.Length;
			if (Role == NpcRole.Merchant)
				_stateMachine.TransitionTo("Shop");
			else
				_stateMachine.TransitionTo("Idle");
		}
	}

	public void HideDialogueUI()
	{
		_dlgScreen.Visible = false;
		_blinkTween?.Kill();
		UpdatePromptVisibility();
	}

	private void ShowPage(int index)
	{
		_dlgNameLabel.Text = NpcName;
		_dlgTextLabel.Text = DialogueLines[index];

		bool hasMore = index < DialogueLines.Length - 1;
		if (!hasMore && Role == NpcRole.Merchant)
			_dlgContinueHint.Text = "▼   Press [E]  to browse shop";
		else
			_dlgContinueHint.Text = hasMore ? "▼   Press [E]" : "✓   Press [E]  to close";

		_blinkTween?.Kill();
		_blinkTween = _dlgContinueHint.CreateTween().SetLoops();
		_blinkTween.TweenProperty(_dlgContinueHint, "modulate:a", 0.25f, 0.55f).SetTrans(Tween.TransitionType.Sine);
		_blinkTween.TweenProperty(_dlgContinueHint, "modulate:a", 1.0f, 0.55f).SetTrans(Tween.TransitionType.Sine);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = true;
		UpdatePromptVisibility();
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = false;
		UpdatePromptVisibility();
		
		if (_stateMachine.CurrentState is NPCTalkingState or NPCShopState)
			_stateMachine.TransitionTo("Idle");
	}
}

public enum NpcRole { Dialogue, Merchant, QuestGiver }
