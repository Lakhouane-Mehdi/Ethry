using Godot;

/// <summary>
/// A world object (tree, rock, herb, etc.) that takes damage from player attacks
/// and drops item pickups when destroyed.
///
/// Supports two modes:
///   1. Simple mode: set DropType/DropMin/DropMax (rocks, herbs, etc.)
///   2. TreeData mode: assign a TreeData resource for full tree behaviour
///      including fruit harvesting, stump after chopping, and regrowth.
///
/// Scene must have:
///   - Sprite2D child
///   - StaticBody2D child (with CollisionShape2D) for physics blocking
///   - HurtBox (Area2D child, with CollisionShape2D) for hit detection
/// </summary>
public partial class ResourceNode : Node2D
{
	[ExportGroup("Simple Mode")]
	[Export] public int Health = 3;
	[Export] public ItemType DropType = ItemType.Wood;
	[Export] public int DropMin = 1;
	[Export] public int DropMax = 3;
	[Export] public PackedScene ItemPickupScene;
	[Export] public string RequiredTool = "";

	[ExportGroup("Tree Mode (optional)")]
	[Export] public TreeData Tree;

	private Sprite2D _sprite;
	private int _maxHealth;

	// Fruit tree state
	private bool _hasFruit;
	private int  _daysSinceHarvest;
	private bool _isStump;
	private bool _playerNear;
	private Label _promptLabel;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		// If TreeData is assigned, use its health
		if (Tree != null)
		{
			Health = Tree.Health;
			DropType = System.Enum.TryParse<ItemType>(Tree.PrimaryDropId, out var t) ? t : ItemType.Wood;
			DropMin = Tree.PrimaryDropMin;
			DropMax = Tree.PrimaryDropMax;
		}

		_maxHealth = Health;

		ApplyVisuals();

		var hurtBox = GetNode<Area2D>("HurtBox");
		hurtBox.CollisionLayer = 0;
		hurtBox.CollisionMask = 2;
		hurtBox.AreaEntered += OnHurtBoxAreaEntered;

		// Fruit tree interaction setup
		if (Tree is { IsFruitTree: true })
		{
			SetupFruitInteraction();

			// Connect to day system for fruit regrowth
			if (DaySystem.Instance != null)
				DaySystem.Instance.DayAdvanced += OnDayAdvanced;

			// Start with fruit if in a valid season
			_hasFruit = IsInFruitSeason();
			_daysSinceHarvest = 0;
			UpdateFruitVisuals();
		}
	}

	// ── Visuals ────────────────────────────────────────────────────────────
	private void ApplyVisuals()
	{
		if (_sprite == null) return;

		// If TreeData has a texture, use it (this overrides any default editor texture)
		if (Tree?.TreeTexture != null)
		{
			_sprite.Texture = Tree.TreeTexture;
			_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		}
		// Final fallback: item icon (only if still no texture)
		else if (_sprite.Texture == null)
		{
			var data = ItemDatabase.Instance?.Get(DropType.ToString());
			if (data?.Icon != null)
			{
				_sprite.Texture = data.Icon;
				_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			}
		}
	}

	private void UpdateFruitVisuals()
	{
		if (_sprite == null || Tree == null) return;

		if (_isStump && Tree.StumpTexture != null)
		{
			_sprite.Texture = Tree.StumpTexture;
		}
		else if (_hasFruit && Tree.FruitTexture != null)
		{
			_sprite.Texture = Tree.FruitTexture;
		}
		else if (Tree.TreeTexture != null)
		{
			_sprite.Texture = Tree.TreeTexture;
		}
	}

	// ── Fruit interaction ──────────────────────────────────────────────────
	private void SetupFruitInteraction()
	{
		// Interaction area for pressing E to harvest fruit
		var area = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape = new CollisionShape2D();
		var circle = new CircleShape2D { Radius = 28f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += b => { if (b.IsInGroup("player")) { _playerNear = true; UpdatePrompt(); } };
		area.BodyExited  += b => { if (b.IsInGroup("player")) { _playerNear = false; if (_promptLabel != null) _promptLabel.Visible = false; } };

		// Floating prompt
		var anchor = new Node2D { TopLevel = true };
		AddChild(anchor);
		var ctrl = new Control();
		anchor.AddChild(ctrl);

		_promptLabel = new Label { Visible = false };
		_promptLabel.AddThemeColorOverride("font_color", Colors.White);
		_promptLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		_promptLabel.AddThemeFontSizeOverride("font_size", 11);
		_promptLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_promptLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		ctrl.AddChild(_promptLabel);

		// Keep prompt above the tree
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (_promptLabel != null && _promptLabel.GetParent() is Control ctrl)
			ctrl.GlobalPosition = GlobalPosition + new Vector2(-50, -40);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_playerNear || _isStump || Tree == null || !Tree.IsFruitTree) return;
		if (!@event.IsActionPressed("interact")) return;

		if (_hasFruit)
		{
			HarvestFruit();
			GetViewport().SetInputAsHandled();
		}
	}

	private void HarvestFruit()
	{
		if (!_hasFruit || Tree == null) return;

		int amount = (int)GD.RandRange(Tree.FruitDropMin, Tree.FruitDropMax + 1);
		if (amount < Tree.FruitDropMin) amount = Tree.FruitDropMin;

		Inventory.Instance.AddItem(Tree.FruitDropId, amount);

		_hasFruit = false;
		_daysSinceHarvest = 0;
		UpdateFruitVisuals();
		UpdatePrompt();
	}

	private void UpdatePrompt()
	{
		if (_promptLabel == null) return;

		if (!_playerNear || _isStump)
		{
			_promptLabel.Visible = false;
			return;
		}

		if (_hasFruit)
		{
			string name = ItemDatabase.Instance?.Get(Tree.FruitDropId)?.DisplayName ?? Tree.FruitDropId;
			_promptLabel.Text = $"[E]  Harvest {name}";
			_promptLabel.Visible = true;
		}
		else
		{
			int daysLeft = Tree.FruitRegrowDays - _daysSinceHarvest;
			if (daysLeft > 0 && IsInFruitSeason())
				_promptLabel.Text = $"{Tree.DisplayName}  ({daysLeft}d until fruit)";
			else if (!IsInFruitSeason())
				_promptLabel.Text = $"{Tree.DisplayName}  (out of season)";
			else
				_promptLabel.Text = Tree.DisplayName;
			_promptLabel.Visible = true;
		}
	}

	// ── Day advance (fruit regrowth) ───────────────────────────────────────
	private void OnDayAdvanced(int day, int season, int year)
	{
		if (_isStump || Tree == null || !Tree.IsFruitTree) return;

		_daysSinceHarvest++;

		if (!_hasFruit && IsInFruitSeason() && _daysSinceHarvest >= Tree.FruitRegrowDays)
		{
			_hasFruit = true;
			UpdateFruitVisuals();
		}
	}

	private bool IsInFruitSeason()
	{
		if (Tree?.FruitSeasons == null || Tree.FruitSeasons.Length == 0) return true;
		int season = DaySystem.Instance?.SeasonIndex ?? 0;
		foreach (int s in Tree.FruitSeasons)
			if (s == season) return true;
		return false;
	}

	// ── Combat damage ──────────────────────────────────────────────────────
	private void OnHurtBoxAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player player)
			TakeDamage(player.AttackDamage, player);
	}

	public void TakeDamage(int damage, Player attacker = null)
	{
		if (Health <= 0 || _isStump) return;

		// Check tool requirement
		if (!string.IsNullOrEmpty(RequiredTool) && attacker != null)
		{
			string equippedTool = attacker.EquippedToolId ?? "";
			if (!equippedTool.ToLower().Contains(RequiredTool.ToLower()))
			{
				string article = "AEIOUaeiou".Contains(RequiredTool[0]) ? "an" : "a";
				NotificationManager.Instance?.ShowInfo($"Need {article} {RequiredTool}!");
				FlashHitWhite(); // Brief white flash for "blocked" hit
				return;
			}
		}

		Health -= damage;
		FlashHit();

		// Trigger screen shake for impact
		attacker?.ShakeCamera(0.12f, Tree != null ? 3.5f : 2.0f);

		// Spawn impact particles
		Color pColor = DropType switch
		{
			ItemType.Stone or ItemType.Coal or ItemType.IronOre or ItemType.GoldOre or ItemType.Crystal => new Color(0.5f, 0.5f, 0.5f), // grey
			_ => new Color(0.45f, 0.3f, 0.15f) // brown/wood
		};
		EffectsManager.Instance?.SpawnImpact(GlobalPosition, pColor, 6);

		if (Health <= 0)
			ChopDown();
	}

	private void FlashHit()
	{
		if (_sprite == null) return;

		_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
		GetTree().CreateTimer(0.12).Timeout += () =>
		{
			if (IsInstanceValid(_sprite))
				_sprite.Modulate = Colors.White;
		};
	}

	private void FlashHitWhite()
	{
		if (_sprite == null) return;

		_sprite.Modulate = new Color(1.5f, 1.5f, 1.5f); // Overbright white
		GetTree().CreateTimer(0.08).Timeout += () =>
		{
			if (IsInstanceValid(_sprite))
				_sprite.Modulate = Colors.White;
		};
	}

	// ── Chopping / Harvest ─────────────────────────────────────────────────
	private void ChopDown()
	{
		// Drop primary resource (wood)
		int amount = (int)GD.RandRange(DropMin, DropMax + 1);
		if (amount < DropMin) amount = DropMin;

		if (ItemPickupScene != null)
		{
			var pickup = ItemPickupScene.Instantiate<ItemPickup>();
			pickup.Type = DropType;
			pickup.Amount = amount;
			pickup.GlobalPosition = GlobalPosition;
			GetParent().CallDeferred(Node.MethodName.AddChild, pickup);
		}
		else
		{
			SpawnDrop(DropType.ToString(), amount);
		}

		// Fruit trees: also drop any remaining fruit, then show stump
		if (Tree is { IsFruitTree: true })
		{
			if (_hasFruit)
			{
				int fruitAmt = (int)GD.RandRange(Tree.FruitDropMin, Tree.FruitDropMax + 1);
				SpawnDrop(Tree.FruitDropId, fruitAmt);
			}

			// Show stump instead of destroying
			if (Tree.StumpTexture != null)
			{
				_isStump = true;
				_hasFruit = false;
				_sprite.Texture = Tree.StumpTexture;

				// Disable the collision so player can walk over stump
				var staticBody = GetNodeOrNull<StaticBody2D>("StaticBody2D");
				if (staticBody != null)
					staticBody.SetDeferred("process_mode", (int)ProcessModeEnum.Disabled);

				UpdatePrompt();
				return;
			}
		}

		QueueFree();
	}

	// ── Spawn helpers ──────────────────────────────────────────────────────
	private void SpawnDrop(string itemId, int amount)
	{
		var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");
		if (pickupScene == null) return;

		var pickup = pickupScene.Instantiate<ItemPickup>();

		// Try to use ItemData from database
		var data = ItemDatabase.Instance?.Get(itemId);
		if (data != null)
			pickup.Data = data;
		else if (System.Enum.TryParse<ItemType>(itemId, out var t))
			pickup.Type = t;

		pickup.Amount = amount;

		float angle  = (float)GD.RandRange(0, Mathf.Tau);
		float radius = (float)GD.RandRange(12, 30);
		pickup.GlobalPosition = GlobalPosition + new Vector2(
			Mathf.Cos(angle) * radius,
			Mathf.Sin(angle) * radius
		);

		GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, pickup);
	}
}
