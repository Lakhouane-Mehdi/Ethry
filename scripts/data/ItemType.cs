using Godot;

public enum ItemCategory { Resource, Tool, Weapon, Armor, Food, Potion }

public enum ItemType
{
	// Resources — raw materials
	Wood, Stone, Herb, IronOre, GoldOre, Crystal,
	Coal, Leather, Bone, Fiber,
	IronIngot, GoldIngot, Wheat, WheatSeeds, RawMeat,

	// Tools
	Axe, Pickaxe, Shovel, FishingRod, WateringCan, WoodenTorch,

	// Weapons
	IronSword, GoldSword, Spear, Bow,

	// Armor
	LeatherHelmet, LeatherArmor, LeatherBoots,
	IronHelmet, IronArmor, IronBoots,

	// Food
	Apple, Mushroom, Bread, Cheese, CookedMeat, Carrot, Berry,
	Cherry, Peach, Pear,

	// Potions
	HealthPotion,
}

/// <summary>
/// Static lookup for item properties. Delegates to ItemDatabase (.tres files)
/// when available, falling back to hardcoded data.
/// </summary>
public static class ItemRegistry
{
	/// <summary>Returns the ItemData resource for an enum type (from database or null).</summary>
	public static ItemData GetData(ItemType type)
	{
		return ItemDatabase.Instance?.Get(type.ToString());
	}
	private const string ResNoOutline   = "res://assets/cute_fantasy/cute_fantasy/icons/no outline/resources_icons_no_outline.png";
	private const string ResOutline     = "res://assets/cute_fantasy/cute_fantasy/icons/outline/resources_icons_outline.png";
	private const string ToolOutline    = "res://assets/cute_fantasy/cute_fantasy/icons/outline/tool_icons_outline.png";
	private const string FoodOutline    = "res://assets/cute_fantasy/cute_fantasy/icons/outline/food_icons_outline.png";
	private const string OtherOutline   = "res://assets/cute_fantasy/cute_fantasy/icons/outline/other_icons_outline.png";
	private const string WoodDecor      = "res://assets/cute_fantasy/cute_fantasy/outdoor decoration/outdoor_decor.png";
	private const string HerbDecor      = "res://assets/cute_fantasy_free/cute_fantasy_free/outdoor decoration/outdoor_decor_free.png";

	// ── Display name ───────────────────────────────────────────────────────
	public static string GetName(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.DisplayName;
		return GetNameFallback(type);
	}
	private static string GetNameFallback(ItemType type) => type switch
	{
		ItemType.Wood           => "Wood",
		ItemType.Stone          => "Stone",
		ItemType.Herb           => "Herb",
		ItemType.IronOre        => "Iron Ore",
		ItemType.GoldOre        => "Gold Ore",
		ItemType.Crystal        => "Crystal",
		ItemType.Coal           => "Coal",
		ItemType.Leather        => "Leather",
		ItemType.Bone           => "Bone",
		ItemType.Fiber          => "Fiber",
		ItemType.IronIngot      => "Iron Ingot",
		ItemType.GoldIngot      => "Gold Ingot",
		ItemType.Axe            => "Axe",
		ItemType.Pickaxe        => "Pickaxe",
		ItemType.Shovel         => "Shovel",
		ItemType.FishingRod     => "Fishing Rod",
		ItemType.WateringCan    => "Watering Can",
		ItemType.WoodenTorch    => "Wooden Torch",
		ItemType.IronSword      => "Iron Sword",
		ItemType.GoldSword      => "Gold Sword",
		ItemType.Spear          => "Spear",
		ItemType.Bow            => "Bow",
		ItemType.LeatherHelmet  => "Leather Helmet",
		ItemType.LeatherArmor   => "Leather Armor",
		ItemType.LeatherBoots   => "Leather Boots",
		ItemType.IronHelmet     => "Iron Helmet",
		ItemType.IronArmor      => "Iron Armor",
		ItemType.IronBoots      => "Iron Boots",
		ItemType.Apple          => "Apple",
		ItemType.Mushroom       => "Mushroom",
		ItemType.Bread          => "Bread",
		ItemType.Cheese         => "Cheese",
		ItemType.CookedMeat     => "Cooked Meat",
		ItemType.Carrot         => "Carrot",
		ItemType.Berry          => "Berry",
		ItemType.Cherry         => "Cherry",
		ItemType.Peach          => "Peach",
		ItemType.Pear           => "Pear",
		ItemType.Wheat          => "Wheat",
		ItemType.WheatSeeds     => "Wheat Seeds",
		ItemType.RawMeat        => "Raw Meat",
		ItemType.HealthPotion   => "Health Potion",
		_                       => type.ToString()
	};

	// ── Category ───────────────────────────────────────────────────────────
	public static ItemCategory GetCategory(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.Category;
		return GetCategoryFallback(type);
	}
	private static ItemCategory GetCategoryFallback(ItemType type) => type switch
	{
		ItemType.Wood or ItemType.Stone or ItemType.Herb or
		ItemType.IronOre or ItemType.GoldOre or ItemType.Crystal or
		ItemType.Coal or ItemType.Leather or ItemType.Bone or ItemType.Fiber or
		ItemType.IronIngot or ItemType.GoldIngot or ItemType.Wheat or ItemType.WheatSeeds or
		ItemType.RawMeat
			=> ItemCategory.Resource,

		ItemType.Axe or ItemType.Pickaxe or ItemType.Shovel or ItemType.FishingRod or
		ItemType.WateringCan or ItemType.WoodenTorch
			=> ItemCategory.Tool,

		ItemType.IronSword or ItemType.GoldSword or ItemType.Spear or ItemType.Bow
			=> ItemCategory.Weapon,

		ItemType.LeatherHelmet or ItemType.LeatherArmor or ItemType.LeatherBoots or
		ItemType.IronHelmet or ItemType.IronArmor or ItemType.IronBoots
			=> ItemCategory.Armor,

		ItemType.Apple or ItemType.Mushroom or ItemType.Bread or
		ItemType.Cheese or ItemType.CookedMeat or ItemType.Carrot or ItemType.Berry or
		ItemType.Cherry or ItemType.Peach or ItemType.Pear
			=> ItemCategory.Food,

		ItemType.HealthPotion
			=> ItemCategory.Potion,

		_ => ItemCategory.Resource
	};

	// ── Description ────────────────────────────────────────────────────────
	public static string GetDescription(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.Description;
		return GetDescriptionFallback(type);
	}
	private static string GetDescriptionFallback(ItemType type) => type switch
	{
		ItemType.Wood          => "Basic building material gathered from trees.",
		ItemType.Stone         => "Sturdy material mined from rocks.",
		ItemType.Herb          => "A medicinal plant with healing properties.",
		ItemType.IronOre       => "Raw iron ready to be smelted.",
		ItemType.GoldOre       => "Precious golden ore.",
		ItemType.Crystal       => "A shimmering gemstone.",
		ItemType.Coal          => "Fuel for the furnace. Burns hot.",
		ItemType.Leather       => "Tanned hide. Used for armor and gear.",
		ItemType.Bone          => "Sturdy bone. Has many uses.",
		ItemType.Fiber         => "Plant fiber for weaving and binding.",
		ItemType.IronIngot     => "Refined iron bar. Ready for forging.",
		ItemType.GoldIngot     => "Pure gold bar. Valuable and strong.",
		ItemType.Wheat         => "Golden grain ready for baking.",
		ItemType.WheatSeeds    => "Seeds for growing wheat.",
		ItemType.RawMeat      => "Fresh meat that should be cooked.",
		ItemType.Axe           => "Used to chop trees more efficiently.",
		ItemType.Pickaxe       => "Used to mine rocks more efficiently.",
		ItemType.Shovel        => "Used to dig and till soil for farming.",
		ItemType.FishingRod    => "Cast your line and catch fish.",
		ItemType.WateringCan   => "Waters tilled soil for crops.",
		ItemType.WoodenTorch   => "A warm light source.",
		ItemType.IronSword     => "A sturdy iron blade.",
		ItemType.GoldSword     => "A gleaming golden sword. Very powerful.",
		ItemType.Spear         => "Long reach. Strikes from a distance.",
		ItemType.Bow           => "Fires arrows at distant targets.",
		ItemType.LeatherHelmet => "A tough leather cap. Light protection.",
		ItemType.LeatherArmor  => "Supple leather chest piece.",
		ItemType.LeatherBoots  => "Sturdy leather boots.",
		ItemType.IronHelmet    => "Forged iron helm. Solid protection.",
		ItemType.IronArmor     => "Heavy iron chest plate.",
		ItemType.IronBoots     => "Iron-plated boots. Strong defence.",
		ItemType.Apple         => "A fresh apple. Restores 2 HP.",
		ItemType.Mushroom      => "A forest mushroom. Restores 1 HP.",
		ItemType.Bread         => "Freshly baked bread. Restores 3 HP.",
		ItemType.Cheese        => "Aged cheese wheel. Restores 2 HP.",
		ItemType.CookedMeat    => "Grilled meat. Restores 3 HP.",
		ItemType.Carrot        => "A fresh carrot. Restores 1 HP.",
		ItemType.Berry         => "Wild berries. Restores 1 HP.",
		ItemType.Cherry        => "Sweet cherry. Restores 2 HP.",
		ItemType.Peach         => "Juicy peach. Restores 2 HP.",
		ItemType.Pear          => "Ripe pear. Restores 2 HP.",
		ItemType.HealthPotion  => "Concentrated healing brew. Restores 5 HP.",
		_                      => ""
	};

	// ── Equip slot (null = not equippable) ────────────────────────────────
	public static EquipSlot? GetEquipSlot(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.GetEquipSlot();
		return GetEquipSlotFallback(type);
	}
	private static EquipSlot? GetEquipSlotFallback(ItemType type) => type switch
	{
		ItemType.IronSword or ItemType.GoldSword or ItemType.Spear or ItemType.Bow
			=> EquipSlot.Weapon,
		ItemType.Axe or ItemType.Pickaxe or ItemType.Shovel or ItemType.FishingRod or
		ItemType.WateringCan
			=> EquipSlot.Weapon,
		ItemType.LeatherHelmet or ItemType.IronHelmet
			=> EquipSlot.Head,
		ItemType.LeatherArmor or ItemType.IronArmor
			=> EquipSlot.Body,
		ItemType.LeatherBoots or ItemType.IronBoots
			=> EquipSlot.Boots,
		_ => null
	};

	// ── Stats ──────────────────────────────────────────────────────────────
	public static int GetWeaponDamage(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.WeaponDamage;
		return GetWeaponDamageFallback(type);
	}
	private static int GetWeaponDamageFallback(ItemType type) => type switch
	{
		ItemType.GoldSword  => 5,
		ItemType.Spear      => 4,
		ItemType.IronSword  => 3,
		ItemType.Bow        => 3,
		ItemType.Axe        => 2,
		ItemType.Pickaxe    => 2,
		ItemType.Shovel     => 1,
		ItemType.FishingRod => 1,
		_                   => 0
	};
	
	public static string GetToolPrefix(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return "attack";
		
		string idLower = itemId.ToLower();
		if (idLower.Contains("axe"))     return "axe";
		if (idLower.Contains("pickaxe")) return "pickaxe";
		if (idLower.Contains("shovel"))  return "shovel";
		
		return "attack";
	}

	public static int GetArmorRating(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.ArmorRating;
		return GetArmorRatingFallback(type);
	}
	private static int GetArmorRatingFallback(ItemType type) => type switch
	{
		ItemType.IronArmor     => 4,
		ItemType.IronHelmet    => 2,
		ItemType.IronBoots     => 2,
		ItemType.LeatherArmor  => 2,
		ItemType.LeatherHelmet => 1,
		ItemType.LeatherBoots  => 1,
		_                      => 0
	};

	public static int GetHealAmount(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.HealAmount;
		return GetHealAmountFallback(type);
	}
	private static int GetHealAmountFallback(ItemType type) => type switch
	{
		ItemType.HealthPotion => 5,
		ItemType.CookedMeat   => 3,
		ItemType.Bread        => 3,
		ItemType.Apple        => 2,
		ItemType.Cheese       => 2,
		ItemType.Mushroom     => 1,
		ItemType.Carrot       => 1,
		ItemType.Berry        => 1,
		ItemType.Cherry       => 2,
		ItemType.Peach        => 2,
		ItemType.Pear         => 2,
		_                     => 0
	};

	public static int GetMaxStack(ItemType type)
	{
		var d = GetData(type);
		if (d != null) return d.MaxStack;
		var cat = GetCategory(type);
		return cat switch
		{
			ItemCategory.Resource => 99,
			ItemCategory.Food     => 20,
			ItemCategory.Potion   => 10,
			_                     => 1
		};
	}

	/// <summary>Returns the Icon texture directly from the database (preferred over path+region).</summary>
	public static Texture2D GetIcon(ItemType type)
	{
		var d = GetData(type);
		return d?.Icon;
	}

	// ── Icon atlas (fallback when no database) ─────────────────────────────
	public static string GetIconTexturePath(ItemType type) => type switch
	{
		ItemType.Wood => WoodDecor,
		ItemType.Herb => HerbDecor,

		// Resources — use no-outline resource sheet
		ItemType.Stone or ItemType.IronOre or ItemType.GoldOre or ItemType.Crystal or
		ItemType.Coal or ItemType.IronIngot or ItemType.GoldIngot or
		ItemType.Leather or ItemType.Bone or ItemType.Fiber
			=> ResNoOutline,

		// Tools & weapons — tool outline sheet
		ItemType.Axe or ItemType.Pickaxe or ItemType.Shovel or ItemType.FishingRod or
		ItemType.WateringCan or ItemType.WoodenTorch or
		ItemType.IronSword or ItemType.GoldSword or ItemType.Spear or ItemType.Bow
			=> ToolOutline,

		// Armor — use resource outline sheet (shield icon from tools for body)
		ItemType.LeatherHelmet or ItemType.LeatherArmor or ItemType.LeatherBoots or
		ItemType.IronHelmet or ItemType.IronArmor or ItemType.IronBoots
			=> ResOutline,

		// Food
		ItemType.Apple or ItemType.Mushroom or ItemType.Bread or
		ItemType.Cheese or ItemType.CookedMeat or ItemType.Carrot or ItemType.Berry or
		ItemType.Cherry or ItemType.Peach or ItemType.Pear
			=> FoodOutline,

		// Potions — use other icons (flower/potion sheet)
		ItemType.HealthPotion => OtherOutline,

		_ => ResOutline
	};

	public static Rect2 GetIconRegion(ItemType type) => type switch
	{
		// ── Outdoor decor sprites (not 16x16 grid) ────────────────────────
		ItemType.Wood => new Rect2(67, 115, 26, 11),
		ItemType.Herb => new Rect2(35, 3,   11, 10),

		// ── Resources (no-outline, 6×6 grid of 16×16) ────────────────────
		ItemType.IronOre    => new Rect2(0,  0,  16, 16),  // row 0, col 0
		ItemType.GoldOre    => new Rect2(0,  16, 16, 16),  // row 1, col 0
		ItemType.Crystal    => new Rect2(16, 0,  16, 16),  // row 0, col 1
		ItemType.Stone      => new Rect2(0,  80, 16, 16),  // row 5, col 0
		ItemType.Coal       => new Rect2(32, 64, 16, 16),  // row 4, col 2 — black coal
		ItemType.IronIngot  => new Rect2(32, 0,  16, 16),  // row 0, col 2 — silver bar
		ItemType.GoldIngot  => new Rect2(32, 32, 16, 16),  // row 2, col 2 — gold bar
		ItemType.Leather    => new Rect2(48, 64, 16, 16),  // row 4, col 3 — leather piece
		ItemType.Bone       => new Rect2(16, 64, 16, 16),  // row 4, col 1 — white bone
		ItemType.Fiber      => new Rect2(16, 80, 16, 16),  // row 5, col 1 — rope/twine

		// ── Armor (outline resource sheet, 6×6 grid of 16×16) ─────────────
		// Leather tier — brown-toned resource icons
		ItemType.LeatherHelmet => new Rect2(64, 64, 16, 16),  // row 4, col 4 — dark leather
		ItemType.LeatherArmor  => new Rect2(48, 64, 16, 16),  // row 4, col 3 — leather piece
		ItemType.LeatherBoots  => new Rect2(32, 16, 16, 16),  // row 1, col 2 — copper piece
		// Iron tier — grey/blue-toned resource icons
		ItemType.IronHelmet    => new Rect2(32, 0,  16, 16),  // row 0, col 2 — steel bar
		ItemType.IronArmor     => new Rect2(16, 0,  16, 16),  // row 0, col 1 — dark iron
		ItemType.IronBoots     => new Rect2(0,  80, 16, 16),  // row 5, col 0 — steel piece

		// ── Tools & Weapons (outline, 10×1 grid of 16×16) ─────────────────
		ItemType.Spear      => new Rect2(0,   0, 16, 16),  // spear/lance
		ItemType.Pickaxe    => new Rect2(32,  0, 16, 16),
		ItemType.Axe        => new Rect2(48,  0, 16, 16),
		ItemType.IronSword  => new Rect2(64,  0, 16, 16),
		ItemType.Shovel     => new Rect2(80,  0, 16, 16),
		ItemType.GoldSword  => new Rect2(96,  0, 16, 16),  // hammer → gold weapon
		ItemType.Bow        => new Rect2(112, 0, 16, 16),
		ItemType.FishingRod  => new Rect2(144, 0, 16, 16),
		ItemType.WateringCan => new Rect2(128, 0, 16, 16),
		ItemType.WoodenTorch => new Rect2(16,  0, 16, 16),

		// ── Food (outline, 8×12 grid of 16×16) ────────────────────────────
		ItemType.CookedMeat => new Rect2(0,  0,  16, 16),  // row 0, col 0 — meat steak
		ItemType.Bread      => new Rect2(16, 0,  16, 16),  // row 0, col 1 — bread loaf
		ItemType.Apple      => new Rect2(0,  16, 16, 16),  // row 1, col 0
		ItemType.Mushroom   => new Rect2(16, 16, 16, 16),  // row 1, col 1
		ItemType.Cheese     => new Rect2(48, 0,  16, 16),  // row 0, col 3 — cheese/pastry
		ItemType.Carrot     => new Rect2(96, 0,  16, 16),  // row 0, col 6 — orange item
		ItemType.Wheat      => new Rect2(0,  112, 16, 16), // crops.png fallback - dummy pos
		ItemType.WheatSeeds => new Rect2(16, 112, 16, 16),
		ItemType.RawMeat    => new Rect2(32, 112, 16, 16),

		// ── Potions (other outline, 5×3 grid of 16×16) ────────────────────
		ItemType.HealthPotion => new Rect2(0, 0, 16, 16),  // row 0, col 0 — red flower/potion

		_ => new Rect2(0, 0, 16, 16)
	};
}
