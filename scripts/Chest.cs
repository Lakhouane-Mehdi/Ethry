using Godot;

/// <summary>
/// Interactable loot chest with 4 tiers.
/// Opens on interact, spawns random items, then stays open.
/// </summary>
public partial class Chest : Area2D
{
	/// <summary>0 = Wood, 1 = Metal, 2 = Gold, 3 = Jeweled</summary>
	[Export] public int Tier = 0;

	private bool _playerInRange;
	private bool _opened;
	private Label _prompt;
	private AnimatedSprite2D _sprite;

	// Loot tables per tier — (item, minAmount, maxAmount, weight)
	private static readonly (ItemType, int, int, int)[][] LootTables = new[]
	{
		// Tier 0 — Wood chest: basic resources
		new (ItemType, int, int, int)[]
		{
			(ItemType.Wood,   2, 5, 30),
			(ItemType.Stone,  2, 4, 25),
			(ItemType.Fiber,  1, 3, 20),
			(ItemType.Herb,   1, 2, 15),
			(ItemType.Coal,   1, 2, 10),
		},
		// Tier 1 — Metal chest: better resources
		new (ItemType, int, int, int)[]
		{
			(ItemType.IronOre,  2, 4, 25),
			(ItemType.Coal,     2, 3, 20),
			(ItemType.Leather,  1, 3, 20),
			(ItemType.Bone,     1, 2, 15),
			(ItemType.Crystal,  1, 1, 10),
			(ItemType.Herb,     2, 4, 10),
		},
		// Tier 2 — Gold chest: rare materials
		new (ItemType, int, int, int)[]
		{
			(ItemType.GoldOre,    1, 3, 25),
			(ItemType.IronIngot,  1, 2, 20),
			(ItemType.Crystal,    1, 2, 20),
			(ItemType.Leather,    2, 4, 15),
			(ItemType.GoldIngot,  1, 1, 10),
			(ItemType.HealthPotion, 1, 1, 10),
		},
		// Tier 3 — Jeweled chest: best loot
		new (ItemType, int, int, int)[]
		{
			(ItemType.GoldIngot,    1, 2, 20),
			(ItemType.IronIngot,    2, 3, 20),
			(ItemType.Crystal,      1, 3, 15),
			(ItemType.HealthPotion, 1, 2, 15),
			(ItemType.GoldOre,      2, 4, 15),
			(ItemType.Crystal,      2, 3, 5),
		},
	};

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask  = 1;
		ProcessMode    = ProcessModeEnum.Always;

		BodyEntered += OnBodyEntered;
		BodyExited  += OnBodyExited;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.AnimationFinished += OnAnimationFinished;
		_sprite.Play("closed");

		_prompt = new Label();
		_prompt.Text     = "Press E to Open";
		_prompt.Position = new Vector2(-40, -50);
		_prompt.AddThemeColorOverride("font_color", Colors.White);
		_prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_prompt.AddThemeFontSizeOverride("font_size", 10);
		_prompt.AddThemeConstantOverride("shadow_offset_x", 1);
		_prompt.AddThemeConstantOverride("shadow_offset_y", 1);
		_prompt.Visible = false;
		AddChild(_prompt);
	}

	public override void _Process(double delta)
	{
		if (!_playerInRange || _opened) return;

		if (Input.IsActionJustPressed("interact"))
			Open();
	}

	private void Open()
	{
		_opened         = true;
		_prompt.Visible = false;
		_sprite.Play("open");
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "open")
			SpawnLoot();
	}

	private void SpawnLoot()
	{
		int tier  = Mathf.Clamp(Tier, 0, LootTables.Length - 1);
		var table = LootTables[tier];
		int rolls = 2 + tier; // wood=2, metal=3, gold=4, jeweled=5

		var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");

		for (int i = 0; i < rolls; i++)
		{
			var (itemType, minAmt, maxAmt, _) = PickWeighted(table);
			int amount = (int)GD.RandRange(minAmt, maxAmt + 1);
			if (amount < minAmt) amount = minAmt;

			var pickup = pickupScene.Instantiate<ItemPickup>();
			pickup.Type   = itemType;
			pickup.Amount = amount;

			// Scatter items around the chest
			float angle  = (float)GD.RandRange(0, Mathf.Tau);
			float radius = (float)GD.RandRange(20, 45);
			pickup.Position = GlobalPosition + new Vector2(
				Mathf.Cos(angle) * radius,
				Mathf.Sin(angle) * radius
			);

			GetTree().CurrentScene.AddChild(pickup);
		}

		NotificationManager.Instance?.Show("Chest opened!", new Color(1f, 0.85f, 0.3f));
	}

	private static (ItemType, int, int, int) PickWeighted((ItemType, int, int, int)[] table)
	{
		int totalWeight = 0;
		foreach (var (_, _, _, w) in table)
			totalWeight += w;

		int roll = (int)GD.RandRange(0, totalWeight);
		int cumulative = 0;
		foreach (var entry in table)
		{
			cumulative += entry.Item4;
			if (roll < cumulative)
				return entry;
		}
		return table[^1];
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = true;
		if (!_opened)
			_prompt.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange  = false;
		_prompt.Visible = false;
	}
}
