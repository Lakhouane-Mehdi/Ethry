using Godot;

/// <summary>
/// Top-left HUD overlay: clock, day/season, gold.
/// Builds itself entirely in code — no scene file needed.
/// Sits on CanvasLayer 8 (above weather layer 2, below inventory layer 10).
/// </summary>
public partial class GameHUD : CanvasLayer
{
	private Label _timeLabel;
	private Label _dayLabel;
	private Label _goldLabel;
	private Label _weatherLabel;
	private PanelContainer _panel;

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Layer       = 8;
		ProcessMode = ProcessModeEnum.Always;

		BuildPanel();

		// Connect to singletons
		DaySystem.Instance.TimeChanged    += _ => RefreshTime();
		DaySystem.Instance.DayAdvanced    += (_, _, _) => RefreshTime();
		PlayerData.Instance.GoldChanged   += _ => RefreshGold();

		if (WeatherSystem.Instance != null)
			WeatherSystem.Instance.WeatherChanged += _ => RefreshWeather();

		RefreshTime();
		RefreshGold();
		RefreshWeather();
	}

	// ── Build UI ───────────────────────────────────────────────────────────
	private void BuildPanel()
	{
		// Background panel — nine-patch from ui_frames.png
		var atlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(4, 49, 40, 46) };
		var style = new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 8, TextureMarginTop    = 8,
			TextureMarginRight  = 8, TextureMarginBottom = 10,
			ContentMarginLeft   = 10, ContentMarginTop   = 6,
			ContentMarginRight  = 10, ContentMarginBottom = 6,
		};
		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", style);
		// Top-right corner, 10px margin
		_panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_panel.GrowHorizontal = Control.GrowDirection.Begin; // grow to the left
		_panel.OffsetRight = -10;
		_panel.OffsetTop   = 10;
		AddChild(_panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 2);
		_panel.AddChild(vbox);

		// Clock row: ☀ / 🌙 time
		_timeLabel = MakeLabel("6:00 AM", new Color(0.95f, 0.82f, 0.35f), 13);
		vbox.AddChild(_timeLabel);

		// Day / Season row
		_dayLabel  = MakeLabel("Day 1  ·  Spring", new Color(0.55f, 0.38f, 0.18f), 10);
		vbox.AddChild(_dayLabel);

		// Weather row
		_weatherLabel = MakeLabel("Clear", new Color(0.65f, 0.78f, 0.95f), 10);
		vbox.AddChild(_weatherLabel);

		// Gold row
		_goldLabel = MakeLabel("0 g", new Color(1f, 0.88f, 0.28f), 12);
		vbox.AddChild(_goldLabel);
	}

	private static Label MakeLabel(string text, Color color, int size)
	{
		var lbl = new Label { Text = text };
		lbl.AddThemeColorOverride("font_color", color);
		lbl.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.75f));
		lbl.AddThemeFontSizeOverride("font_size", size);
		lbl.AddThemeConstantOverride("shadow_offset_x", 1);
		lbl.AddThemeConstantOverride("shadow_offset_y", 1);
		lbl.MouseFilter = Control.MouseFilterEnum.Ignore;
		return lbl;
	}

	// ── Refresh ────────────────────────────────────────────────────────────
	private void RefreshTime()
	{
		var ds = DaySystem.Instance;
		string icon = ds.IsNight ? "🌙" : "☀";
		_timeLabel.Text = $"{icon}  {ds.TimeString}";
		_dayLabel.Text  = ds.DayString;

		// Tint the panel slightly warmer at sunrise/dusk
		float h = ds.Hour;
		if (h < 8f || h > 20f)       // night/early morning — cool
			_timeLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.78f, 1f));
		else if (h < 10f || h > 18f) // golden hour
			_timeLabel.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.3f));
		else                          // midday — bright
			_timeLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.45f));
	}

	private void RefreshWeather()
	{
		if (WeatherSystem.Instance == null) return;
		var w = WeatherSystem.Instance.CurrentWeather;
		(string icon, string name, Color col) = w switch
		{
			WeatherSystem.WeatherType.Cloudy => ("~",  "Cloudy", new Color(0.65f, 0.68f, 0.75f)),
			WeatherSystem.WeatherType.Rain   => ("~",  "Rain",   new Color(0.5f,  0.65f, 0.9f)),
			WeatherSystem.WeatherType.Storm  => ("!!", "Storm",  new Color(0.6f,  0.45f, 0.85f)),
			WeatherSystem.WeatherType.Snow   => ("*",  "Snow",   new Color(0.8f,  0.85f, 0.95f)),
			_                                => ("o",  "Clear",  new Color(0.95f, 0.85f, 0.45f)),
		};
		_weatherLabel.Text = $"{icon}  {name}";
		_weatherLabel.AddThemeColorOverride("font_color", col);
	}

	private void RefreshGold()
	{
		_goldLabel.Text = $"⬡  {PlayerData.Instance.Gold} g";
	}
}
