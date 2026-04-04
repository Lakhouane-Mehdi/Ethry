using Godot;
using FSM;

/// <summary>
/// FarmPlot — a single tillable/plantable/harvestable tile.
/// Refactored to use a StateMachine for modular crop life-cycle management.
/// </summary>
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

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		_stateMachine = GetNode<FSM.StateMachine>("StateMachine");

		BuildPrompt();

		// Detection area
		var area = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 20) } };
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += b => { if (b.IsInGroup("player")) { _playerNear = true; UpdatePrompt(); } };
		area.BodyExited  += b => { if (b.IsInGroup("player")) { _playerNear = false; _promptLabel.Visible = false; } };

		if (WeatherSystem.Instance != null)
		{
			WeatherSystem.Instance.WeatherChanged += OnWeatherChanged;
			if (WeatherSystem.Instance.IsPrecipitating) IsWatered = true;
		}

		// Initial state determination (usually Empty unless pre-seeded in editor)
		_stateMachine.TransitionTo("Empty");
	}

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
		_promptAnchor.GlobalPosition = GlobalPosition + new Vector2(-55, -28);
		_stateMachine?.Update(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		_stateMachine?.HandleInput(@event);
	}

	public void UpdateVisuals()
	{
		if (_sprite == null) return;
		var state = _stateMachine.CurrentState;

		if (state is PlotEmptyState)
		{
			_sprite.Modulate = Colors.White;
		}
		else if (state is PlotTilledState)
		{
			_sprite.Modulate = Colors.White;
			_sprite.Texture  = IsWatered ? WetTexture : DryTexture;
		}
		else if (state is PlotGrowingState)
		{
			_sprite.Modulate = Colors.White;
			if (CurrentCrop != null && CurrentCrop.GrowthStages.Length > 0)
			{
				int idx = Mathf.Min(
					(int)(GrowthDay / (float)CurrentCrop.GrowthDays * CurrentCrop.GrowthStages.Length),
					CurrentCrop.GrowthStages.Length - 1);
				if (CurrentCrop.GrowthStages[idx] != null)
					_sprite.Texture = CurrentCrop.GrowthStages[idx];
			}
		}
		else if (state is PlotMatureState)
		{
			_sprite.Modulate = new Color(0.85f, 1f, 0.65f);
		}
	}

	public void UpdatePrompt()
	{
		if (!_playerNear) { _promptLabel.Visible = false; return; }
		_promptLabel.Visible = true;
		var state = _stateMachine.CurrentState;

		if (state is PlotEmptyState) _promptLabel.Text = "[E]  Till soil";
		else if (state is PlotTilledState) _promptLabel.Text = "[E]  Plant seeds";
		else if (state is PlotGrowingState) _promptLabel.Text = $"[E]  {CurrentCrop?.DisplayName} ({GrowthDay}/{CurrentCrop?.GrowthDays}d)";
		else if (state is PlotMatureState) _promptLabel.Text = $"[E]  Harvest {CurrentCrop?.DisplayName}!";
	}

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
		if (Inventory.Instance.HasItem(seedId))
			Inventory.Instance.RemoveItem(seedId, 1);

		CurrentCrop = crop;
		GrowthDay = 0;
		_stateMachine.TransitionTo("Growing");
		NotificationManager.Instance?.Show($"Planted {crop.DisplayName}!", new Color(0.5f, 0.88f, 0.42f));
	}

	public void Harvest()
	{
		int amount = (int)GD.RandRange(CurrentCrop.HarvestMin, CurrentCrop.HarvestMax + 1);
		Inventory.Instance.AddItem(CurrentCrop.HarvestItemId, amount);

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
		}
	}

	public void GrowInfo()
	{
		NotificationManager.Instance?.Show(
			$"{CurrentCrop.DisplayName}: {GrowthDay}/{CurrentCrop.GrowthDays} days",
			new Color(0.5f, 0.85f, 0.45f));
	}

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
		_promptCtrl = new Control();
		_promptAnchor.AddChild(_promptCtrl);
		_promptLabel = new Label { Visible = false };
		_promptLabel.AddThemeColorOverride("font_color", Colors.White);
		_promptLabel.AddThemeFontSizeOverride("font_size", 11);
		_promptCtrl.AddChild(_promptLabel);
	}
}
