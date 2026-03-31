using Godot;

/// <summary>
/// Dynamic weather system using cute_fantasy weather effect assets.
/// Spawns rain, drifting clouds, and wind particles as CanvasLayer overlays.
/// Attach to any outdoor level scene. Auto-cycles between clear and rain.
/// </summary>
public partial class WeatherSystem : CanvasLayer
{
	[Export] public bool  StartRaining    = false;
	[Export] public float MinClearTime    = 30f;
	[Export] public float MaxClearTime    = 90f;
	[Export] public float MinRainTime     = 15f;
	[Export] public float MaxRainTime     = 40f;
	[Export] public float CloudSpeed      = 20f;
	[Export] public int   CloudCount      = 4;

	private bool  _isRaining;
	private float _weatherTimer;

	// Particle systems
	private GpuParticles2D _rainParticles;
	private Node2D         _cloudLayer;
	private GpuParticles2D _windParticles;

	// Assets
	private static readonly Texture2D RainTex  = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/weather effects/rain_drop.png");
	private static readonly Texture2D CloudTex = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/weather effects/clouds.png");
	private static readonly Texture2D WindTex  = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/weather effects/wind_anim.png");

	// Screen dimensions (adjusted dynamically)
	private Vector2 _screenSize;

	public override void _Ready()
	{
		_screenSize = GetViewport().GetVisibleRect().Size;

		CreateClouds();
		CreateRain();
		CreateWind();

		_isRaining = StartRaining;
		_rainParticles.Emitting  = _isRaining;
		_windParticles.Emitting  = _isRaining;
		_weatherTimer = (float)GD.RandRange(MinClearTime, MaxClearTime);
	}

	public override void _Process(double delta)
	{
		_weatherTimer -= (float)delta;
		if (_weatherTimer <= 0)
			ToggleWeather();

		MoveClouds((float)delta);
	}

	private void ToggleWeather()
	{
		_isRaining = !_isRaining;
		_rainParticles.Emitting = _isRaining;
		_windParticles.Emitting = _isRaining;

		if (_isRaining)
		{
			_weatherTimer = (float)GD.RandRange(MinRainTime, MaxRainTime);
			// Darken clouds during rain
			foreach (Node child in _cloudLayer.GetChildren())
			{
				if (child is Sprite2D spr)
					spr.Modulate = new Color(0.6f, 0.6f, 0.7f, 0.7f);
			}
		}
		else
		{
			_weatherTimer = (float)GD.RandRange(MinClearTime, MaxClearTime);
			foreach (Node child in _cloudLayer.GetChildren())
			{
				if (child is Sprite2D spr)
					spr.Modulate = new Color(1f, 1f, 1f, 0.4f);
			}
		}
	}

	private void CreateClouds()
	{
		_cloudLayer = new Node2D();
		AddChild(_cloudLayer);

		for (int i = 0; i < CloudCount; i++)
		{
			var cloud = new Sprite2D();
			cloud.Texture       = CloudTex;
			cloud.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			cloud.Scale         = new Vector2(2f + (float)GD.RandRange(0, 1.5), 2f + (float)GD.RandRange(0, 1));
			cloud.Modulate      = new Color(1f, 1f, 1f, 0.4f);
			cloud.Position      = new Vector2(
				(float)GD.RandRange(-200, _screenSize.X + 200),
				(float)GD.RandRange(20, _screenSize.Y * 0.3f)
			);
			_cloudLayer.AddChild(cloud);
		}
	}

	private void MoveClouds(float delta)
	{
		foreach (Node child in _cloudLayer.GetChildren())
		{
			if (child is not Sprite2D spr) continue;
			spr.Position += new Vector2(CloudSpeed * delta, 0);

			// Wrap around screen
			if (spr.Position.X > _screenSize.X + 300)
			{
				spr.Position = new Vector2(-300, (float)GD.RandRange(20, _screenSize.Y * 0.3f));
				spr.Scale    = new Vector2(2f + (float)GD.RandRange(0, 1.5), 2f + (float)GD.RandRange(0, 1));
			}
		}
	}

	private void CreateRain()
	{
		_rainParticles = new GpuParticles2D();
		_rainParticles.Amount     = 200;
		_rainParticles.Lifetime   = 1.2f;
		_rainParticles.Preprocess = 0.5f;
		_rainParticles.Emitting   = false;

		var mat = new ParticleProcessMaterial();
		mat.Direction          = new Vector3(0.1f, 1f, 0f);
		mat.Spread             = 5f;
		mat.InitialVelocityMin = 300f;
		mat.InitialVelocityMax = 400f;
		mat.Gravity            = new Vector3(0, 200, 0);
		mat.EmissionShape      = ParticleProcessMaterial.EmissionShapeEnum.Box;
		mat.EmissionBoxExtents = new Vector3(_screenSize.X * 0.6f, 10, 0);
		mat.ScaleMin           = 1.5f;
		mat.ScaleMax           = 2.5f;
		mat.Color              = new Color(0.7f, 0.8f, 1f, 0.6f);

		_rainParticles.ProcessMaterial = mat;
		_rainParticles.Texture         = RainTex;
		_rainParticles.Position        = new Vector2(_screenSize.X / 2, -20);

		AddChild(_rainParticles);
	}

	private void CreateWind()
	{
		_windParticles = new GpuParticles2D();
		_windParticles.Amount     = 15;
		_windParticles.Lifetime   = 2f;
		_windParticles.Emitting   = false;

		var mat = new ParticleProcessMaterial();
		mat.Direction          = new Vector3(1f, 0.1f, 0f);
		mat.Spread             = 15f;
		mat.InitialVelocityMin = 80f;
		mat.InitialVelocityMax = 150f;
		mat.EmissionShape      = ParticleProcessMaterial.EmissionShapeEnum.Box;
		mat.EmissionBoxExtents = new Vector3(10, _screenSize.Y * 0.5f, 0);
		mat.ScaleMin           = 1f;
		mat.ScaleMax           = 2f;
		mat.Color              = new Color(1f, 1f, 1f, 0.3f);

		_windParticles.ProcessMaterial = mat;
		_windParticles.Texture         = WindTex;
		_windParticles.Position        = new Vector2(-50, _screenSize.Y / 2);

		AddChild(_windParticles);
	}
}
