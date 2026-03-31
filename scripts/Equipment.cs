using Godot;

public enum EquipSlot { Weapon, Head, Body, Boots }

public partial class Equipment : Node
{
	public static Equipment Instance { get; private set; }

	[Signal] public delegate void ChangedEventHandler();

	private ItemType? _weapon;
	private ItemType? _head;
	private ItemType? _body;
	private ItemType? _boots;

	public ItemType? Weapon => _weapon;
	public ItemType? Head   => _head;
	public ItemType? Body   => _body;
	public ItemType? Boots  => _boots;

	public override void _Ready() => Instance = this;

	// ── Queries ────────────────────────────────────────────────────────────
	public ItemType? GetSlot(EquipSlot slot) => slot switch
	{
		EquipSlot.Weapon => _weapon,
		EquipSlot.Head   => _head,
		EquipSlot.Body   => _body,
		EquipSlot.Boots  => _boots,
		_                => null
	};

	public int GetAttackDamage()
	{
		return _weapon.HasValue ? ItemRegistry.GetWeaponDamage(_weapon.Value) : 1;
	}

	public int GetTotalDefence()
	{
		int def = 0;
		if (_head.HasValue)  def += ItemRegistry.GetArmorRating(_head.Value);
		if (_body.HasValue)  def += ItemRegistry.GetArmorRating(_body.Value);
		if (_boots.HasValue) def += ItemRegistry.GetArmorRating(_boots.Value);
		return def;
	}

	// ── Equip ──────────────────────────────────────────────────────────────
	/// <summary>Routes the item to the correct slot based on its EquipSlot.
	/// Returns false if the item is not equippable or not in inventory.</summary>
	public bool Equip(ItemType type)
	{
		EquipSlot? slot = ItemRegistry.GetEquipSlot(type);
		if (slot == null) return false;
		if (!Inventory.Instance.HasItem(type)) return false;

		// Return currently-equipped item in that slot to inventory
		ItemType? current = GetSlot(slot.Value);
		if (current.HasValue)
			Inventory.Instance.AddItem(current.Value);

		Inventory.Instance.RemoveItem(type);
		SetSlot(slot.Value, type);
		EmitSignal(SignalName.Changed);
		return true;
	}

	/// <summary>Unequips the item in the given slot, returning it to inventory.</summary>
	public void Unequip(EquipSlot slot)
	{
		ItemType? current = GetSlot(slot);
		if (!current.HasValue) return;

		Inventory.Instance.AddItem(current.Value);
		SetSlot(slot, null);
		EmitSignal(SignalName.Changed);
	}

	// ── Private ────────────────────────────────────────────────────────────
	private void SetSlot(EquipSlot slot, ItemType? value)
	{
		switch (slot)
		{
			case EquipSlot.Weapon: _weapon = value; break;
			case EquipSlot.Head:   _head   = value; break;
			case EquipSlot.Body:   _body   = value; break;
			case EquipSlot.Boots:  _boots  = value; break;
		}
	}
}
