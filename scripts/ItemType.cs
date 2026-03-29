using Godot;

public enum ItemCategory
{
	Resource,
	Tool,
	Weapon,
	Food,
	Other
}

public enum ItemType
{
	// Resources
	Wood,
	Stone,
	Herb,
	IronOre,
	GoldOre,
	Crystal,

	// Tools
	Axe,
	Pickaxe,
	Shovel,

	// Weapons
	IronSword,

	// Food
	Apple,
	Mushroom,
}

public static class ItemRegistry
{
	public static string GetName(ItemType type) => type switch
	{
		ItemType.Wood      => "Wood",
		ItemType.Stone     => "Stone",
		ItemType.Herb      => "Herb",
		ItemType.IronOre   => "Iron Ore",
		ItemType.GoldOre   => "Gold Ore",
		ItemType.Crystal   => "Crystal",
		ItemType.Axe       => "Axe",
		ItemType.Pickaxe   => "Pickaxe",
		ItemType.Shovel    => "Shovel",
		ItemType.IronSword => "Iron Sword",
		ItemType.Apple     => "Apple",
		ItemType.Mushroom  => "Mushroom",
		_                  => type.ToString()
	};

	public static ItemCategory GetCategory(ItemType type) => type switch
	{
		ItemType.Wood or ItemType.Stone or ItemType.Herb or
		ItemType.IronOre or ItemType.GoldOre or
		ItemType.Crystal => ItemCategory.Resource,

		ItemType.Axe or ItemType.Pickaxe or ItemType.Shovel => ItemCategory.Tool,

		ItemType.IronSword => ItemCategory.Weapon,

		ItemType.Apple or ItemType.Mushroom => ItemCategory.Food,

		_ => ItemCategory.Other
	};

	public static string GetDescription(ItemType type) => type switch
	{
		ItemType.Wood      => "Basic building material gathered from trees.",
		ItemType.Stone     => "Sturdy material mined from rocks.",
		ItemType.Herb      => "A medicinal plant with healing properties.",
		ItemType.IronOre   => "Raw iron ready to be smelted.",
		ItemType.GoldOre   => "Precious golden ore.",
		ItemType.Crystal   => "A shimmering gemstone.",
		ItemType.Axe       => "Used to chop trees more efficiently.",
		ItemType.Pickaxe   => "Used to mine rocks more efficiently.",
		ItemType.Shovel    => "Used to dig and uncover items.",
		ItemType.IronSword => "A sturdy iron blade.",
		ItemType.Apple     => "Restores a small amount of health.",
		ItemType.Mushroom  => "A forest mushroom. Edible... probably.",
		_                  => ""
	};

	// Icon atlas path + region for each item
	// Uses the same textures as the actual item pickups in the game
	public static Rect2 GetIconRegion(ItemType type) => type switch
	{
		// Wood: log from outdoor_decor.png
		ItemType.Wood    => new Rect2(67, 115, 26, 11),
		// Stone: grey stone from resources_icons_no_outline.png (row 5, col 0)
		ItemType.Stone   => new Rect2(0, 80, 16, 16),
		// Herb: plant from outdoor_decor_free.png
		ItemType.Herb    => new Rect2(35, 3, 11, 10),

		// Resources (from resources_icons_no_outline.png) - 6x6 grid of 16x16
		ItemType.IronOre => new Rect2(0, 0, 16, 16),
		ItemType.GoldOre => new Rect2(0, 16, 16, 16),
		ItemType.Crystal => new Rect2(16, 0, 16, 16),

		// Tools (from tool_icons_outline.png) - strip: bow, arrow, pickaxe, axe, sword, shovel/plough, ...
		ItemType.Axe     => new Rect2(48, 0, 16, 16),
		ItemType.Pickaxe => new Rect2(32, 0, 16, 16),
		ItemType.Shovel  => new Rect2(80, 0, 16, 16),

		// Weapons (from tool_icons_outline.png)
		ItemType.IronSword => new Rect2(64, 0, 16, 16),

		// Food (from food_icons_outline.png) - 8x12 grid of 16x16
		ItemType.Apple    => new Rect2(0, 16, 16, 16),
		ItemType.Mushroom => new Rect2(16, 16, 16, 16),

		_ => new Rect2(0, 0, 16, 16)
	};

	// Each item type maps to a specific texture atlas
	public static string GetIconTexturePath(ItemType type) => type switch
	{
		ItemType.Wood => "res://assets/cute_fantasy/cute_fantasy/outdoor decoration/outdoor_decor.png",
		ItemType.Stone => "res://assets/cute_fantasy/cute_fantasy/icons/no outline/resources_icons_no_outline.png",
		ItemType.Herb => "res://assets/cute_fantasy_free/cute_fantasy_free/outdoor decoration/outdoor_decor_free.png",

		ItemType.IronOre or ItemType.GoldOre or ItemType.Crystal
			=> "res://assets/cute_fantasy/cute_fantasy/icons/no outline/resources_icons_no_outline.png",

		ItemType.Axe or ItemType.Pickaxe or ItemType.Shovel or
		ItemType.IronSword
			=> "res://assets/cute_fantasy/cute_fantasy/icons/outline/tool_icons_outline.png",

		ItemType.Apple or ItemType.Mushroom
			=> "res://assets/cute_fantasy/cute_fantasy/icons/outline/food_icons_outline.png",

		_ => "res://assets/cute_fantasy/cute_fantasy/icons/outline/other_icons_outline.png"
	};

	public static int GetWeaponDamage(ItemType type) => type switch
	{
		ItemType.IronSword => 3,
		_ => 0
	};

	public static int GetMaxStack(ItemType type)
	{
		var category = GetCategory(type);
		return category switch
		{
			ItemCategory.Resource => 99,
			ItemCategory.Food     => 20,
			ItemCategory.Tool     => 1,
			ItemCategory.Weapon   => 1,
			_                     => 99
		};
	}
}
