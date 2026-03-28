using System.Collections.Generic;
using Godot;

public partial class Inventory : Node
{
	public static Inventory Instance { get; private set; }

	private readonly Dictionary<ItemType, int> _items = new();

	[Signal]
	public delegate void ChangedEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}

	public void AddItem(ItemType type, int amount = 1)
	{
		_items[type] = GetCount(type) + amount;
		EmitSignal(SignalName.Changed);
		GD.Print($"Picked up {amount}x {ItemRegistry.GetName(type)}. Total: {_items[type]}");
	}

	public int GetCount(ItemType type) =>
		_items.TryGetValue(type, out int count) ? count : 0;

	public IReadOnlyDictionary<ItemType, int> Items => _items;
}
