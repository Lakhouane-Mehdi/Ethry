using System.Collections.Generic;
using Godot;

public partial class Inventory : Node
{
	[Export] public bool IsPlayerInventory = true;
	public static Inventory Instance { get; private set; }

	private readonly Dictionary<string, int> _items = new();

	[Signal] public delegate void ChangedEventHandler();
	[Signal] public delegate void ItemAddedEventHandler(string itemId, string itemName, int amount);

	public override void _Ready()
	{
		if (IsPlayerInventory)
		{
			Instance = this;
			// DEBUG: starter shovel for testing farming
			if (!HasItem("Shovel")) AddItem("Shovel", 1);
		}
	}

	/// <summary>Moves an item from this inventory to another.</summary>
	public bool TransferTo(Inventory other, string id, int amount = 1)
	{
		if (!HasItem(id, amount)) return false;
		RemoveItem(id, amount);
		other.AddItem(id, amount);
		return true;
	}

	// ── Core API (string ID) ──────────────────────────────────────────────
	public void AddItem(string id, int amount = 1)
	{
		_items[id] = GetCount(id) + amount;
		EmitSignal(SignalName.Changed);

		var data = ItemDatabase.Instance?.Get(id);
		string name = data != null ? data.DisplayName : id;
		EmitSignal(SignalName.ItemAdded, id, name, amount);
	}

	public bool RemoveItem(string id, int amount = 1)
	{
		int current = GetCount(id);
		if (current < amount) return false;

		current -= amount;
		if (current <= 0) _items.Remove(id);
		else              _items[id] = current;

		EmitSignal(SignalName.Changed);
		return true;
	}

	public bool HasItem(string id, int amount = 1) => GetCount(id) >= amount;
	public int  GetCount(string id)                => _items.TryGetValue(id, out int c) ? c : 0;
	public IReadOnlyDictionary<string, int> Items  => _items;

	// ── ItemData overloads ────────────────────────────────────────────────
	public void AddItem(ItemData item, int amount = 1)    => AddItem(item.Id, amount);
	public bool RemoveItem(ItemData item, int amount = 1) => RemoveItem(item.Id, amount);
	public bool HasItem(ItemData item, int amount = 1)    => HasItem(item.Id, amount);
	public int  GetCount(ItemData item)                   => GetCount(item.Id);

	// ── Legacy ItemType overloads (backward compat) ───────────────────────
	public void AddItem(ItemType type, int amount = 1)    => AddItem(type.ToString(), amount);
	public bool RemoveItem(ItemType type, int amount = 1) => RemoveItem(type.ToString(), amount);
	public bool HasItem(ItemType type, int amount = 1)    => HasItem(type.ToString(), amount);
	public int  GetCount(ItemType type)                   => GetCount(type.ToString());
}
