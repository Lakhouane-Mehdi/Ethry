using Godot;

public enum ItemType
{
	Wood,
	Stone,
	Herb,
}

public static class ItemRegistry
{
	public static string GetName(ItemType type) => type switch
	{
		ItemType.Wood  => "Wood",
		ItemType.Stone => "Stone",
		ItemType.Herb  => "Herb",
		_              => type.ToString()
	};
}
