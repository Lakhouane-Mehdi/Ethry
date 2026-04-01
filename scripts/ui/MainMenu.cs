using Godot;

/// <summary>
/// Main menu scene — built entirely in code.
/// Set res://scenes/ui/main_menu.tscn as the main scene in Project Settings to use it.
/// "New Game" → loads base_level. "Continue" → loads save then base_level. "Quit" → exits.
/// </summary>
public partial class MainMenu : Node
{
	private const string GameScene = "res://scenes/levels/base_level.tscn";

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// Ensure game is unpaused when returning to menu
		GetTree().Paused = false;

		BuildUI();
	}

	// ── Build UI ───────────────────────────────────────────────────────────
	private void BuildUI()
	{
		// Full canvas layer so UI is always on top
		var layer = new CanvasLayer { Layer = 100 };
		AddChild(layer);

		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(root);

		// Dark gradient background
		var bg = new ColorRect();
		bg.Color = new Color(0.08f, 0.06f, 0.04f);
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(bg);

		// Subtle decorative vignette
		var vignette = new ColorRect();
		vignette.Color = new Color(0f, 0f, 0f, 0.35f);
		vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(vignette);

		// Centre panel
		var atlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(4, 49, 40, 46) };
		var style = new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 8,  TextureMarginTop    = 8,
			TextureMarginRight  = 8,  TextureMarginBottom = 10,
			ContentMarginLeft   = 40, ContentMarginTop    = 30,
			ContentMarginRight  = 40, ContentMarginBottom = 30,
		};
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", style);
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft   = -180; panel.OffsetRight  = 180;
		panel.OffsetTop    = -200; panel.OffsetBottom = 200;
		root.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 16);
		panel.AddChild(vbox);

		// Title
		var title = new Label { Text = "ETHRY" };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeColorOverride("font_color", new Color(0.88f, 0.68f, 0.28f));
		title.AddThemeFontSizeOverride("font_size", 36);
		title.AddThemeColorOverride("font_shadow_color", new Color(0.4f, 0.2f, 0.05f, 0.8f));
		title.AddThemeConstantOverride("shadow_offset_x", 2);
		title.AddThemeConstantOverride("shadow_offset_y", 2);
		vbox.AddChild(title);

		var subtitle = new Label { Text = "A Stardew-inspired adventure" };
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		subtitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.38f, 0.2f));
		subtitle.AddThemeFontSizeOverride("font_size", 11);
		vbox.AddChild(subtitle);

		var sep = new HSeparator();
		sep.Modulate = new Color(0.55f, 0.38f, 0.18f, 0.4f);
		vbox.AddChild(sep);

		var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
		vbox.AddChild(spacer);

		// Buttons
		bool hasSave = SaveSystem.HasSave();

		if (hasSave)
			AddBtn(vbox, "Continue", new Color(0.55f, 0.9f, 0.55f), OnContinue);

		AddBtn(vbox, hasSave ? "New Game" : "Play", new Color(0.92f, 0.82f, 0.45f), OnNewGame);
		AddBtn(vbox, "Quit",      new Color(0.85f, 0.38f, 0.32f),  () => GetTree().Quit());

		// Version footer
		var version = new Label { Text = "v0.1  —  Built with Godot 4" };
		version.HorizontalAlignment = HorizontalAlignment.Center;
		version.AddThemeColorOverride("font_color", new Color(0.3f, 0.22f, 0.1f, 0.6f));
		version.AddThemeFontSizeOverride("font_size", 9);
		vbox.AddChild(version);
	}

	// ── Button actions ─────────────────────────────────────────────────────
	private void OnNewGame()
	{
		// Delete save and start fresh
		if (SaveSystem.HasSave())
			DirAccess.RemoveAbsolute("user://ethry_save.json");

		GetTree().ChangeSceneToFile(GameScene);
	}

	private void OnContinue()
	{
		// Load into game scene; SaveSystem.Load() is called from autoload _Ready()
		GetTree().ChangeSceneToFile(GameScene);
		// Load runs after scene is ready via deferred:
		CallDeferred(MethodName.LoadSaveDeferred);
	}

	private void LoadSaveDeferred() => SaveSystem.Load();

	// ── Helper ─────────────────────────────────────────────────────────────
	private void AddBtn(VBoxContainer parent, string text, Color color, System.Action action)
	{
		var btn = new Button { Text = text };
		btn.AddThemeColorOverride("font_color",           color);
		btn.AddThemeFontSizeOverride("font_size",         15);
		btn.FocusMode = Control.FocusModeEnum.None;
		btn.CustomMinimumSize = new Vector2(0, 38);
		btn.Pressed += () => action();
		parent.AddChild(btn);
	}
}
