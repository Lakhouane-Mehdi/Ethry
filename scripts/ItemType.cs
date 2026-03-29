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
	Coal,
	IronOre,
	GoldOre,
	Crystal,

	// Tools
	Axe,
	Pickaxe,
	Shovel,

	// Weapons
	WoodSword,
	IronSword,
	GoldSword,

	// Food
	Apple,
	Bread,
	Mushroom,
}

public static class ItemRegistry
{
	public static string GetName(ItemType type) => type switch
	{
		ItemType.Wood      => "Wood",
		ItemType.Stone     => "Stone",
		ItemType.Herb      => "Herb",
		ItemType.Coal      => "Coal",
		ItemType.IronOre   => "Iron Ore",
		ItemType.GoldOre   => "Gold Ore",
		ItemType.Crystal   => "Crystal",
		ItemType.Axe       => "Axe",
		ItemType.Pickaxe   => "Pickaxe",
		ItemType.Shovel    => "Shovel",
		ItemType.WoodSword => "Wood Sword",
		ItemType.IronSword => "Iron Sword",
		ItemType.GoldSword => "Gold Sword",
		ItemType.Apple     => "Apple",
		ItemType.Bread     => "Bread",
		ItemType.Mushroom  => "Mushroom",
		_                  => type.ToString()
	};

	public static ItemCategory GetCategory(ItemType type) => type switch
	{
		ItemType.Wood or ItemType.Stone or ItemType.Herb or
		ItemType.Coal or ItemType.IronOre or ItemType.GoldOre or
		ItemType.Crystal => ItemCategory.Resource,

		ItemType.Axe or ItemType.Pickaxe or ItemType.Shovel => ItemCategory.Tool,

		ItemType.WoodSword or ItemType.IronSword or ItemType.GoldSword => ItemCategory.Weapon,

		ItemType.Apple or ItemType.Bread or ItemType.Mushroom => ItemCategory.Food,

		_ => ItemCategory.Other
	};

	public static string GetDescription(ItemType type) => type switch
	{
		ItemType.Wood      => "Basic building material gathered from trees.",
		ItemType.Stone     => "Sturdy material mined from rocks.",
		ItemType.Herb      => "A medicinal plant with healing properties.",
		ItemType.Coal      => "Dark mineral used for fuel and smelting.",
		ItemType.IronOre   => "Raw iron ready to be smelted.",
		ItemType.GoldOre   => "Precious golden ore.",
		ItemType.Crystal   => "A shimmering gemstone.",
		ItemType.Axe       => "Used to chop trees more efficiently.",
		ItemType.Pickaxe   => "Used to mine rocks more efficiently.",
		ItemType.Shovel    => "Used to dig and uncover items.",
		ItemType.WoodSword => "A basic wooden sword.",
		ItemType.IronSword => "A sturdy iron blade.",
		ItemType.GoldSword => "A powerful golden sword.",
		ItemType.Apple     => "Restores a small amount of health.",
		ItemType.Bread     => "Restores a moderate amount of health.",
		ItemType.Mushroom  => "A forest mushroom. Edible... probably.",
		_                  => ""
	};

	// Returns the atlas region (x, y, w, h) in the icon spritesheet for each item
	// resources_icons_outline.png: 96x96, 16x16 tiles (6 columns, 6 rows)
	// tool_icons_outline.png: 160x16, 16x16 tiles (10 columns, 1 row)
	// food_icons_outline.png: 128x192, 16x16 tiles (8 columns, 12 rows)
	public static Rect2 GetIconRegion(ItemType type) => type switch
	{
		// Resources (from resources_icons_outline.png)
		ItemType.Wood    => new Rect2(0, 0, 16, 16),
		ItemType.Stone   => new Rect2(16, 0, 16, 16),
		ItemType.Herb    => new Rect2(64, 16, 16, 16),
		ItemType.Coal    => new Rect2(32, 0, 16, 16),
		ItemType.IronOre => new Rect2(48, 0, 16, 16),
		ItemType.GoldOre => new Rect2(64, 0, 16, 16),
		ItemType.Crystal => new Rect2(80, 0, 16, 16),

		// Tools (from tool_icons_outline.png)
		ItemType.Axe     => new Rect2(0, 0, 16, 16),
		ItemType.Pickaxe => new Rect2(16, 0, 16, 16),
		ItemType.Shovel  => new Rect2(32, 0, 16, 16),

		// Weapons (from tool_icons_outline.png)
		ItemType.WoodSword => new Rect2(48, 0, 16, 16),
		ItemType.IronSword => new Rect2(64, 0, 16, 16),
		ItemType.GoldSword => new Rect2(80, 0, 16, 16),

		// Food (from food_icons_outline.png)
		ItemType.Apple    => new Rect2(0, 0, 16, 16),
		ItemType.Bread    => new Rect2(16, 0, 16, 16),
		ItemType.Mushroom => new Rect2(32, 0, 16, 16),

		_ => new Rect2(0, 0, 16, 16)
	};

	public static string GetIconTexturePath(ItemType type)
	{
		var category = GetCategory(type);
		return category switch
		{
			ItemCategory.Resource => "res://assets/cute_fantasy/cute_fantasy/icons/outline/resources_icons_outline.png",
			ItemCategory.Tool     => "res://assets/cute_fantasy/cute_fantasy/icons/outline/tool_icons_outline.png",
			ItemCategory.Weapon   => "res://assets/cute_fantasy/cute_fantasy/icons/outline/tool_icons_outline.png",
			ItemCategory.Food     => "res://assets/cute_fantasy/cute_fantasy/icons/outline/food_icons_outline.png",
			_                     => "res://assets/cute_fantasy/cute_fantasy/icons/outline/other_icons_outline.png"
		};
	}

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
