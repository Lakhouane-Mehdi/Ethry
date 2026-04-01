using Godot;

/// <summary>
/// Interactable NPC with a proper bottom-screen dialogue box (Stardew-style).
/// Press [E] to open dialogue, [E] again to advance through lines, closes on last.
/// </summary>
public partial class NPC : CharacterBody2D
{
	[Export] public string NpcName = "Villager";
	[Export] public NpcRole Role = NpcRole.Dialogue;
	[Export(PropertyHint.MultilineText)]
	public string[] DialogueLines = new[] { "Hello, traveller!" };

	[Export] public int[] ShopItems = System.Array.Empty<int>();

	private bool _playerInRange;
	private bool _dialogueOpen;
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

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	// ── Lifecycle ───────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// ── Floating prompt (screen-space TopLevel anchor) ──
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

		// ── Bottom dialogue box (CanvasLayer so it's always screen-space) ──
		BuildDialogueBox();

		// ── Proximity detection ──
		var area   = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape  = new CollisionShape2D();
		var circle = new CircleShape2D { Radius = 36f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += OnBodyEntered;
		area.BodyExited  += OnBodyExited;
	}

	private void BuildDialogueBox()
	{
		_dlgLayer = new CanvasLayer { Layer = 60 };
		_dlgLayer.ProcessMode = ProcessModeEnum.Always;
		AddChild(_dlgLayer);

		// Full-screen control so children can use anchors
		_dlgScreen = new Control();
		_dlgScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_dlgScreen.ProcessMode = ProcessModeEnum.Always;
		_dlgScreen.Visible = false;
		_dlgLayer.AddChild(_dlgScreen);

		// ── Panel background ──────────────────────────────────────────────
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
		// Anchored bottom-wide, 120px tall, 20px side margins, 12px from bottom
		panel.AnchorLeft   = 0f; panel.AnchorRight  = 1f;
		panel.AnchorTop    = 1f; panel.AnchorBottom = 1f;
		panel.OffsetLeft   =  20f;
		panel.OffsetRight  = -20f;
		panel.OffsetTop    = -130f;
		panel.OffsetBottom = -12f;
		_dlgScreen.AddChild(panel);

		// ── VBox: name + text + hint row ─────────────────────────────────
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);
		panel.AddChild(vbox);

		_dlgNameLabel = new Label();
		_dlgNameLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.38f, 0.1f));
		_dlgNameLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.7f));
		_dlgNameLabel.AddThemeFontSizeOverride("font_size", 13);
		_dlgNameLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_dlgNameLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		vbox.AddChild(_dlgNameLabel);

		_dlgTextLabel = new Label();
		_dlgTextLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_dlgTextLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_dlgTextLabel.AddThemeColorOverride("font_color", new Color(0.12f, 0.07f, 0.02f));
		_dlgTextLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.4f));
		_dlgTextLabel.AddThemeFontSizeOverride("font_size", 15);
		_dlgTextLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_dlgTextLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		vbox.AddChild(_dlgTextLabel);

		// Hint row: spacer + "▼ E to continue"
		var hintRow = new HBoxContainer();
		var spacer  = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		hintRow.AddChild(spacer);

		_dlgContinueHint = new Label();
		_dlgContinueHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.32f, 0.1f));
		_dlgContinueHint.AddThemeFontSizeOverride("font_size", 12);
		hintRow.AddChild(_dlgContinueHint);
		vbox.AddChild(hintRow);
	}

	// ── Process ─────────────────────────────────────────────────────────────
	public override void _Process(double delta)
	{
		// Keep floating prompt above NPC in world-to-screen
		_uiRoot.GlobalPosition = GlobalPosition + new Vector2(0, -52);

		if (!_playerInRange) return;
		if (Input.IsActionJustPressed("interact"))
			Interact();
	}

	// ── Dialogue flow ──────────────────────────────────────────────────────
	private void Interact()
	{
		if (DialogueLines.Length == 0) return;

		if (_dialogueOpen)
		{
			// Advance to next line
			_pageIndex++;
			if (_pageIndex < DialogueLines.Length)
				ShowPage(_pageIndex);
			else
			{
				// After last line: open shop if merchant, otherwise close
				_cycleSeed = (_cycleSeed + 1) % DialogueLines.Length;
				if (Role == NpcRole.Merchant)
				{
					CloseDialogue();
					var shop = GetNodeOrNull<ShopUI>("ShopUI");
					shop?.Open();
				}
				else
				{
					CloseDialogue();
				}
			}
		}
		else
		{
			// Start new session (cycle from where we left off)
			_pageIndex = _cycleSeed;
			ShowPage(_pageIndex);
		}
	}

	private void ShowPage(int index)
	{
		_dialogueOpen = true;
		_prompt.Visible = false;
		_dlgScreen.Visible = true;

		_dlgNameLabel.Text = NpcName;
		_dlgTextLabel.Text = DialogueLines[index];

		bool hasMore = index < DialogueLines.Length - 1;
		// If merchant and this is the last line: hint opens shop next
		if (!hasMore && Role == NpcRole.Merchant)
			_dlgContinueHint.Text = "▼   Press [E]  to browse shop";
		else
			_dlgContinueHint.Text = hasMore ? "▼   Press [E]" : "✓   Press [E]  to close";

		// Blink the hint
		_blinkTween?.Kill();
		_blinkTween = _dlgContinueHint.CreateTween().SetLoops();
		_blinkTween.TweenProperty(_dlgContinueHint, "modulate:a", 0.25f, 0.55f)
				   .SetTrans(Tween.TransitionType.Sine);
		_blinkTween.TweenProperty(_dlgContinueHint, "modulate:a", 1.0f,  0.55f)
				   .SetTrans(Tween.TransitionType.Sine);
	}

	private void CloseDialogue()
	{
		_dialogueOpen = false;
		_blinkTween?.Kill();
		_dlgScreen.Visible = false;
		if (_playerInRange) _prompt.Visible = true;
	}

	// ── Area callbacks ────────────────────────────────────────────────────
	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = true;
		if (!_dialogueOpen) _prompt.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = false;
		_prompt.Visible = false;
		if (_dialogueOpen) CloseDialogue();
	}
}

public enum NpcRole
{
	Dialogue,
	Merchant,
	QuestGiver,
}
