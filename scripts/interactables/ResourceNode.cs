using Godot;
using FSM;

/// <summary>
/// A world object (tree, rock, herb, etc.) that takes damage from player attacks
/// and drops item pickups when destroyed. Now refactored to use a StateMachine.
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
	[Export] public bool IsForageable = false;

	[ExportGroup("Tree Mode (optional)")]
	[Export] public TreeData Tree;

	private Sprite2D _sprite;
	private int _maxHealth;
	private FSM.StateMachine _stateMachine;

	// Shared state accessible by FSM
	public bool HasFruit;
	public int DaysSinceHarvest;
	public bool IsStump;
	public bool PlayerNear;

	public int MaxHealth => _maxHealth;
	public Sprite2D Sprite => _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		_stateMachine = GetNode<FSM.StateMachine>("StateMachine");

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
		hurtBox.AreaEntered += OnHurtBoxAreaEntered;

		SetupInteraction();

		if (Tree is { IsFruitTree: true } || IsForageable)
		{
			if (Tree != null)
			{
				HasFruit = IsInFruitSeason();
				DaysSinceHarvest = 0;
			}
			else
			{
				HasFruit = true; // Forageables are always "fruitful" by default
			}
			
			if (HasFruit)
				_stateMachine.TransitionTo("Harvestable");
			else
				_stateMachine.TransitionTo("Healthy");
		}
		else
		{
			_stateMachine.TransitionTo("Healthy");
		}
	}

	public void ApplyVisuals()
	{
		if (_sprite == null) return;
		if (Tree?.TreeTexture != null)
		{
			_sprite.Texture = Tree.TreeTexture;
			_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		}
		else if (_sprite.Texture == null)
		{
			var data = ItemDatabase.Instance?.Get(DropType.ToString());
			if (data?.Icon != null)
				_sprite.Texture = data.Icon;
		}
	}

	public void UpdateVisuals()
	{
		if (_sprite == null || Tree == null) return;

		if (IsStump && Tree.StumpTexture != null)
			_sprite.Texture = Tree.StumpTexture;
		else if (HasFruit && Tree.FruitTexture != null)
			_sprite.Texture = Tree.FruitTexture;
		else if (Tree.TreeTexture != null)
			_sprite.Texture = Tree.TreeTexture;
	}

	private void SetupInteraction()
	{
		var area = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = 32f } };
		area.AddChild(shape);
		AddChild(area);
		
		area.BodyEntered += OnInteractionBodyEntered;
		area.BodyExited  += OnInteractionBodyExited;
	}

	private void OnInteractionBodyEntered(Node2D body)
	{
		if (!GodotObject.IsInstanceValid(body)) return;
		if (!body.IsInGroup("player")) return;

		PlayerNear = true;
		UpdatePrompt();

		if (HasFruit) return;
		if (string.IsNullOrEmpty(RequiredTool)) return;

		string equipped = Equipment.Instance?.GetSlotId(EquipSlot.Weapon) ?? "";
		string reqLower = RequiredTool.ToLower();
		
		if (!equipped.ToLower().Contains(reqLower))
		{
			string itemName = "item";
			if (Tree != null && !string.IsNullOrEmpty(Tree.DisplayName))
				itemName = Tree.DisplayName;
			else
				itemName = DropType.ToString();

			NotificationManager.Instance?.ShowInfo($"Need {RequiredTool} to harvest {itemName}");
		}
	}

	private void OnInteractionBodyExited(Node2D body)
	{
		if (!GodotObject.IsInstanceValid(body)) return;
		if (!body.IsInGroup("player")) return;

		PlayerNear = false;
		NotificationManager.Instance?.ClearActionPrompt();
	}

	public void UpdatePrompt()
	{
		if (!PlayerNear || IsStump) return;

		string itemName = Tree?.DisplayName ?? DropType.ToString();
		string equipped = Equipment.Instance?.GetSlotId(EquipSlot.Weapon) ?? "";
		
		string promptText = "";
		Color promptColor = Colors.White;

		if (HasFruit)
		{
			string fruitName = Tree != null ? (ItemDatabase.Instance?.Get(Tree.FruitDropId)?.DisplayName ?? Tree.FruitDropId) : itemName;
			promptText = $"[E]  Harvest {fruitName}";
		}
		else if (Tree is { IsFruitTree: true } && !IsStump && !IsInFruitSeason())
		{
			string seasonList = GetFruitSeasonNames();
			promptText = $"{itemName} — Not in season ({seasonList})";
			promptColor = new Color(1f, 0.88f, 0.4f); // Yellow/gold
		}
		else if (!string.IsNullOrEmpty(RequiredTool))
		{
			bool hasTool = equipped.ToLower().Contains(RequiredTool.ToLower());
			if (hasTool)
			{
				string action = RequiredTool.ToLower().Contains("axe") ? "Chop" : "Mine";
				promptText = $"[Attack]  {action} {itemName}";
			}
			else
			{
				promptText = $"Need {RequiredTool} for {itemName}";
				promptColor = new Color(1, 0.4f, 0.4f); // Light red
			}
		}
		else if (IsForageable)
		{
			promptText = $"[E]  Harvest {itemName}";
		}

		if (!string.IsNullOrEmpty(promptText))
			NotificationManager.Instance?.SetActionPrompt(promptText, promptColor);
	}

	public override void _Process(double delta)
	{
		_stateMachine?.Update(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		_stateMachine?.HandleInput(@event);
	}

	public void HarvestFruit()
	{
		if (!HasFruit) return;

		if (Tree != null)
		{
			int amount = (int)GD.RandRange(Tree.FruitDropMin, Tree.FruitDropMax + 1);
			Inventory.Instance.AddItem(Tree.FruitDropId, amount);
			HasFruit = false;
			DaysSinceHarvest = 0;
			_stateMachine.TransitionTo("Regrowing");
		}
		else if (IsForageable)
		{
			int amount = (int)GD.RandRange(DropMin, DropMax + 1);
			Inventory.Instance.AddItem(DropType.ToString(), amount);
			QueueFree(); // Forageables like herbs disappear after harvest
		}

		UpdateVisuals();
		UpdatePrompt();
	}

	public bool IsInFruitSeason()
	{
		if (Tree?.FruitSeasons == null || Tree.FruitSeasons.Length == 0) return true;
		int season = DaySystem.Instance?.SeasonIndex ?? 0;
		foreach (int s in Tree.FruitSeasons)
			if (s == season) return true;
		return false;
	}

	public string GetFruitSeasonNames()
	{
		if (Tree?.FruitSeasons == null || Tree.FruitSeasons.Length == 0) return "";
		var names = new System.Collections.Generic.List<string>();
		foreach (int s in Tree.FruitSeasons)
			if (s >= 0 && s < DaySystem.SeasonNames.Length)
				names.Add(DaySystem.SeasonNames[s]);
		return string.Join(", ", names);
	}

	private void OnHurtBoxAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player player)
			TakeDamage(player.AttackDamage, player);
	}

	public void TakeDamage(int damage, Player attacker = null)
	{
		// Delegate to the current state (Harvestable first since it inherits from Healthy)
		if (_stateMachine.CurrentState is ResourceHarvestableState harvestable)
			harvestable.TakeDamage(damage, attacker);
		else if (_stateMachine.CurrentState is ResourceHealthyState healthy)
			healthy.TakeDamage(damage, attacker);
	}

	public void PerformDamage(int damage, Player attacker)
	{
		if (!string.IsNullOrEmpty(RequiredTool) && attacker != null)
		{
			string equippedTool = attacker.EquippedToolId ?? "";
			if (!equippedTool.ToLower().Contains(RequiredTool.ToLower()))
			{
				string itemName = Tree?.DisplayName ?? DropType.ToString();
				string msg = $"Need {RequiredTool} to harvest {itemName}!";
				NotificationManager.Instance?.ShowInfo(msg);
				FlashHitWhite();
				return;
			}
		}

		Health -= damage;
		FlashHit();
		attacker?.ShakeCamera(0.12f, Tree != null ? 3.5f : 2.0f);
		
		Color pColor = DropType switch
		{
			ItemType.Stone or ItemType.Crystal => new Color(0.5f, 0.5f, 0.5f),
			_ => new Color(0.45f, 0.3f, 0.15f)
		};
		EffectsManager.Instance?.SpawnImpact(GlobalPosition, pColor, 6);
	}

	public void ChopDown()
	{
		int amount = (int)GD.RandRange(DropMin, DropMax + 1);
		SpawnDrop(DropType.ToString(), amount);

		if (Tree is { IsFruitTree: true } && Tree.StumpTexture != null)
		{
			if (HasFruit)
			{
				int fruitAmt = (int)GD.RandRange(Tree.FruitDropMin, Tree.FruitDropMax + 1);
				SpawnDrop(Tree.FruitDropId, fruitAmt);
			}

			IsStump = true;
			HasFruit = false;
			UpdateVisuals();
			var staticBody = GetNodeOrNull<StaticBody2D>("StaticBody2D");
			if (staticBody != null) staticBody.ProcessMode = ProcessModeEnum.Disabled;
			UpdatePrompt();
			_stateMachine.TransitionTo("Stump");
		}
		else
		{
			QueueFree();
		}
	}

	private void FlashHit()
	{
		if (_sprite == null) return;
		_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
		GetTree().CreateTimer(0.12).Timeout += () => { if (IsInstanceValid(_sprite)) _sprite.Modulate = Colors.White; };
	}

	private void FlashHitWhite()
	{
		if (_sprite == null) return;
		_sprite.Modulate = new Color(1.5f, 1.5f, 1.5f);
		GetTree().CreateTimer(0.08).Timeout += () => { if (IsInstanceValid(_sprite)) _sprite.Modulate = Colors.White; };
	}

	private void SpawnDrop(string itemId, int amount)
	{
		var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");
		var pickup = pickupScene.Instantiate<ItemPickup>();
		var data = ItemDatabase.Instance?.Get(itemId);
		if (data != null) pickup.Data = data;
		else if (System.Enum.TryParse<ItemType>(itemId, out var t)) pickup.Type = t;
		pickup.Amount = amount;
		pickup.GlobalPosition = GlobalPosition + new Vector2((float)GD.RandRange(-12, 12), (float)GD.RandRange(-12, 12));
		GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, pickup);
	}
}
