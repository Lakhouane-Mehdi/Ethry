using System.Collections.Generic;
using Godot;

public partial class Inventory : Node
{
	public static Inventory Instance { get; private set; }

	private readonly Dictionary<ItemType, int> _items = new();

	/// <summary>Fired whenever any stack count changes.</summary>
	[Signal] public delegate void ChangedEventHandler();

	/// <summary>Fired specifically when items are added (for pickup notifications).</summary>
	[Signal] public delegate void ItemAddedEventHandler(string itemName, int amount);

	public override void _Ready() => Instance = this;

	public void AddItem(ItemType type, int amount = 1)
	{
		_items[type] = GetCount(type) + amount;
		EmitSignal(SignalName.Changed);
		EmitSignal(SignalName.ItemAdded, ItemRegistry.GetName(type), amount);
	}

	public bool RemoveItem(ItemType type, int amount = 1)
	{
		int current = GetCount(type);
		if (current < amount) return false;

		current -= amount;
		if (current <= 0) _items.Remove(type);
		else              _items[type] = current;

		EmitSignal(SignalName.Changed);
		return true;
	}

	public bool HasItem(ItemType type, int amount = 1) => GetCount(type) >= amount;
	public int  GetCount(ItemType type)                => _items.TryGetValue(type, out int c) ? c : 0;
	public IReadOnlyDictionary<ItemType, int> Items    => _items;
}
