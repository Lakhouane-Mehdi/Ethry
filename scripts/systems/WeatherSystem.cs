using Godot;

/// <summary>
/// Dynamic weather system with seasonal behaviour (Stardew Valley-style).
///   Spring: frequent light rain showers, moderate clouds.
///   Summer: mostly sunny, rare thunderstorms (heavy rain + lightning).
///   Autumn: overcast skies, frequent drizzle, wind gusts.
///   Winter: snow particles replace rain, constant overcast.
/// Clouds drift in world-space and wrap correctly on all edges.
/// Rain/snow auto-waters farm plots via the WeatherChanged signal.
/// </summary>
public partial class WeatherSystem : CanvasLayer
{
	public static WeatherSystem Instance { get; private set; }

	// ── Exports ────────────────────────────────────────────────────────────
	[Export] public bool  StartRaining  = false;
	[Export] public float CloudSpeedMin = 6f;
	[Export] public float CloudSpeedMax = 16f;
	[Export] public int   CloudCount    = 14;

	// ── Signals ────────────────────────────────────────────────────────────
	[Signal] public delegate void WeatherChangedEventHandler(bool isRaining);
	[Signal] public delegate void ThunderClapEventHandler();

	// ── Weather types ──────────────────────────────────────────────────────
	public enum WeatherType { Clear, Cloudy, Rain, Storm, Snow }

	public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
	public bool IsRaining => CurrentWeather is WeatherType.Rain or WeatherType.Storm;
	public bool IsSnowing => CurrentWeather == WeatherType.Snow;
	public bool IsPrecipitating => IsRaining || IsSnowing;

	// ── Internal state ─────────────────────────────────────────────────────
	private float _weatherTimer;
	private float _lightningTimer;
	private float _windGustTimer;

	private GpuParticles2D _rainParticles;
	private GpuParticles2D _snowParticles;
	private GpuParticles2D _windParticles;
	private ColorRect      _lightningFlash;

	private struct CloudEntry
	{
		public Sprite2D Sprite;
		public float    Speed;
		public Vector2  WorldPos;
		public float    BaseAlpha;
	}
	private CloudEntry[] _clouds;
	private bool         _cloudsReady;

	private float _screenW;
	private float _screenH;

	// ── Textures ───────────────────────────────────────────────────────────
	private static readonly Texture2D RainTex  = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/weather effects/rain_drop.png");
	private static readonly Texture2D CloudTex = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/weather effects/clouds.png");
	private static readonly Texture2D WindTex  = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/weather effects/wind_anim.png");

	// ── Tint presets ───────────────────────────────────────────────────────
	private static readonly Color ClearTint   = new(1f, 1f, 1f, 1f);
	private static readonly Color CloudyTint  = new(0.82f, 0.84f, 0.88f, 1f);
	private static readonly Color RainTint    = new(0.55f, 0.62f, 0.78f, 1f);
	private static readonly Color StormTint   = new(0.38f, 0.42f, 0.58f, 1f);
	private static readonly Color SnowTint    = new(0.78f, 0.82f, 0.92f, 1f);

	// ── Season-based weather weights: [Clear, Cloudy, Rain, Storm, Snow] ──
	private static readonly float[][] SeasonWeights = new[]
	{
		new float[] { 35, 25, 30, 10, 0 },   // Spring: frequent showers
		new float[] { 55, 20, 10, 15, 0 },   // Summer: sunny, rare thunderstorms
		new float[] { 20, 35, 35, 10, 0 },   // Autumn: overcast, drizzle
		new float[] { 10, 25,  0,  0, 65 },  // Winter: mostly snow
	};

	// ── Season-based duration ranges (min, max seconds) ───────────────────
	private static readonly (float min, float max)[] ClearDurations =
		{ (40, 100), (60, 150), (25, 60), (20, 50) };
	private static readonly (float min, float max)[] PrecipDurations =
		{ (20, 50), (15, 40), (30, 70), (40, 90) };

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance = this;
		Layer = 2;

		var rect = GetViewport().GetVisibleRect().Size;
		_screenW = rect.X;
		_screenH = rect.Y;

		_lightningFlash = new ColorRect
		{
			Color = new Color(1, 1, 1, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 100
		};
		_lightningFlash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_lightningFlash);

		CreateClouds();
		CreateRain();
		CreateSnow();
		CreateWind();

		// Initial weather
		if (StartRaining)
			SetWeatherImmediate(WeatherType.Rain);
		else
			SetWeatherImmediate(WeatherType.Clear);

		_weatherTimer = RollDuration();
		_lightningTimer = (float)GD.RandRange(8, 20);
		_windGustTimer  = (float)GD.RandRange(5, 15);
	}

	public override void _Process(double delta)
	{
		if (GetTree().Paused || !_cloudsReady) return;

		float dt = (float)delta;

		_weatherTimer -= dt;
		if (_weatherTimer <= 0) RollNewWeather();

		// Storm lightning
		if (CurrentWeather == WeatherType.Storm)
		{
			_lightningTimer -= dt;
			if (_lightningTimer <= 0) TriggerLightning();
		}

		// Autumn wind gusts
		if (DaySystem.Instance?.SeasonIndex == 2 && IsPrecipitating)
		{
			_windGustTimer -= dt;
			if (_windGustTimer <= 0) TriggerWindGust();
		}

		UpdateClouds(dt);
	}

	// ── Weather rolling ────────────────────────────────────────────────────
	private void RollNewWeather()
	{
		int season = DaySystem.Instance?.SeasonIndex ?? 0;
		float[] weights = SeasonWeights[Mathf.Clamp(season, 0, 3)];

		float total = 0;
		foreach (float w in weights) total += w;
		float roll = (float)GD.RandRange(0, total);

		float cumulative = 0;
		WeatherType picked = WeatherType.Clear;
		for (int i = 0; i < weights.Length; i++)
		{
			cumulative += weights[i];
			if (roll < cumulative)
			{
				picked = (WeatherType)i;
				break;
			}
		}

		// Avoid repeating the same weather twice in a row (unless snow in winter)
		if (picked == CurrentWeather && season != 3)
		{
			picked = picked == WeatherType.Clear ? WeatherType.Cloudy : WeatherType.Clear;
		}

		TransitionTo(picked);
		_weatherTimer = RollDuration();
	}

	private float RollDuration()
	{
		int season = DaySystem.Instance?.SeasonIndex ?? 0;
		season = Mathf.Clamp(season, 0, 3);

		bool precip = IsPrecipitating;
		var range = precip ? PrecipDurations[season] : ClearDurations[season];
		return (float)GD.RandRange(range.min, range.max);
	}

	// ── Transitions ────────────────────────────────────────────────────────
	private void TransitionTo(WeatherType newWeather)
	{
		bool wasPrecip = IsPrecipitating;
		CurrentWeather = newWeather;
		bool isPrecip  = IsPrecipitating;

		// Emit signal if precipitation state changed
		if (wasPrecip != isPrecip)
			EmitSignal(SignalName.WeatherChanged, isPrecip);

		var tween = CreateTween().SetParallel(true);
		float dur = 3.5f;

		// Rain particles
		float rainTarget = (newWeather == WeatherType.Rain) ? 0.7f
		                 : (newWeather == WeatherType.Storm) ? 1.0f : 0.0f;
		tween.TweenProperty(_rainParticles, "amount_ratio", rainTarget, dur);

		// Snow particles
		float snowTarget = (newWeather == WeatherType.Snow) ? 1.0f : 0.0f;
		tween.TweenProperty(_snowParticles, "amount_ratio", snowTarget, dur);

		// Wind particles
		float windTarget = (newWeather is WeatherType.Storm or WeatherType.Snow) ? 1.0f
		                 : (newWeather == WeatherType.Rain) ? 0.4f : 0.0f;
		tween.TweenProperty(_windParticles, "amount_ratio", windTarget, dur);

		// Cloud opacity (moved to a baseline alpha)
		float cloudAlpha = newWeather switch
		{
			WeatherType.Clear  => 0.12f,
			WeatherType.Cloudy => 0.18f,
			WeatherType.Rain   => 0.28f,
			WeatherType.Storm  => 0.35f,
			WeatherType.Snow   => 0.22f,
			_                  => 0.12f,
		};
		for (int ci = 0; ci < _clouds.Length; ci++)
		{
			_clouds[ci].BaseAlpha = cloudAlpha;
			Color c = _clouds[ci].Sprite.Modulate;
			c.A = cloudAlpha;
			tween.TweenProperty(_clouds[ci].Sprite, "modulate", c, dur);
		}

		// Reset timers
		if (newWeather == WeatherType.Storm)
			_lightningTimer = (float)GD.RandRange(5, 12);
	}

	private void SetWeatherImmediate(WeatherType weather)
	{
		CurrentWeather = weather;

		float rainRatio = (weather == WeatherType.Rain) ? 0.7f
		                : (weather == WeatherType.Storm) ? 1.0f : 0.0f;
		_rainParticles.AmountRatio = rainRatio;

		_snowParticles.AmountRatio = weather == WeatherType.Snow ? 1.0f : 0.0f;

		float windRatio = (weather is WeatherType.Storm or WeatherType.Snow) ? 1.0f
		                : (weather == WeatherType.Rain) ? 0.4f : 0.0f;
		_windParticles.AmountRatio = windRatio;

		// (Tint logic handled externally now)
		float cloudAlpha = weather switch
		{
			WeatherType.Clear  => 0.12f,
			WeatherType.Cloudy => 0.18f,
			WeatherType.Rain   => 0.28f,
			WeatherType.Storm  => 0.35f,
			WeatherType.Snow   => 0.22f,
			_                  => 0.12f,
		};
		for (int ci = 0; ci < _clouds.Length; ci++)
		{
			_clouds[ci].BaseAlpha = cloudAlpha;
			Color c = _clouds[ci].Sprite.Modulate;
			c.A = cloudAlpha;
			_clouds[ci].Sprite.Modulate = c;
		}
	}

	public Color GetWeatherTint()
	{
		return CurrentWeather switch
		{
			WeatherType.Cloudy => CloudyTint,
			WeatherType.Rain   => RainTint,
			WeatherType.Storm  => StormTint,
			WeatherType.Snow   => SnowTint,
			_                  => ClearTint,
		};
	}

	public void SyncWithLighting(Color ambientTint)
	{
		if (!_cloudsReady) return;
		
		for (int i = 0; i < _clouds.Length; i++)
		{
			Color c = ambientTint;
			c.A = _clouds[i].BaseAlpha;
			_clouds[i].Sprite.Modulate = c;
		}
	}

	// ── Lightning ──────────────────────────────────────────────────────────
	private void TriggerLightning()
	{
		_lightningTimer = (float)GD.RandRange(6, 18);
		EmitSignal(SignalName.ThunderClap);

		var tween = CreateTween();
		tween.TweenProperty(_lightningFlash, "color:a", 0.6f, 0.04f);
		tween.TweenProperty(_lightningFlash, "color:a", 0.15f, 0.08f);
		tween.TweenProperty(_lightningFlash, "color:a", 0.45f, 0.03f);
		tween.TweenProperty(_lightningFlash, "color:a", 0.0f, 0.4f).SetDelay(0.05f);
	}

	// ── Wind gusts (autumn) ────────────────────────────────────────────────
	private void TriggerWindGust()
	{
		_windGustTimer = (float)GD.RandRange(8, 20);

		// Briefly speed up clouds
		for (int ci = 0; ci < _clouds.Length; ci++)
		{
			float origSpeed = _clouds[ci].Speed;
			_clouds[ci].Speed *= 3f;

			// Schedule reset (capture index + value)
			float reset = origSpeed;
			int idx = ci;
			GetTree().CreateTimer(1.5).Timeout += () =>
			{
				if (idx >= 0 && idx < _clouds.Length)
					_clouds[idx].Speed = reset;
			};
		}

		// Boost wind particles briefly
		var windTween = CreateTween();
		windTween.TweenProperty(_windParticles, "amount_ratio", 1.0f, 0.2f);
		windTween.TweenProperty(_windParticles, "amount_ratio", 0.4f, 2.0f).SetDelay(1.5f);
	}

	// ── Cloud update with correct wrapping ─────────────────────────────────
	private void UpdateClouds(float dt)
	{
		var cam    = GetViewport().GetCamera2D();
		var camPos = cam?.GlobalPosition ?? Vector2.Zero;
		var center = new Vector2(_screenW * 0.5f, _screenH * 0.5f);

		float wrapDist = _screenW * 1.5f;
		float wrapDistY = _screenH * 1.5f;

		for (int i = 0; i < _clouds.Length; i++)
		{
			ref var cd = ref _clouds[i];
			cd.WorldPos.X += cd.Speed * dt;

			float relX = cd.WorldPos.X - camPos.X;
			float relY = cd.WorldPos.Y - camPos.Y;

			// Wrap on all edges
			if (relX > wrapDist)
			{
				cd.WorldPos.X = camPos.X - wrapDist - (float)GD.RandRange(0, 200);
				cd.WorldPos.Y = camPos.Y + (float)GD.RandRange(-_screenH, _screenH);
			}
			else if (relX < -wrapDist)
			{
				cd.WorldPos.X = camPos.X + wrapDist + (float)GD.RandRange(0, 200);
				cd.WorldPos.Y = camPos.Y + (float)GD.RandRange(-_screenH, _screenH);
			}

			if (relY > wrapDistY)
				cd.WorldPos.Y = camPos.Y - wrapDistY;
			else if (relY < -wrapDistY)
				cd.WorldPos.Y = camPos.Y + wrapDistY;

			var screenPos = (cd.WorldPos - camPos) + center;
			cd.Sprite.Position = screenPos;
			cd.Sprite.Visible = true;
		}
	}

	// ── Cloud creation ─────────────────────────────────────────────────────
	private void CreateClouds()
	{
		_clouds = new CloudEntry[CloudCount];
		var cam    = GetViewport().GetCamera2D();
		var camPos = cam?.GlobalPosition ?? Vector2.Zero;

		for (int i = 0; i < CloudCount; i++)
		{
			var c = new Sprite2D { Texture = CloudTex, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
			c.Modulate = new Color(1f, 1f, 1f, 0.04f);
			c.ZIndex = 3;

			float sx = (float)GD.RandRange(4.0, 8.0);
			float sy = (float)GD.RandRange(3.0, 6.0);
			c.Scale = new Vector2(sx, sy);
			AddChild(c);

			var worldPos = new Vector2(
				camPos.X + (float)GD.RandRange(-_screenW * 2f, _screenW * 2f),
				camPos.Y + (float)GD.RandRange(-_screenH * 2f, _screenH * 2f)
			);

			// Alternate speeds — some fast, some slow for depth
			float speed = (float)GD.RandRange(CloudSpeedMin, CloudSpeedMax);
			if (i % 3 == 0) speed *= -0.5f; // Some clouds drift the other way

			_clouds[i] = new CloudEntry
			{
				Sprite    = c,
				Speed     = speed,
				WorldPos  = worldPos,
				BaseAlpha = 0.12f + (float)GD.RandRange(0, 0.1f),
			};
		}
		_cloudsReady = true;
	}

	// ── Rain particles ─────────────────────────────────────────────────────
	private void CreateRain()
	{
		_rainParticles = new GpuParticles2D
		{
			Amount     = 300,
			Lifetime   = 1.0f,
			Preprocess = 0.5f,
			Emitting   = true,
			AmountRatio = 0.0f
		};

		var mat = new ParticleProcessMaterial();
		mat.Direction           = new Vector3(0.15f, 1f, 0f);
		mat.Spread              = 3f;
		mat.InitialVelocityMin  = 380f;
		mat.InitialVelocityMax  = 520f;
		mat.Gravity             = new Vector3(0, 180, 0);
		mat.EmissionShape       = ParticleProcessMaterial.EmissionShapeEnum.Box;
		mat.EmissionBoxExtents  = new Vector3(_screenW * 0.8f, 10, 0);
		mat.ScaleMin            = 2.0f;
		mat.ScaleMax            = 3.5f;
		mat.Color               = new Color(0.7f, 0.85f, 1f, 0.55f);

		_rainParticles.ProcessMaterial = mat;
		_rainParticles.Texture         = RainTex;
		_rainParticles.Position        = new Vector2(_screenW * 0.5f, -20);
		AddChild(_rainParticles);
	}

	// ── Snow particles ─────────────────────────────────────────────────────
	private void CreateSnow()
	{
		_snowParticles = new GpuParticles2D
		{
			Amount      = 200,
			Lifetime    = 4.0f,
			Preprocess  = 1.0f,
			Emitting    = true,
			AmountRatio = 0.0f,
		};

		var mat = new ParticleProcessMaterial();
		mat.Direction           = new Vector3(0.1f, 1f, 0f);
		mat.Spread              = 15f;
		mat.InitialVelocityMin  = 25f;
		mat.InitialVelocityMax  = 55f;
		mat.Gravity             = new Vector3(0, 12, 0);
		mat.EmissionShape       = ParticleProcessMaterial.EmissionShapeEnum.Box;
		mat.EmissionBoxExtents  = new Vector3(_screenW * 0.9f, 10, 0);
		mat.ScaleMin            = 1.5f;
		mat.ScaleMax            = 3.5f;
		mat.Color               = new Color(0.95f, 0.97f, 1f, 0.7f);
		// Gentle sway via turbulence
		mat.TurbulenceEnabled       = true;
		mat.TurbulenceNoiseStrength = 3f;
		mat.TurbulenceNoiseSpeed    = new Vector3(0.4f, 0.1f, 0);

		_snowParticles.ProcessMaterial = mat;
		_snowParticles.Texture         = RainTex; // reuse drop texture for snowflakes
		_snowParticles.Position        = new Vector2(_screenW * 0.5f, -30);
		AddChild(_snowParticles);
	}

	// ── Wind particles ─────────────────────────────────────────────────────
	private void CreateWind()
	{
		_windParticles = new GpuParticles2D
		{
			Amount      = 15,
			Lifetime    = 2.5f,
			Emitting    = true,
			AmountRatio = 0.0f,
		};

		var mat = new ParticleProcessMaterial();
		mat.Direction           = new Vector3(1f, 0.12f, 0f);
		mat.Spread              = 12f;
		mat.InitialVelocityMin  = 120f;
		mat.InitialVelocityMax  = 220f;
		mat.EmissionShape       = ParticleProcessMaterial.EmissionShapeEnum.Box;
		mat.EmissionBoxExtents  = new Vector3(10, _screenH, 0);
		mat.Color               = new Color(1f, 1f, 1f, 0.18f);

		_windParticles.ProcessMaterial = mat;
		_windParticles.Texture         = WindTex;
		_windParticles.Position        = new Vector2(-50, _screenH * 0.5f);
		AddChild(_windParticles);
	}
}
