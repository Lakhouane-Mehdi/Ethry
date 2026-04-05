using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload singleton for playing sound effects and background music.
/// SFX are loaded from res://assets/audio/sfx/ and music from res://assets/audio/music/.
/// Supports pooled one-shot SFX, looping music with crossfade, and volume control.
/// </summary>
public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	// ── Audio buses ────────────────────────────────────────────────────────
	private const string MasterBus = "Master";
	private const string SfxBus    = "SFX";
	private const string MusicBus  = "Music";

	// ── SFX pool ───────────────────────────────────────────────────────────
	[Export(PropertyHint.Range, "1,32,1")]
	private int _sfxPoolSize = 12;
	private AudioStreamPlayer[] _sfxPool;
	private int _sfxIndex;

	// ── Music players (two for crossfade) ──────────────────────────────────
	private AudioStreamPlayer _musicA;
	private AudioStreamPlayer _musicB;
	private AudioStreamPlayer _activeMusic;
	private Tween _crossfadeTween;
	private string _currentMusicPath;
	[Export(PropertyHint.Range, "0.1,5.0,0.1")]
	private float _crossfadeDuration = 1.5f;

	// ── Cache ──────────────────────────────────────────────────────────────
	private readonly Dictionary<string, AudioStream> _cache = new();

	// ── Volume (linear 0-1) ────────────────────────────────────────────────
	[ExportGroup("Volume")]
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	private float _sfxVolume = 0.35f;
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	private float _musicVolume = 0.2f;

	// ── SFX keys — map logical names to file paths ─────────────────────────
	// These will be resolved at runtime from res://assets/audio/sfx/
	// Supported formats: .wav, .ogg, .mp3
	private static readonly Dictionary<string, string> SfxMap = new()
	{
		// Player
		["player_attack"]   = "sfx/player_attack",
		["player_hurt"]     = "sfx/player_hurt",
		["player_death"]    = "sfx/player_death",
		["player_footstep"] = "sfx/player_footstep",

		// Combat
		["hit_enemy"]       = "sfx/hit_enemy",
		["enemy_death"]     = "sfx/enemy_death",
		["explosion"]       = "sfx/explosion",

		// Tools & Resources
		["chop_wood"]       = "sfx/chop_wood",
		["mine_rock"]       = "sfx/mine_rock",
		["till_soil"]       = "sfx/till_soil",
		["harvest"]         = "sfx/harvest",
		["plant_seed"]      = "sfx/plant_seed",
		["water_crop"]      = "sfx/water_crop",

		// Items & Inventory
		["item_pickup"]     = "sfx/item_pickup",
		["item_equip"]      = "sfx/item_equip",
		["craft_success"]   = "sfx/craft_success",
		["buy_item"]        = "sfx/buy_item",
		["sell_item"]       = "sfx/sell_item",

		// Interactables
		["chest_open"]      = "sfx/chest_open",
		["chest_close"]     = "sfx/chest_close",
		["door_open"]       = "sfx/door_open",

		// UI
		["ui_click"]        = "sfx/ui_click",
		["ui_cancel"]       = "sfx/ui_cancel",
		["ui_navigate"]     = "sfx/ui_navigate",

		// World
		["sleep"]           = "sfx/sleep",
		["eat"]             = "sfx/eat",
		["notification"]    = "sfx/notification",
	};

	// ── Music zone mapping (scene name → music file) ──────────────────────
	// Place music files in res://assets/audio/music/ with these names.
	// The system tries the file with common extensions (.ogg, .wav, .mp3).
	private static readonly Dictionary<string, string> MusicZones = new()
	{
		["base_level"]      = "overworld",
		["level_1"]         = "overworld",
		["level_2"]         = "exploration",
		["level_3"]         = "exploration",
		["house_interior"]  = "indoor",
		["world"]           = "overworld",
	};

	// Time-based music overrides (checked before zone)
	[ExportGroup("Music Timing")]
	[Export] private string _nightMusic = "night";
	[Export(PropertyHint.Range, "0,23,1")] private int _nightHour = 20;
	[Export(PropertyHint.Range, "0,23,1")] private int _dawnHour  = 6;

	private string _lastZoneMusic;

	// ── Lifecycle ──────────────────────────────────────────────────────────

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		EnsureAudioBuses();

		// Create SFX pool
		_sfxPool = new AudioStreamPlayer[_sfxPoolSize];
		for (int i = 0; i < _sfxPoolSize; i++)
		{
			_sfxPool[i] = new AudioStreamPlayer { Bus = SfxBus };
			AddChild(_sfxPool[i]);
		}

		// Create music players
		_musicA = new AudioStreamPlayer { Bus = MusicBus, VolumeDb = LinearToDb(_musicVolume) };
		_musicB = new AudioStreamPlayer { Bus = MusicBus, VolumeDb = Mathf.LinearToDb(0f) };
		AddChild(_musicA);
		AddChild(_musicB);
		_activeMusic = _musicA;

		// Check music zone every few seconds
		var timer = new Timer { WaitTime = 3.0, Autostart = true };
		timer.Timeout += UpdateMusicZone;
		AddChild(timer);

		// Trigger first music check after scene is loaded
		GetTree().CreateTimer(1.0).Timeout += UpdateMusicZone;

		// Also update on scene change
		GetTree().TreeChanged += OnTreeChanged;
	}

	private float _sceneChangeDebounce;
	private void OnTreeChanged()
	{
		// Debounce — tree changes fire many times during a scene load
		_sceneChangeDebounce = 0.5f;
	}

	public override void _Process(double delta)
	{
		if (_sceneChangeDebounce > 0)
		{
			_sceneChangeDebounce -= (float)delta;
			if (_sceneChangeDebounce <= 0)
				UpdateMusicZone();
		}
	}

	private void UpdateMusicZone()
	{
		// Determine what music should be playing
		string targetMusic = null;

		// Check time of day for night music
		int hour = (int)(DaySystem.Instance?.Hour ?? 12);
		bool isNight = hour >= _nightHour || hour < _dawnHour;

		if (isNight)
		{
			targetMusic = _nightMusic;
		}
		else
		{
			// Check scene-based zone
			string sceneName = GetTree().CurrentScene?.SceneFilePath ?? "";
			sceneName = sceneName.GetFile().GetBaseName();

			if (MusicZones.TryGetValue(sceneName, out var zoneName))
				targetMusic = zoneName;
		}

		if (targetMusic == null || targetMusic == _lastZoneMusic)
			return;

		_lastZoneMusic = targetMusic;

		// Try to resolve the music file
		string basePath = $"res://assets/audio/music/{targetMusic}";
		foreach (string ext in new[] { ".ogg", ".wav", ".mp3" })
		{
			string full = basePath + ext;
			if (ResourceLoader.Exists(full) || FileAccess.FileExists(full))
			{
				GD.Print($"AudioManager: Playing music — {full}");
				PlayMusic(full);
				return;
			}
		}
		GD.Print($"AudioManager: No music file found for '{targetMusic}'");
	}

	// ── Public SFX API ─────────────────────────────────────────────────────

	// Per-SFX volume multipliers for sounds that need to be quieter
	private static readonly Dictionary<string, float> SfxVolumeScale = new()
	{
		["player_footstep"] = 0.7f,
		["ui_navigate"]     = 0.5f,
	};

	/// <summary>Play a named sound effect. Returns false if the sound file wasn't found.</summary>
	public bool PlaySfx(string name, float pitchVariation = 0.1f)
	{
		var stream = ResolveSfx(name);
		if (stream == null) return false;

		var player = _sfxPool[_sfxIndex];
		_sfxIndex = (_sfxIndex + 1) % _sfxPoolSize;

		float vol = _sfxVolume;
		if (SfxVolumeScale.TryGetValue(name, out float scale))
			vol *= scale;

		player.Stream   = stream;
		player.VolumeDb = LinearToDb(vol);
		player.PitchScale = 1f + (float)GD.RandRange(-pitchVariation, pitchVariation);
		player.Play();
		return true;
	}

	/// <summary>Play SFX with no pitch variation.</summary>
	public bool PlaySfxFlat(string name)
		=> PlaySfx(name, 0f);

	// ── Public Music API ───────────────────────────────────────────────────

	/// <summary>Play background music with crossfade. Pass null or "" to stop.</summary>
	public void PlayMusic(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			StopMusic();
			return;
		}

		string fullPath = path.StartsWith("res://") ? path : $"res://assets/audio/music/{path}";

		if (fullPath == _currentMusicPath && _activeMusic.Playing)
			return;

		var stream = LoadStream(fullPath);
		if (stream == null)
		{
			GD.PrintErr($"AudioManager: Music not found at {fullPath}");
			return;
		}

		_currentMusicPath = fullPath;

		// Crossfade
		var outgoing = _activeMusic;
		var incoming = outgoing == _musicA ? _musicB : _musicA;
		incoming.Stream   = stream;
		incoming.VolumeDb = Mathf.LinearToDb(0.001f);
		incoming.Play();

		_crossfadeTween?.Kill();
		_crossfadeTween = CreateTween().SetIgnoreTimeScale();
		_crossfadeTween.SetParallel(true);
		_crossfadeTween.TweenProperty(incoming, "volume_db", LinearToDb(_musicVolume), _crossfadeDuration);
		_crossfadeTween.TweenProperty(outgoing, "volume_db", Mathf.LinearToDb(0.001f), _crossfadeDuration);
		_crossfadeTween.Chain().TweenCallback(Callable.From(() => outgoing.Stop()));

		_activeMusic = incoming;
	}

	/// <summary>Stop all music with fade out.</summary>
	public void StopMusic(float fadeTime = 1f)
	{
		_currentMusicPath = null;
		_crossfadeTween?.Kill();

		if (_activeMusic.Playing)
		{
			var tween = CreateTween().SetIgnoreTimeScale();
			var player = _activeMusic;
			tween.TweenProperty(player, "volume_db", Mathf.LinearToDb(0.001f), fadeTime);
			tween.TweenCallback(Callable.From(() => player.Stop()));
		}
	}

	// ── Volume control ─────────────────────────────────────────────────────

	public float SfxVolume
	{
		get => _sfxVolume;
		set => _sfxVolume = Mathf.Clamp(value, 0f, 1f);
	}

	public float MusicVolume
	{
		get => _musicVolume;
		set
		{
			_musicVolume = Mathf.Clamp(value, 0f, 1f);
			if (_activeMusic?.Playing == true)
				_activeMusic.VolumeDb = LinearToDb(_musicVolume);
		}
	}

	// ── Internal helpers ───────────────────────────────────────────────────

	private AudioStream ResolveSfx(string name)
	{
		// Try mapped name first
		string basePath;
		if (SfxMap.TryGetValue(name, out var mapped))
			basePath = $"res://assets/audio/{mapped}";
		else
			basePath = $"res://assets/audio/sfx/{name}";

		// Try common extensions
		foreach (string ext in new[] { ".wav", ".ogg", ".mp3" })
		{
			string full = basePath + ext;
			var stream = LoadStream(full);
			if (stream != null) return stream;
		}

		return null;
	}

	private AudioStream LoadStream(string path)
	{
		if (_cache.TryGetValue(path, out var cached))
			return cached;

		if (!ResourceLoader.Exists(path))
			return null;

		var stream = GD.Load<AudioStream>(path);
		if (stream != null)
			_cache[path] = stream;
		return stream;
	}

	private static float LinearToDb(float linear)
		=> Mathf.LinearToDb(Mathf.Max(linear, 0.001f));

	private void EnsureAudioBuses()
	{
		// Add SFX and Music buses if they don't exist
		if (AudioServer.GetBusIndex(SfxBus) == -1)
		{
			AudioServer.AddBus();
			int idx = AudioServer.BusCount - 1;
			AudioServer.SetBusName(idx, SfxBus);
			AudioServer.SetBusSend(idx, MasterBus);
		}

		if (AudioServer.GetBusIndex(MusicBus) == -1)
		{
			AudioServer.AddBus();
			int idx = AudioServer.BusCount - 1;
			AudioServer.SetBusName(idx, MusicBus);
			AudioServer.SetBusSend(idx, MasterBus);
		}
	}
}
