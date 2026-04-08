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

	public int GetAttackDamage()
	{
		if (_weaponId == null) return 1;
		var data = ItemDatabase.Instance?.Get(_weaponId);
		if (data != null && data.WeaponDamage > 0) return data.WeaponDamage;
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
		return data?.ArmorRating ?? 0;
	}

	// ── Equip (string ID) ─────────────────────────────────────────────────
	public bool Equip(string id)
	{
		var data = ItemDatabase.Instance?.Get(id);
		if (data == null) return false;

		EquipSlot? slot = data.GetEquipSlot();
		if (slot == null) return false;
		if (!Inventory.Instance.HasItem(id)) return false;

		string current = GetSlotId(slot.Value);
		if (current != null)
			Inventory.Instance.AddItem(current);

		Inventory.Instance.RemoveItem(id);
		SetSlot(slot.Value, id);
		AudioManager.Instance?.PlaySfx("item_equip");
		EmitSignal(SignalName.Changed);
		return true;
	}

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
