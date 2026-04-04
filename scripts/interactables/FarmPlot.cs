using Godot;

/// <summary>
/// FarmPlot — a single tillable/plantable/harvestable tile.
/// Place farm_plot.tscn in your farm scene. Interact with [E].
/// Behaviour depends on what the player has equipped/in inventory.
/// </summary>
public partial class FarmPlot : Node2D
{
	[Export] public CropData DefaultCrop;  // optional: forces this crop to always be used

	// ── State ──────────────────────────────────────────────────────────────
	public enum PlotState { Empty, Tilled, Growing, Mature }

	public PlotState State       { get; private set; } = PlotState.Empty;
	public CropData  CurrentCrop { get; private set; }
	public int       GrowthDay   { get; private set; }  // days since planting
	public bool      IsWatered   { get; private set; }

	// ── Visuals ────────────────────────────────────────────────────────────
	private Sprite2D  _sprite;
	private Label     _promptLabel;
	private Control   _promptCtrl;
	private Node2D    _promptAnchor;
	private bool      _playerNear;

	// Tilled soil tint (brown)
	private static readonly Color TilledColor  = new(0.62f, 0.42f, 0.22f, 1f);
	private static readonly Color WateredColor = new(0.38f, 0.28f, 0.18f, 1f);

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		BuildPrompt();

		// Detection area
		var area   = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape  = new CollisionShape2D();
		var rect   = new RectangleShape2D { Size = new Vector2(20, 20) };
		shape.Shape = rect;
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += b => { if (b.IsInGroup("player")) { _playerNear = true;  UpdatePrompt(); } };
		area.BodyExited  += b => { if (b.IsInGroup("player")) { _playerNear = false; _promptLabel.Visible = false; } };

		// Connect to day system
		if (DaySystem.Instance != null)
			DaySystem.Instance.DayAdvanced += OnDayAdvanced;

		// Connect to weather system for auto-watering
		if (WeatherSystem.Instance != null)
		{
			WeatherSystem.Instance.WeatherChanged += OnWeatherChanged;
			if (WeatherSystem.Instance.IsPrecipitating) IsWatered = true;
		}

		UpdateVisuals();
	}

	private void OnWeatherChanged(bool isRaining)
	{
		if (isRaining && (State == PlotState.Tilled || State == PlotState.Growing))
		{
			IsWatered = true;
			UpdateVisuals();
		}
	}

	public override void _Process(double delta)
	{
		_promptAnchor.GlobalPosition = GlobalPosition + new Vector2(-55, -28);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_playerNear || !@event.IsActionPressed("interact")) return;
		Interact();
		GetViewport().SetInputAsHandled();
	}

	// ── Interaction ────────────────────────────────────────────────────────
	private void Interact()
	{
		switch (State)
		{
			case PlotState.Empty:
				TryTill();
				break;

			case PlotState.Tilled:
				if (!TryWater()) TryPlant();
				break;

			case PlotState.Growing:
				NotificationManager.Instance?.Show(
					$"{CurrentCrop.DisplayName}: {GrowthDay}/{CurrentCrop.GrowthDays} days",
					new Color(0.5f, 0.85f, 0.45f));
				break;

			case PlotState.Mature:
				Harvest();
				break;
		}
		UpdateVisuals();
		UpdatePrompt();
	}

	private void TryTill()
	{
		// Requires Shovel equipped
		string equipped = Equipment.Instance.GetSlotId(EquipSlot.Weapon);
		if (equipped is "Shovel" or "shovel" ||
		    (ItemDatabase.Instance?.Get(equipped)?.DisplayName?.ToLower().Contains("shovel") ?? false))
		{
			State = PlotState.Tilled;
			NotificationManager.Instance?.Show("Soil tilled!", new Color(0.75f, 0.55f, 0.28f));
		}
		else
		{
			NotificationManager.Instance?.ShowWarning("Equip a Shovel to till this soil.");
		}
	}

	private bool TryWater()
	{
		if (IsWatered) return false;
		string equipped = Equipment.Instance.GetSlotId(EquipSlot.Weapon);
		// Watering can check (we'll add a "WateringCan" item later)
		bool hasWateringCan = equipped is "WateringCan" or "Watering Can" ||
		                      (ItemDatabase.Instance?.Get(equipped)?.DisplayName?.ToLower().Contains("water") ?? false);
		if (!hasWateringCan) return false;

		IsWatered = true;
		NotificationManager.Instance?.Show("Watered!", new Color(0.45f, 0.65f, 0.95f));
		return true;
	}

	private void TryPlant()
	{
		// Use DefaultCrop if set, otherwise look for the first crop seed in inventory
		CropData crop = DefaultCrop ?? FindSeedInInventory();
		if (crop == null)
		{
			NotificationManager.Instance?.ShowWarning("No seeds to plant!");
			return;
		}

		// Check season
		bool validSeason = false;
		if (crop.ValidSeasons == null || crop.ValidSeasons.Length == 0) validSeason = true;
		else foreach (int s in crop.ValidSeasons) if (s == DaySystem.Instance?.SeasonIndex) { validSeason = true; break; }

		if (!validSeason)
		{
			NotificationManager.Instance?.ShowWarning($"{crop.DisplayName} can't grow this season!");
			return;
		}

		// Remove seed from inventory (look for a seed item matching the crop)
		string seedId = crop.Id + "Seed";
		if (Inventory.Instance.HasItem(seedId))
			Inventory.Instance.RemoveItem(seedId, 1);
		// If no matching seed item, still plant (for DefaultCrop playtesting)

		CurrentCrop = crop;
		GrowthDay   = 0;
		State       = PlotState.Growing;
		NotificationManager.Instance?.Show($"Planted {crop.DisplayName}!", new Color(0.5f, 0.88f, 0.42f));
	}

	private void Harvest()
	{
		int amount = (int)GD.RandRange(CurrentCrop.HarvestMin, CurrentCrop.HarvestMax + 1);
		Inventory.Instance.AddItem(CurrentCrop.HarvestItemId, amount);

		if (CurrentCrop.Regrows)
		{
			GrowthDay = 0;
			State     = PlotState.Growing;
		}
		else
		{
			State       = PlotState.Tilled;
			CurrentCrop = null;
			GrowthDay   = 0;
		}
		IsWatered = false;
	}

	// ── Day advance ────────────────────────────────────────────────────────
	private void OnDayAdvanced(int day, int season, int year)
	{
		if (State == PlotState.Growing && CurrentCrop != null)
		{
			// Grow each day (watering doubles speed in future; for now just always grow)
			GrowthDay++;
			if (GrowthDay >= CurrentCrop.GrowthDays)
				State = PlotState.Mature;
		}
		IsWatered = false; // reset watering each morning
		UpdateVisuals();
	}

	// ── Visuals / Prompt ───────────────────────────────────────────────────
	private void UpdateVisuals()
	{
		if (_sprite == null) return;

		switch (State)
		{
			case PlotState.Empty:
				_sprite.Modulate = Colors.White;
				break;
			case PlotState.Tilled:
				_sprite.Modulate = IsWatered ? WateredColor : TilledColor;
				break;
			case PlotState.Growing:
				_sprite.Modulate = Colors.White;
				// Show growth stage sprite if available
				int idx = Mathf.Min(
					(int)(GrowthDay / (float)CurrentCrop.GrowthDays * CurrentCrop.GrowthStages.Length),
					CurrentCrop.GrowthStages.Length - 1);
				if (CurrentCrop.GrowthStages.Length > 0 && CurrentCrop.GrowthStages[idx] != null)
					_sprite.Texture = CurrentCrop.GrowthStages[idx];
				break;
			case PlotState.Mature:
				_sprite.Modulate = new Color(0.85f, 1f, 0.65f);
				break;
		}
	}

	private void UpdatePrompt()
	{
		if (!_playerNear) { _promptLabel.Visible = false; return; }
		_promptLabel.Visible = true;
		_promptLabel.Text = State switch
		{
			PlotState.Empty   => "[E]  Till soil",
			PlotState.Tilled  => "[E]  Plant seeds",
			PlotState.Growing => $"[E]  {CurrentCrop?.DisplayName} ({GrowthDay}/{CurrentCrop?.GrowthDays}d)",
			PlotState.Mature  => $"[E]  Harvest {CurrentCrop?.DisplayName}!",
			_                 => ""
		};
	}

	private CropData FindSeedInInventory()
	{
		// Search inventory for any item ending in "Seed" and find matching CropData
		foreach (var (id, cnt) in Inventory.Instance.Items)
		{
			if (cnt <= 0) continue;
			if (!id.EndsWith("Seed") && !id.EndsWith("Seeds")) continue;

			// Try to find a CropData resource matching this seed
			string cropId = id.Replace("Seeds", "").Replace("Seed", "");
			string cropPath = $"res://data/crops/{cropId.ToLower()}.tres";
			if (ResourceLoader.Exists(cropPath))
			{
				var crop = GD.Load<CropData>(cropPath);
				if (crop != null) return crop;
			}
		}
		return null;
	}

	private void BuildPrompt()
	{
		_promptAnchor = new Node2D { TopLevel = true };
		AddChild(_promptAnchor);
		_promptCtrl   = new Control();
		_promptAnchor.AddChild(_promptCtrl);

		_promptLabel = new Label { Visible = false };
		_promptLabel.AddThemeColorOverride("font_color", Colors.White);
		_promptLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		_promptLabel.AddThemeFontSizeOverride("font_size", 11);
		_promptLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_promptLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		_promptCtrl.AddChild(_promptLabel);
	}
}
