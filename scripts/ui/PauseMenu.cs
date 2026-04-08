using Godot;

/// <summary>
/// Pause menu — shown when player presses Esc while no other menu is open.
/// Builds its entire UI in code (no .tscn needed — it's an autoload CanvasLayer).
/// Includes main panel, settings submenu, and a quit-confirm dialog.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
	public static PauseMenu Instance { get; private set; }

	private Control _screen;
	private Control _mainPanel;
	private Control _settingsPanel;
	private Control _confirmPanel;
	private Label   _statsLabel;
	private bool    _open;

	// Tracked playtime in seconds (this session). Persisted only if SaveSystem extends to it.
	private double _playSeconds;

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	private static readonly Color TitleColor    = new(0.75f, 0.52f, 0.18f);
	private static readonly Color FooterColor   = new(0.5f, 0.38f, 0.2f, 0.85f);
	private static readonly Color SepColor      = new(0.55f, 0.38f, 0.18f, 0.5f);

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance    = this;
		Layer       = 50;
		ProcessMode = ProcessModeEnum.Always;
		BuildUI();
	}

	public override void _Process(double delta)
	{
		// Only count time when the game is actually running (not paused)
		if (!GetTree().Paused)
			_playSeconds += delta;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel")) return;

		if (_open)
		{
			// Step back through sub-screens before closing fully
			if (_confirmPanel.Visible) { _confirmPanel.Visible = false; _mainPanel.Visible = true; }
			else if (_settingsPanel.Visible) { _settingsPanel.Visible = false; _mainPanel.Visible = true; }
			else Close();
		}
		else
		{
			if (!GetTree().Paused) Open();
		}
		GetViewport().SetInputAsHandled();
	}

	// ── Open / Close ──────────────────────────────────────────────────────
	public void Open()
	{
		_open            = true;
		_screen.Visible  = true;
		_mainPanel.Visible     = true;
		_settingsPanel.Visible = false;
		_confirmPanel.Visible  = false;
		GetTree().Paused = true;
		RefreshStats();
	}

	public void Close()
	{
		_open            = false;
		_screen.Visible  = false;
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

		// Blur backdrop
		var bbc = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport };
		_screen.AddChild(bbc);

		var dim = new ColorRect();
		dim.Color = new Color(0, 0, 0, 0.45f);
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		const string BlurMatPath = "res://shaders/blur_material.tres";
		if (FileAccess.FileExists(BlurMatPath))
			dim.Material = GD.Load<ShaderMaterial>(BlurMatPath);
		_screen.AddChild(dim);

		_mainPanel     = BuildMainPanel();
		_settingsPanel = BuildSettingsPanel();
		_confirmPanel  = BuildConfirmPanel();
		_screen.AddChild(_mainPanel);
		_screen.AddChild(_settingsPanel);
		_screen.AddChild(_confirmPanel);
		_settingsPanel.Visible = false;
		_confirmPanel.Visible  = false;
	}

	private StyleBoxTexture MakePanelStyle()
	{
		var atlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(4, 49, 40, 46) };
		return new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 8, TextureMarginTop    = 8,
			TextureMarginRight  = 8, TextureMarginBottom = 10,
			ContentMarginLeft   = 30, ContentMarginTop   = 24,
			ContentMarginRight  = 30, ContentMarginBottom = 24,
		};
	}

	private PanelContainer MakeCenteredPanel(int halfW, int halfH)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", MakePanelStyle());
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft   = -halfW; panel.OffsetRight  = halfW;
		panel.OffsetTop    = -halfH; panel.OffsetBottom = halfH;
		return panel;
	}

	// ── Main panel ─────────────────────────────────────────────────────────
	private Control BuildMainPanel()
	{
		var panel = MakeCenteredPanel(150, 220);
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		panel.AddChild(vbox);

		AddTitle(vbox, "PAUSED");
		AddSeparator(vbox);

		AddMenuBtn(vbox, "Resume",     Colors.White,                  Close);
		AddMenuBtn(vbox, "Save Game",  new Color(0.6f, 0.9f, 0.55f),
			() => { SaveSystem.Save(); NotificationManager.Instance?.Show("Game saved!", new Color(0.5f, 0.9f, 0.5f)); });
		AddMenuBtn(vbox, "Settings",   new Color(0.55f, 0.78f, 0.95f),
			() => { _mainPanel.Visible = false; _settingsPanel.Visible = true; });
		AddMenuBtn(vbox, "Main Menu",  new Color(0.95f, 0.78f, 0.45f),
			() => ShowConfirm("Return to main menu?", () =>
			{
				GetTree().Paused = false;
				GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
			}));
		AddMenuBtn(vbox, "Quit to Desktop", new Color(0.9f, 0.4f, 0.35f),
			() => ShowConfirm("Quit to desktop?", () => GetTree().Quit()));

		AddSeparator(vbox);

		// Stats footer
		_statsLabel = new Label();
		_statsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_statsLabel.AddThemeColorOverride("font_color", FooterColor);
		_statsLabel.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(_statsLabel);

		return panel;
	}

	private void RefreshStats()
	{
		if (_statsLabel == null) return;
		string day  = DaySystem.Instance?.DayString ?? "Day 1 · Spring · Year 1";
		int    gold = PlayerData.Instance?.Gold ?? 0;
		var t = System.TimeSpan.FromSeconds(_playSeconds);
		string playtime = $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
		_statsLabel.Text = $"{day}\nGold: {gold}\nPlaytime: {playtime}";
	}

	// ── Settings panel ─────────────────────────────────────────────────────
	private Control BuildSettingsPanel()
	{
		var panel = MakeCenteredPanel(180, 240);
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		AddTitle(vbox, "SETTINGS");
		AddSeparator(vbox);

		// Volume sliders
		AddSlider(vbox, "Master",
			() => AudioManager.Instance?.MasterVolume ?? 1f,
			v => { if (AudioManager.Instance != null) AudioManager.Instance.MasterVolume = v; });
		AddSlider(vbox, "Music",
			() => AudioManager.Instance?.MusicVolume ?? 0f,
			v => { if (AudioManager.Instance != null) AudioManager.Instance.MusicVolume = v; });
		AddSlider(vbox, "SFX",
			() => AudioManager.Instance?.SfxVolume ?? 0f,
			v => { if (AudioManager.Instance != null) AudioManager.Instance.SfxVolume = v; });

		AddSeparator(vbox);

		// Display toggles
		AddCheck(vbox, "Fullscreen",
			() => DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen,
			on => DisplayServer.WindowSetMode(on ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed));
		AddCheck(vbox, "VSync",
			() => DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled,
			on => DisplayServer.WindowSetVsyncMode(on ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled));

		AddSeparator(vbox);

		AddMenuBtn(vbox, "Back", Colors.White, () =>
		{
			AudioManager.Instance?.SaveSettings();
			_settingsPanel.Visible = false;
			_mainPanel.Visible = true;
		});

		return panel;
	}

	// ── Confirm dialog ─────────────────────────────────────────────────────
	private Label _confirmLabel;
	private System.Action _confirmAction;

	private Control BuildConfirmPanel()
	{
		var panel = MakeCenteredPanel(160, 90);
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		panel.AddChild(vbox);

		_confirmLabel = new Label { Text = "Are you sure?" };
		_confirmLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_confirmLabel.AddThemeColorOverride("font_color", TitleColor);
		_confirmLabel.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(_confirmLabel);

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 16);
		hbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddChild(hbox);

		var yes = new Button { Text = "Yes" };
		yes.AddThemeColorOverride("font_color", new Color(0.9f, 0.4f, 0.35f));
		yes.AddThemeFontSizeOverride("font_size", 14);
		yes.FocusMode = Control.FocusModeEnum.None;
		yes.Pressed += () => { _confirmAction?.Invoke(); };
		hbox.AddChild(yes);

		var no = new Button { Text = "No" };
		no.AddThemeColorOverride("font_color", Colors.White);
		no.AddThemeFontSizeOverride("font_size", 14);
		no.FocusMode = Control.FocusModeEnum.None;
		no.Pressed += () => { _confirmPanel.Visible = false; _mainPanel.Visible = true; };
		hbox.AddChild(no);

		return panel;
	}

	private void ShowConfirm(string text, System.Action onYes)
	{
		_confirmLabel.Text = text;
		_confirmAction     = onYes;
		_mainPanel.Visible = false;
		_confirmPanel.Visible = true;
	}

	// ── Helpers ────────────────────────────────────────────────────────────
	private void AddTitle(VBoxContainer parent, string text)
	{
		var t = new Label { Text = text };
		t.HorizontalAlignment = HorizontalAlignment.Center;
		t.AddThemeColorOverride("font_color", TitleColor);
		t.AddThemeFontSizeOverride("font_size", 20);
		parent.AddChild(t);
	}

	private void AddSeparator(VBoxContainer parent)
	{
		var sep = new HSeparator { Modulate = SepColor };
		parent.AddChild(sep);
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

	private void AddSlider(VBoxContainer parent, string label, System.Func<float> getter, System.Action<float> setter)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		parent.AddChild(row);

		var lbl = new Label { Text = label };
		lbl.CustomMinimumSize = new Vector2(60, 0);
		lbl.AddThemeColorOverride("font_color", FooterColor);
		lbl.AddThemeFontSizeOverride("font_size", 12);
		row.AddChild(lbl);

		var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = getter() };
		slider.CustomMinimumSize = new Vector2(150, 0);
		slider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(slider);

		var pct = new Label { Text = $"{(int)(getter() * 100)}%" };
		pct.CustomMinimumSize = new Vector2(36, 0);
		pct.AddThemeColorOverride("font_color", FooterColor);
		pct.AddThemeFontSizeOverride("font_size", 12);
		pct.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(pct);

		slider.ValueChanged += v =>
		{
			setter((float)v);
			pct.Text = $"{(int)(v * 100)}%";
		};
	}

	private void AddCheck(VBoxContainer parent, string label, System.Func<bool> getter, System.Action<bool> setter)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		parent.AddChild(row);

		var lbl = new Label { Text = label };
		lbl.CustomMinimumSize = new Vector2(60, 0);
		lbl.AddThemeColorOverride("font_color", FooterColor);
		lbl.AddThemeFontSizeOverride("font_size", 12);
		row.AddChild(lbl);

		var box = new CheckBox { ButtonPressed = getter() };
		box.FocusMode = Control.FocusModeEnum.None;
		box.Toggled += on => setter(on);
		row.AddChild(box);
	}
}
