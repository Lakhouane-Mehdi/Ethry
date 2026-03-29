using Godot;

public partial class Equipment : Node
{
	public static Equipment Instance { get; private set; }

	[Signal]
	public delegate void ChangedEventHandler();

	private ItemType? _weapon;

	public ItemType? Weapon => _weapon;

	public override void _Ready()
	{
		Instance = this;
	}

	public int GetAttackDamage()
	{
		return _weapon switch
		{
			ItemType.IronSword => 3,
			_ => 1
		};
	}

	public bool Equip(ItemType type)
	{
		if (ItemRegistry.GetCategory(type) != ItemCategory.Weapon) return false;
		if (!Inventory.Instance.HasItem(type)) return false;

		// Unequip current weapon back to inventory
		if (_weapon.HasValue)
			Inventory.Instance.AddItem(_weapon.Value);

		Inventory.Instance.RemoveItem(type);
		_weapon = type;
		EmitSignal(SignalName.Changed);
		return true;
	}

	public void Unequip()
	{
		if (!_weapon.HasValue) return;
		Inventory.Instance.AddItem(_weapon.Value);
		_weapon = null;
		EmitSignal(SignalName.Changed);
	}
}
