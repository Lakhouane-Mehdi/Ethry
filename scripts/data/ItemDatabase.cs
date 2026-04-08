using System.Collections.Generic;
using Godot;

/// <summary>
/// Autoload singleton that loads all ItemData resources from a folder.
/// To add a new item: create a .tres file in res://data/items/ via the editor.
/// </summary>
public partial class ItemDatabase : Node
{
	public static ItemDatabase Instance { get; private set; }

	[Export] public string ItemFolder = "res://data/items";

	private readonly Dictionary<string, ItemData> _items = new();

	public override void _Ready()
	{
		Instance = this;
		LoadItems();
	}

	private void LoadItems()
	{
		using var dir = DirAccess.Open(ItemFolder);
		if (dir == null)
		{
			GD.PrintErr($"ItemDatabase: cannot open folder '{ItemFolder}'");
			return;
		}

		dir.ListDirBegin();
		string file;
		while ((file = dir.GetNext()) != "")
		{
			if (file.StartsWith('.') || !file.EndsWith(".tres"))
				continue;

			var item = GD.Load<ItemData>($"{ItemFolder}/{file}");
			if (item == null || string.IsNullOrEmpty(item.Id)) continue;
			if (_items.ContainsKey(item.Id))
				GD.PrintErr($"ItemDatabase: duplicate Id '{item.Id}' in '{file}'");
			else
				_items[item.Id] = item;
		}
		dir.ListDirEnd();

		GD.Print($"ItemDatabase: loaded {_items.Count} items from '{ItemFolder}'");
	}

	public ItemData Get(string id)
	{
		return id != null && _items.TryGetValue(id, out var d) ? d : null;
	}

	public IReadOnlyDictionary<string, ItemData> All => _items;
}
