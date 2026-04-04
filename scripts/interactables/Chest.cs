using Godot;
using FSM;

/// <summary>
/// Interactable loot chest with 4 tiers.
/// Refactored to use a StateMachine for its interaction and animation lifecycle.
/// </summary>
public partial class Chest : Area2D
{
	[Export] public int Tier = 0;

	private bool _playerInRange;
	private bool _opened;
	private bool _initialLootGenerated;
	private Label _prompt;
	private AnimatedSprite2D _sprite;
	private Inventory _inventory;
	private FSM.StateMachine _stateMachine;

	public bool PlayerInRange => _playerInRange;
	public bool InitialLootGenerated => _initialLootGenerated;

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
		_sprite.Play("closed");

		_stateMachine = GetNode<FSM.StateMachine>("StateMachine");

		// Initialize private inventory for this chest
		_inventory = new Inventory { IsPlayerInventory = false };
		AddChild(_inventory);

		_prompt = new Label();
		_prompt.Position = new Vector2(-40, -50);
		_prompt.AddThemeColorOverride("font_color", Colors.White);
		_prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_prompt.AddThemeFontSizeOverride("font_size", 10);
		_prompt.AddThemeConstantOverride("shadow_offset_x", 1);
		_prompt.AddThemeConstantOverride("shadow_offset_y", 1);
		_prompt.Visible = false;
		AddChild(_prompt);
	}

	public void UpdatePrompt()
	{
		if (_prompt == null) return;
		bool isOpened = _stateMachine.CurrentState is ChestOpenState;
		_prompt.Text = isOpened ? "Press E for Storage" : "Press E to Open";
		_prompt.Visible = _playerInRange;
	}

	public void HidePrompt()
	{
		if (_prompt != null) _prompt.Visible = false;
	}

	public void PlayAnimation(string anim) => _sprite?.Play(anim);

	public bool IsAnimationFinished(string anim)
	{
		return _sprite != null && _sprite.Animation == anim && _sprite.Frame >= _sprite.SpriteFrames.GetFrameCount(anim) - 1;
	}

	public override void _Process(double delta)
	{
		_stateMachine?.Update(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		_stateMachine?.HandleInput(@event);
	}

	public void OpenStorageUI()
	{
		string chestName = Tier switch {
			1 => "Metal Chest",
			2 => "Gold Chest",
			3 => "Jeweled Chest",
			_ => "Wooden Chest"
		};
		var storageUI = GetTree().Root.GetNodeOrNull("StorageUI");
		if (storageUI != null)
			storageUI.Call("Open", _inventory, chestName, this);
		else
			GD.PrintErr("Chest: StorageUI not found in root.");
	}

	/// <summary>Called by StorageUI when closing.</summary>
	public void CloseStorage()
	{
		_sprite.PlayBackwards("open");
		_stateMachine.TransitionTo("Closed");
	}

	public void GenerateInitialLoot()
	{
		_initialLootGenerated = true;
		int tier  = Mathf.Clamp(Tier, 0, LootTables.Length - 1);
		var table = LootTables[tier];
		int rolls = 2 + tier; 

		for (int i = 0; i < rolls; i++)
		{
			var (itemType, minAmt, maxAmt, _) = PickWeighted(table);
			int amount = (int)GD.RandRange(minAmt, maxAmt + 1);
			_inventory.AddItem(itemType.ToString(), amount);
		}

		NotificationManager.Instance?.Show("Chest contents discovered!", new Color(1f, 0.85f, 0.3f));
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
		_stateMachine?.CurrentState?.Enter(); // Update prompt visibility via state
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange  = false;
		HidePrompt();
	}
}
