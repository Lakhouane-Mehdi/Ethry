using Godot;

public enum EquipSlot { Weapon, Head, Body, Boots }

public partial class Equipment : Node
{
	public static Equipment Instance { get; private set; }

	[Signal] public delegate void ChangedEventHandler();

	private string _weaponId;
	private string _headId;
	private string _bodyId;
	private string _bootsId;

	public override void _Ready() => Instance = this;

	// ── Queries ────────────────────────────────────────────────────────────
	public string GetSlotId(EquipSlot slot) => slot switch
	{
		EquipSlot.Weapon => _weaponId,
		EquipSlot.Head   => _headId,
		EquipSlot.Body   => _bodyId,
		EquipSlot.Boots  => _bootsId,
		_                => null
	};

	public ItemData GetSlotData(EquipSlot slot)
	{
		var id = GetSlotId(slot);
		return id != null ? ItemDatabase.Instance?.Get(id) : null;
	}

	/// <summary>Legacy: returns ItemType? for the slot (works for enum-based items).</summary>
	public ItemType? GetSlot(EquipSlot slot)
	{
		var id = GetSlotId(slot);
		if (id == null) return null;
		return System.Enum.TryParse<ItemType>(id, out var t) ? t : null;
	}

	public int GetAttackDamage()
	{
		if (_weaponId == null) return 1;
		var data = ItemDatabase.Instance?.Get(_weaponId);
		if (data != null) return data.WeaponDamage > 0 ? data.WeaponDamage : 1;
		// Fallback to enum registry
		if (System.Enum.TryParse<ItemType>(_weaponId, out var t))
			return ItemRegistry.GetWeaponDamage(t);
		return 1;
	}

	public int GetTotalDefence()
	{
		return GetArmorFor(_headId) + GetArmorFor(_bodyId) + GetArmorFor(_bootsId);
	}

	private int GetArmorFor(string id)
	{
		if (id == null) return 0;
		var data = ItemDatabase.Instance?.Get(id);
		if (data != null) return data.ArmorRating;
		if (System.Enum.TryParse<ItemType>(id, out var t))
			return ItemRegistry.GetArmorRating(t);
		return 0;
	}

	// ── Equip (string ID) ─────────────────────────────────────────────────
	public bool Equip(string id)
	{
		var data = ItemDatabase.Instance?.Get(id);
		EquipSlot? slot;

		if (data != null)
			slot = data.GetEquipSlot();
		else if (System.Enum.TryParse<ItemType>(id, out var t))
			slot = ItemRegistry.GetEquipSlot(t);
		else
			return false;

		if (slot == null) return false;
		if (!Inventory.Instance.HasItem(id)) return false;

		string current = GetSlotId(slot.Value);
		if (current != null)
			Inventory.Instance.AddItem(current);

		Inventory.Instance.RemoveItem(id);
		SetSlot(slot.Value, id);
		EmitSignal(SignalName.Changed);
		return true;
	}

	/// <summary>Legacy ItemType overload.</summary>
	public bool Equip(ItemType type) => Equip(type.ToString());

	// ── Unequip ────────────────────────────────────────────────────────────
	public void Unequip(EquipSlot slot)
	{
		string current = GetSlotId(slot);
		if (current == null) return;

		Inventory.Instance.AddItem(current);
		SetSlot(slot, null);
		EmitSignal(SignalName.Changed);
	}

	// ── Private ────────────────────────────────────────────────────────────
	private void SetSlot(EquipSlot slot, string value)
	{
		switch (slot)
		{
			case EquipSlot.Weapon: _weaponId = value; break;
			case EquipSlot.Head:   _headId   = value; break;
			case EquipSlot.Body:   _bodyId   = value; break;
			case EquipSlot.Boots:  _bootsId  = value; break;
		}
	}
}
