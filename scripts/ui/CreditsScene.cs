using Godot;

/// <summary>
/// Intro credits cutscene — plays at game launch.
/// Black screen with flying swords, walking characters, sparkles, and a
/// clean readable title panel. Auto-advances to main menu; any key skips.
/// </summary>
public partial class CreditsScene : Node
{
	private const string MainMenuPath = "res://scenes/ui/main_menu.tscn";

	private static readonly Texture2D SwordTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/player/player_modular/tools/iron_tools/iron_sword.png");

	// Character spritesheets + their individual frame size (varies per sheet).
	private static readonly (string Path, int Frame)[] CharSheets = new (string, int)[]
	{
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/knights/swordman.png", 48),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/knights/spearman.png", 48),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/knights/archer.png",   48),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/knights/templar.png",  48),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/angels/angel_1.png",   64),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/angels/angel_2.png",   64),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/goblins/goblin_archer.png",   48),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/goblins/goblin_maceman.png",  32),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/goblins/goblin_spearman.png", 32),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/goblins/goblin_thief.png",    32),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/orcs/orc_chief.png",  64),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/orcs/orc_grunt.png",  64),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/orcs/orc_peon.png",   64),
		("res://assets/cute_fantasy_characters/cute_fantasy_characters/orcs/orc_archer.png", 64),
	};

	private Control _root;
	private Control _fxLayer;
	private PanelContainer _textPanel;
	private Label   _title;
	private Label   _author;
	private VBoxContainer _creditsBox;
	private bool _canSkip;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		GetTree().Paused = false;
		_rng.Randomize();
		BuildUI();
		SpawnEffects();
		PlayIntro();
	}

	// ─────────────────────────────────────────────────────────────────────
	private void BuildUI()
	{
		var layer = new CanvasLayer { Layer = 100 };
		AddChild(layer);

		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(_root);

		// Deep-blue/black background with subtle gradient feel
		var bg = new ColorRect { Color = new Color(0.02f, 0.02f, 0.05f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(bg);

		// Layer for background characters / swords / sparkles
		_fxLayer = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_fxLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(_fxLayer);

		// Vignette
		var vignette = new ColorRect { Color = new Color(0, 0, 0, 0.5f) };
		vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(vignette);

		// ── Readable text panel (dark rounded backdrop) ──
		_textPanel = new PanelContainer();
		var style = new StyleBoxFlat
		{
			BgColor          = new Color(0.0f, 0.0f, 0.0f, 0.72f),
			BorderColor      = new Color(0.95f, 0.78f, 0.35f, 0.85f),
			BorderWidthLeft  = 2, BorderWidthRight  = 2,
			BorderWidthTop   = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
			ContentMarginLeft = 40, ContentMarginRight = 40,
			ContentMarginTop  = 28, ContentMarginBottom = 28,
			ShadowColor = new Color(0.95f, 0.78f, 0.35f, 0.25f),
			ShadowSize  = 16,
		};
		_textPanel.AddThemeStyleboxOverride("panel", style);
		_textPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
		_textPanel.OffsetLeft = -320; _textPanel.OffsetRight  = 320;
		_textPanel.OffsetTop  = -220; _textPanel.OffsetBottom = 220;
		_textPanel.Modulate = new Color(1, 1, 1, 0);
		_root.AddChild(_textPanel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		_textPanel.AddChild(vbox);

		// Title
		_title = new Label { Text = "ETHRY" };
		_title.HorizontalAlignment = HorizontalAlignment.Center;
		_title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
		_title.AddThemeFontSizeOverride("font_size", 72);
		_title.AddThemeColorOverride("font_shadow_color", new Color(0.5f, 0.18f, 0.02f, 1f));
		_title.AddThemeConstantOverride("shadow_offset_x", 4);
		_title.AddThemeConstantOverride("shadow_offset_y", 4);
		_title.AddThemeConstantOverride("shadow_outline_size", 2);
		vbox.AddChild(_title);

		// Thin gold divider
		var div = new ColorRect
		{
			Color = new Color(0.95f, 0.78f, 0.35f, 0.6f),
			CustomMinimumSize = new Vector2(0, 2),
		};
		vbox.AddChild(div);

		// Author (prominent)
		_author = new Label { Text = "Made by  Mehdi Lakhouane" };
		_author.HorizontalAlignment = HorizontalAlignment.Center;
		_author.AddThemeColorOverride("font_color", Colors.White);
		_author.AddThemeFontSizeOverride("font_size", 26);
		_author.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		_author.AddThemeConstantOverride("shadow_offset_x", 2);
		_author.AddThemeConstantOverride("shadow_offset_y", 2);
		_author.Modulate = new Color(1, 1, 1, 0);
		vbox.AddChild(_author);

		var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
		vbox.AddChild(spacer);

		// Small credits block
		_creditsBox = new VBoxContainer();
		_creditsBox.AddThemeConstantOverride("separation", 6);
		_creditsBox.Modulate = new Color(1, 1, 1, 0);
		vbox.AddChild(_creditsBox);

		AddCreditLine("Design  ·  Mehdi Lakhouane");
		AddCreditLine("Programming  ·  Mehdi Lakhouane");
		AddCreditLine("Art  ·  Cute Fantasy asset pack");
		AddCreditLine(" ");
		AddCreditLine("Thank you for playing.");

		// Skip hint (outside panel, bottom of screen)
		var hint = new Label { Text = "Press any key to continue" };
		hint.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
		hint.OffsetLeft = -180; hint.OffsetRight = 180;
		hint.OffsetTop = -44;   hint.OffsetBottom = -14;
		hint.HorizontalAlignment = HorizontalAlignment.Center;
		hint.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f, 0.75f));
		hint.AddThemeFontSizeOverride("font_size", 12);
		_root.AddChild(hint);

		// Pulse the hint
		var hintTw = CreateTween();
		hintTw.SetLoops();
		hintTw.TweenProperty(hint, "modulate:a", 0.25f, 1.0);
		hintTw.TweenProperty(hint, "modulate:a", 0.85f, 1.0);
	}

	private void AddCreditLine(string text)
	{
		var lbl = new Label { Text = text };
		lbl.HorizontalAlignment = HorizontalAlignment.Center;
		lbl.AddThemeColorOverride("font_color", new Color(0.92f, 0.88f, 0.75f));
		lbl.AddThemeFontSizeOverride("font_size", 15);
		_creditsBox.AddChild(lbl);
	}

	// ─────────────────────────────────────────────────────────────────────
	private void SpawnEffects()
	{
		var vp = GetViewport().GetVisibleRect().Size;

		SpawnCharacters(14, vp);
		SpawnSwords(3, vp);
		SpawnSparkles(60, vp);
		SpawnRisingOrbs(12, vp);
	}

	private void SpawnSwords(int count, Vector2 vp)
	{
		for (int i = 0; i < count; i++)
		{
			var sprite = new Sprite2D { Texture = SwordTex };
			sprite.Scale = Vector2.One * _rng.RandfRange(1.4f, 2.6f);
			sprite.Modulate = new Color(1f, 0.95f, 0.7f, _rng.RandfRange(0.25f, 0.55f));

			float startX = _rng.RandfRange(-80, vp.X + 80);
			float startY = vp.Y + _rng.RandfRange(40, 260);
			sprite.Position = new Vector2(startX, startY);
			sprite.Rotation = _rng.RandfRange(-0.6f, 0.6f);
			_fxLayer.AddChild(sprite);

			float duration = _rng.RandfRange(7f, 12f);
			float spin     = _rng.RandfRange(-1.5f, 1.5f);
			LoopSwordFlight(sprite, duration, spin, vp);
		}
	}

	private void LoopSwordFlight(Sprite2D sprite, float duration, float spin, Vector2 vp)
	{
		float endX = sprite.Position.X + _rng.RandfRange(-240, 240);
		var tw = CreateTween();
		tw.TweenProperty(sprite, "position", new Vector2(endX, -180), duration);
		tw.Parallel().TweenProperty(sprite, "rotation", sprite.Rotation + spin, duration);
		tw.TweenCallback(Callable.From(() =>
		{
			sprite.Position = new Vector2(_rng.RandfRange(-80, vp.X + 80), vp.Y + _rng.RandfRange(40, 260));
			sprite.Rotation = _rng.RandfRange(-0.6f, 0.6f);
			LoopSwordFlight(sprite, _rng.RandfRange(7f, 12f), _rng.RandfRange(-1.5f, 1.5f), vp);
		}));
	}

	private void SpawnCharacters(int count, Vector2 vp)
	{
		for (int i = 0; i < count; i++)
		{
			var (path, frame) = CharSheets[_rng.RandiRange(0, CharSheets.Length - 1)];
			var tex  = GD.Load<Texture2D>(path);
			if (tex == null) continue;

			var atlas = new AtlasTexture
			{
				Atlas  = tex,
				Region = new Rect2(0, 0, frame, frame),
			};

			var sprite = new Sprite2D { Texture = atlas };
			// Normalize on-screen size across 32/48/64 source frames
			float baseScale = 96f / frame;
			sprite.Scale = Vector2.One * baseScale * _rng.RandfRange(0.85f, 1.35f);
			sprite.Modulate = new Color(1, 1, 1, _rng.RandfRange(0.35f, 0.75f));

			bool leftToRight = _rng.Randf() < 0.5f;
			float y = _rng.RandfRange(60, vp.Y - 60);
			float startX = leftToRight ? -60 : vp.X + 60;
			sprite.Position = new Vector2(startX, y);
			if (!leftToRight) sprite.FlipH = true;

			_fxLayer.AddChild(sprite);
			LoopCharacterWalk(sprite, leftToRight, vp);
		}
	}

	private void LoopCharacterWalk(Sprite2D sprite, bool leftToRight, Vector2 vp)
	{
		float endX = leftToRight ? vp.X + 80 : -80;
		float dur  = _rng.RandfRange(9f, 16f);
		float bobAmp = _rng.RandfRange(3f, 6f);
		float baseY  = sprite.Position.Y;

		var tw = CreateTween();
		tw.TweenProperty(sprite, "position:x", endX, dur);

		// Bobbing walk feel via secondary tween
		var bobTw = CreateTween();
		bobTw.SetLoops();
		bobTw.TweenProperty(sprite, "position:y", baseY - bobAmp, 0.25);
		bobTw.TweenProperty(sprite, "position:y", baseY + bobAmp, 0.25);

		tw.TweenCallback(Callable.From(() =>
		{
			bobTw.Kill();
			bool newDir = _rng.Randf() < 0.5f;
			float newY = _rng.RandfRange(60, vp.Y - 60);
			sprite.Position = new Vector2(newDir ? -60 : vp.X + 60, newY);
			sprite.FlipH = !newDir;
			LoopCharacterWalk(sprite, newDir, vp);
		}));
	}

	private void SpawnSparkles(int count, Vector2 vp)
	{
		for (int i = 0; i < count; i++)
		{
			var dot = new ColorRect
			{
				Color = new Color(1f, 0.95f, 0.6f, 0f),
				Size  = new Vector2(2, 2),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			dot.Position = new Vector2(_rng.RandfRange(0, vp.X), _rng.RandfRange(0, vp.Y));
			_fxLayer.AddChild(dot);
			LoopSparkle(dot, vp);
		}
	}

	private void SpawnRisingOrbs(int count, Vector2 vp)
	{
		for (int i = 0; i < count; i++)
		{
			var orb = new ColorRect
			{
				Color = new Color(1f, 0.85f, 0.45f, 0f),
				Size  = new Vector2(6, 6),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			orb.PivotOffset = orb.Size / 2;
			_fxLayer.AddChild(orb);
			LoopOrbRise(orb, vp, startDelay: _rng.RandfRange(0f, 6f));
		}
	}

	private void LoopOrbRise(ColorRect orb, Vector2 vp, float startDelay)
	{
		float x = _rng.RandfRange(0, vp.X);
		float startY = vp.Y + 20;
		float endY   = _rng.RandfRange(-30, vp.Y * 0.25f);
		float dur    = _rng.RandfRange(5f, 9f);
		float drift  = _rng.RandfRange(-60f, 60f);
		float peakA  = _rng.RandfRange(0.35f, 0.8f);

		orb.Position = new Vector2(x, startY);
		orb.Color = new Color(orb.Color.R, orb.Color.G, orb.Color.B, 0f);

		var tw = CreateTween();
		tw.TweenInterval(startDelay);
		tw.Parallel().TweenProperty(orb, "position", new Vector2(x + drift, endY), dur);
		tw.Parallel().TweenProperty(orb, "color:a", peakA, dur * 0.4);
		tw.Chain().TweenProperty(orb, "color:a", 0f, dur * 0.6);
		tw.TweenCallback(Callable.From(() => LoopOrbRise(orb, vp, 0f)));
	}

	private void LoopSparkle(ColorRect dot, Vector2 vp)
	{
		float peak  = _rng.RandfRange(0.4f, 0.95f);
		float delay = _rng.RandfRange(0f, 3f);
		float dur   = _rng.RandfRange(0.8f, 2.2f);

		var tw = CreateTween();
		tw.TweenInterval(delay);
		tw.TweenProperty(dot, "color:a", peak, dur * 0.5);
		tw.TweenProperty(dot, "color:a", 0f,   dur * 0.5);
		tw.TweenCallback(Callable.From(() =>
		{
			dot.Position = new Vector2(_rng.RandfRange(0, vp.X), _rng.RandfRange(0, vp.Y));
			LoopSparkle(dot, vp);
		}));
	}

	// ─────────────────────────────────────────────────────────────────────
	private void PlayIntro()
	{
		var tw = CreateTween();
		tw.TweenInterval(0.6);
		tw.TweenProperty(_textPanel, "modulate:a", 1.0f, 1.0);
		tw.TweenInterval(0.3);
		tw.TweenProperty(_author,     "modulate:a", 1.0f, 1.0);
		tw.TweenInterval(0.3);
		tw.TweenProperty(_creditsBox, "modulate:a", 1.0f, 1.2);
		tw.TweenCallback(Callable.From(() => _canSkip = true));
		tw.TweenInterval(3.5);
		tw.TweenCallback(Callable.From(() => { if (_canSkip) ReturnToMenu(); }));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_canSkip) return;
		if (@event is InputEventKey k && k.Pressed) ReturnToMenu();
		else if (@event is InputEventMouseButton m && m.Pressed) ReturnToMenu();
	}

	private void ReturnToMenu()
	{
		_canSkip = false;
		var fade = new ColorRect { Color = new Color(0, 0, 0, 0) };
		fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fade.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(fade);

		var tw = CreateTween();
		tw.TweenProperty(fade, "color:a", 1.0f, 0.7);
		tw.TweenCallback(Callable.From(() => GetTree().ChangeSceneToFile(MainMenuPath)));
	}
}
