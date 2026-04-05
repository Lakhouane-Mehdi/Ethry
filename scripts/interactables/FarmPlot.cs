using System.Collections.Generic;
using Godot;
using FSM;

public partial class FarmPlot : Node2D
{
	[Export] public CropData DefaultCrop;

	public CropData  CurrentCrop { get; set; }
	public int       GrowthDay   { get; set; }
	public bool      IsWatered   { get; set; }

	private Sprite2D  _sprite;
	private Label     _promptLabel;
	private Control   _promptCtrl;
	private Node2D    _promptAnchor;
	private bool      _playerNear;
	private FSM.StateMachine _stateMachine;

	[Export] public Texture2D DryTexture;
	[Export] public Texture2D WetTexture;

	// ── Auto-tiling ────────────────────────────────────────────────────────
	private static readonly Texture2D DrySheet =
		GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/tiles/farmland/farmland_tile.png");
	private static readonly Texture2D WetSheet =
		GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/tiles/farmland/farmland_wet_tile.png");

	// Single tile for all plots — clean individual squares like Stardew Valley
	private static readonly AtlasTexture DryTile = new()
		{ Atlas = DrySheet, Region = new Rect2(32, 16, 16, 16) };
	private static readonly AtlasTexture WetTile = new()
		{ Atlas = WetSheet, Region = new Rect2(32, 16, 16, 16) };

	public void RefreshAutoTile(bool notifyNeighbors = true)
	{
		UpdateVisuals();
		if (notifyNeighbors)
		{
			var neighbors = GetNeighborPlots();
			foreach (var n in neighbors) n.RefreshAutoTile(false);
		}
	}

	private List<FarmPlot> GetNeighborPlots()
	{
		var list = new List<FarmPlot>();
		var parent = GetParent();
		if (parent == null) return list;
		foreach (var node in parent.GetChildren())
		{
			if (node is FarmPlot plot && plot != this)
			{
				float d = GlobalPosition.DistanceTo(plot.GlobalPosition);
				if (d < 80f) list.Add(plot);
			}
		}
		return list;
	}

	private Rect2 GetTileRegion(int mask)
	{
		// Bitmask: N=1, E=2, S=4, W=8
		bool n = (mask & 1) != 0;
		bool e = (mask & 2) != 0;
		bool s = (mask & 4) != 0;
		bool w = (mask & 8) != 0;

		// The 3x3 Square set from TileSet editor starts at (16, 0)
		// Col: 0=16, 1=32, 2=48
		// Row: 0=0, 1=16, 2=32

		// Corners of the field
		if (!n && !w && s && e) return new Rect2(16, 0, 16, 16);  // TL
		if (!n && !e && s && w) return new Rect2(48, 0, 16, 16);  // TR
		if (!s && !w && n && e) return new Rect2(16, 32, 16, 16); // BL
		if (!s && !e && n && w) return new Rect2(48, 32, 16, 16); // BR

		// Edges
		if (!n && s) return new Rect2(32, 0, 16, 16);  // Top edge
		if (!s && n) return new Rect2(32, 32, 16, 16); // Bottom edge
		if (!w && e) return new Rect2(16, 16, 16, 16); // Left edge
		if (!e && w) return new Rect2(48, 16, 16, 16); // Right edge

		// Default to center or special standalone (160, 0)
		if (mask == 0) return new Rect2(160, 0, 16, 16); 
		return new Rect2(32, 16, 16, 16); // Center
	}

	// ── Crop spritesheet auto-loading ──────────────────────────────────────
	private static readonly Texture2D CropsTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/crops/crops.png");

	private static readonly Dictionary<string, int> CropRowY = new()
	{
		{ "Carrot",  0   },
		{ "Herb",    176 },
		{ "Berry",   512 },
		{ "Apple",   512 },
		{ "Wheat",   592 },
	};

	private static readonly Dictionary<string, Texture2D[]> _stageCache = new();

	private Texture2D[] GetGrowthStages(CropData crop)
	{
		if (crop.GrowthStages != null && crop.GrowthStages.Length > 0)
			return crop.GrowthStages;
		if (_stageCache.TryGetValue(crop.Id, out var cached))
			return cached;
		if (!CropRowY.TryGetValue(crop.Id, out int y))
			return System.Array.Empty<Texture2D>();

		var stages = new Texture2D[5];
		for (int i = 0; i < 5; i++)
			stages[i] = new AtlasTexture { Atlas = CropsTex, Region = new Rect2((i + 2) * 16, y, 16, 16) };
		_stageCache[crop.Id] = stages;
		return stages;
	}

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		ZAsRelative = false;
		ZIndex = 0;
		YSortEnabled = true;
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		_stateMachine = GetNode<FSM.StateMachine>("StateMachine");

		BuildPrompt();

		var area = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		float parentScale = Scale.X > 0 ? Scale.X : 1f;
		var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(48, 48) / parentScale } };
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += b => { if (b.IsInGroup("player")) { _playerNear = true; UpdatePrompt(); } };
		area.BodyExited  += b => { if (b.IsInGroup("player")) { _playerNear = false; _promptLabel.Visible = false; } };

		if (WeatherSystem.Instance != null)
		{
			WeatherSystem.Instance.WeatherChanged += OnWeatherChanged;
			if (WeatherSystem.Instance.IsPrecipitating) IsWatered = true;
		}

		_stateMachine.TransitionTo("Empty");
		CallDeferred(nameof(DeferredRefresh));
	}

	private void DeferredRefresh() => RefreshAutoTile(true);

	private void OnWeatherChanged(bool isRaining)
	{
		if (isRaining && (_stateMachine.CurrentState is PlotTilledState or PlotGrowingState))
		{
			IsWatered = true;
			UpdateVisuals();
		}
	}

	public override void _Process(double delta)
	{
		_promptAnchor.GlobalPosition = GlobalPosition + new Vector2(-55, -16 * Scale.Y);
		_stateMachine?.Update(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerNear)
			_stateMachine?.HandleInput(@event);
	}

	// ── Visuals ────────────────────────────────────────────────────────────
	public void UpdateVisuals()
	{
		if (_sprite == null) return;
		var state = _stateMachine.CurrentState;

		// Calculate bitmask for current plot
		int mask = 0;
		var parent = GetParent();
		if (parent != null)
		{
			foreach (var node in parent.GetChildren())
			{
				if (node is FarmPlot other && other != this && (other._stateMachine.CurrentState is not PlotEmptyState))
				{
					Vector2 diff = other.GlobalPosition - GlobalPosition;
					if (Mathf.Abs(diff.X) < 10 && diff.Y < -40 && diff.Y > -80) mask |= 1; // N
					if (Mathf.Abs(diff.X) < 10 && diff.Y > 40 && diff.Y < 80) mask |= 4; // S
					if (Mathf.Abs(diff.Y) < 10 && diff.X > 40 && diff.X < 80) mask |= 2; // E
					if (Mathf.Abs(diff.Y) < 10 && diff.X < -40 && diff.X > -80) mask |= 8; // W
				}
			}
		}

		Rect2 region = GetTileRegion(mask);
		 DryTexture = new AtlasTexture { Atlas = DrySheet, Region = region };
		 WetTexture = new AtlasTexture { Atlas = WetSheet, Region = region };

		if (state is PlotEmptyState)
		{
			_sprite.Texture  = DryTexture;
			_sprite.Modulate = Colors.White;
		}
		else if (state is PlotTilledState)
		{
			if (IsWatered && WetTexture != null)
			{
				_sprite.Texture  = WetTexture;
				_sprite.Modulate = Colors.White;
			}
			else
			{
				_sprite.Texture  = DryTexture;
				_sprite.Modulate = IsWatered ? new Color(0.55f, 0.45f, 0.35f) : Colors.White;
			}
		}
		else if (state is PlotGrowingState)
		{
			_sprite.Modulate = Colors.White;
			if (CurrentCrop != null)
			{
				var stages = GetGrowthStages(CurrentCrop);
				if (stages.Length > 0)
				{
					int idx = Mathf.Min(
						(int)(GrowthDay / (float)CurrentCrop.GrowthDays * stages.Length),
						stages.Length - 1);
					if (stages[idx] != null)
						_sprite.Texture = stages[idx];
				}
			}
		}
		else if (state is PlotMatureState)
		{
			if (CurrentCrop != null)
			{
				var stages = GetGrowthStages(CurrentCrop);
				if (stages.Length > 0)
					_sprite.Texture = stages[^1];
			}
			_sprite.Modulate = new Color(0.85f, 1f, 0.65f);
		}
	}

	// ── Prompts ────────────────────────────────────────────────────────────
	public void UpdatePrompt()
	{
		if (!_playerNear) { _promptLabel.Visible = false; return; }
		_promptLabel.Visible = true;
		var state = _stateMachine.CurrentState;

		if (state is PlotEmptyState) _promptLabel.Text = "[E]  Till soil";
		else if (state is PlotTilledState) _promptLabel.Text = "[E]  Plant seeds";
		else if (state is PlotGrowingState)
		{
			string water = IsWatered ? "" : "  (needs water)";
			_promptLabel.Text = $"[E]  {CurrentCrop?.DisplayName} ({GrowthDay}/{CurrentCrop?.GrowthDays}d){water}";
		}
		else if (state is PlotMatureState) _promptLabel.Text = $"[E]  Harvest {CurrentCrop?.DisplayName}!";
	}

	// ── Actions ────────────────────────────────────────────────────────────
	public void TryTill()
	{
		string equipped = Equipment.Instance.GetSlotId(EquipSlot.Weapon);
		if (equipped?.ToLower().Contains("shovel") ?? false)
		{
			_stateMachine.TransitionTo("Tilled");
			NotificationManager.Instance?.Show("Soil tilled!", new Color(0.75f, 0.55f, 0.28f));
		}
		else
		{
			NotificationManager.Instance?.ShowWarning("Equip a Shovel to till this soil.");
		}
	}

	public bool TryWater()
	{
		if (IsWatered) return false;
		string equipped = Equipment.Instance.GetSlotId(EquipSlot.Weapon);
		if (equipped?.ToLower().Contains("wateringcan") ?? false)
		{
			IsWatered = true;
			AudioManager.Instance?.PlaySfx("water_crop");
			NotificationManager.Instance?.Show("Watered!", new Color(0.45f, 0.65f, 0.95f));
			UpdateVisuals();
			return true;
		}
		return false;
	}

	public void TryPlant()
	{
		CropData crop = DefaultCrop ?? FindSeedInInventory();
		if (crop == null)
		{
			NotificationManager.Instance?.ShowWarning("No seeds to plant!");
			return;
		}
		if (!IsCropSeasonValid(crop))
		{
			NotificationManager.Instance?.ShowWarning($"{crop.DisplayName} can't grow this season!");
			return;
		}

		string seedId = crop.Id + "Seed";
		string seedsId = crop.Id + "Seeds";
		if (Inventory.Instance.HasItem(seedId))
			Inventory.Instance.RemoveItem(seedId, 1);
		else if (Inventory.Instance.HasItem(seedsId))
			Inventory.Instance.RemoveItem(seedsId, 1);

		CurrentCrop = crop;
		GrowthDay = 0;
		_stateMachine.TransitionTo("Growing");
		AudioManager.Instance?.PlaySfx("plant_seed");
		NotificationManager.Instance?.Show($"Planted {crop.DisplayName}!", new Color(0.5f, 0.88f, 0.42f));
	}

	public void Harvest()
	{
		int amount = (int)GD.RandRange(CurrentCrop.HarvestMin, CurrentCrop.HarvestMax + 1);
		Inventory.Instance.AddItem(CurrentCrop.HarvestItemId, amount);
		AudioManager.Instance?.PlaySfx("harvest");

		if (CurrentCrop.Regrows)
		{
			GrowthDay = 0;
			_stateMachine.TransitionTo("Growing");
		}
		else
		{
			CurrentCrop = null;
			GrowthDay = 0;
			IsWatered = false;
			_stateMachine.TransitionTo("Tilled");
			RefreshAutoTile();
		}
	}

	public void GrowInfo()
	{
		NotificationManager.Instance?.Show(
			$"{CurrentCrop.DisplayName}: {GrowthDay}/{CurrentCrop.GrowthDays} days",
			new Color(0.5f, 0.85f, 0.45f));
	}

	// ── Helpers ─────────────────────────────────────────────────────────────
	private bool IsCropSeasonValid(CropData crop)
	{
		if (crop.ValidSeasons == null || crop.ValidSeasons.Length == 0) return true;
		int currentSeason = DaySystem.Instance?.SeasonIndex ?? 0;
		foreach (int s in crop.ValidSeasons) if (s == currentSeason) return true;
		return false;
	}

	private CropData FindSeedInInventory()
	{
		foreach (var (id, cnt) in Inventory.Instance.Items)
		{
			if (cnt <= 0) continue;
			if (!id.EndsWith("Seed") && !id.EndsWith("Seeds")) continue;
			string cropId = id;
			if (cropId.EndsWith("Seeds")) cropId = cropId[..^5];
			else if (cropId.EndsWith("Seed")) cropId = cropId[..^4];

			string[] paths = {
				$"res://data/crops/{cropId.ToLower()}.tres",
				$"res://data/crops/{cropId}.tres",
			};
			foreach (string path in paths)
			{
				if (!ResourceLoader.Exists(path)) continue;
				var crop = GD.Load<CropData>(path);
				if (crop != null) return crop;
			}
		}
		return null;
	}

	public override void _ExitTree()
	{
		if (WeatherSystem.Instance != null)
			WeatherSystem.Instance.WeatherChanged -= OnWeatherChanged;
	}

	private void BuildPrompt()
	{
		_promptAnchor = new Node2D { TopLevel = true };
		AddChild(_promptAnchor);
		_promptCtrl = new Control();
		_promptAnchor.AddChild(_promptCtrl);
		_promptLabel = new Label { Visible = false };
		_promptLabel.AddThemeColorOverride("font_color", Colors.White);
		_promptLabel.AddThemeFontSizeOverride("font_size", 11);
		_promptCtrl.AddChild(_promptLabel);
	}
}
