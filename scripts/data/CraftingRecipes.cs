public struct CraftingRecipe
{
	public ItemType Result;
	public int ResultAmount;
	public (ItemType type, int amount)[] Ingredients;
}

/// <summary>
/// Recipe lists for each crafting station type.
/// CraftingTable = basic recipes (tools, leather, food).
/// Furnace       = smelting ores into ingots.
/// Anvil         = forging metal gear from ingots.
/// </summary>
public static class CraftingRecipes
{
	// ── Crafting Table (basic, available from the start) ───────────────────
	public static readonly CraftingRecipe[] All = new[]
	{
		// Tools
		new CraftingRecipe
		{
			Result = ItemType.Axe, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 3), (ItemType.Stone, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.Pickaxe, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 3), (ItemType.Stone, 3) }
		},
		new CraftingRecipe
		{
			Result = ItemType.Shovel, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 2), (ItemType.Stone, 1) }
		},
		new CraftingRecipe
		{
			Result = ItemType.FishingRod, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 3), (ItemType.Fiber, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.WateringCan, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Stone, 2), (ItemType.Wood, 2) }
		},

		// Weapons — basic
		new CraftingRecipe
		{
			Result = ItemType.Spear, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 4), (ItemType.Stone, 2), (ItemType.Fiber, 1) }
		},
		new CraftingRecipe
		{
			Result = ItemType.Bow, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 3), (ItemType.Fiber, 3) }
		},

		// Leather armor
		new CraftingRecipe
		{
			Result = ItemType.LeatherHelmet, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Leather, 2), (ItemType.Fiber, 1) }
		},
		new CraftingRecipe
		{
			Result = ItemType.LeatherArmor, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Leather, 4), (ItemType.Fiber, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.LeatherBoots, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Leather, 2), (ItemType.Fiber, 1) }
		},

		// Food
		new CraftingRecipe
		{
			Result = ItemType.Bread, ResultAmount = 2,
			Ingredients = new[] { (ItemType.Herb, 3), (ItemType.Wood, 1) }
		},
		new CraftingRecipe
		{
			Result = ItemType.CookedMeat, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Bone, 1), (ItemType.Coal, 1) }
		},

		// Potions
		new CraftingRecipe
		{
			Result = ItemType.HealthPotion, ResultAmount = 1,
			Ingredients = new[] { (ItemType.Herb, 3), (ItemType.Crystal, 1) }
		},
	};

	// ── Furnace (smelting) ────────────────────────────────────────────────
	public static readonly CraftingRecipe[] Furnace = new[]
	{
		new CraftingRecipe
		{
			Result = ItemType.IronIngot, ResultAmount = 1,
			Ingredients = new[] { (ItemType.IronOre, 2), (ItemType.Coal, 1) }
		},
		new CraftingRecipe
		{
			Result = ItemType.GoldIngot, ResultAmount = 1,
			Ingredients = new[] { (ItemType.GoldOre, 2), (ItemType.Coal, 1) }
		},
	};

	// ── Anvil (forging metal gear) ────────────────────────────────────────
	public static readonly CraftingRecipe[] Anvil = new[]
	{
		new CraftingRecipe
		{
			Result = ItemType.IronSword, ResultAmount = 1,
			Ingredients = new[] { (ItemType.IronIngot, 3), (ItemType.Wood, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.GoldSword, ResultAmount = 1,
			Ingredients = new[] { (ItemType.GoldIngot, 3), (ItemType.IronIngot, 1), (ItemType.Wood, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.IronHelmet, ResultAmount = 1,
			Ingredients = new[] { (ItemType.IronIngot, 2), (ItemType.Leather, 1) }
		},
		new CraftingRecipe
		{
			Result = ItemType.IronArmor, ResultAmount = 1,
			Ingredients = new[] { (ItemType.IronIngot, 4), (ItemType.Leather, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.IronBoots, ResultAmount = 1,
			Ingredients = new[] { (ItemType.IronIngot, 2), (ItemType.Leather, 1) }
		},
	};

	public static bool CanCraft(CraftingRecipe recipe)
	{
		foreach (var (type, amount) in recipe.Ingredients)
			if (!Inventory.Instance.HasItem(type, amount)) return false;
		return true;
	}

	public static bool Craft(CraftingRecipe recipe)
	{
		if (!CanCraft(recipe)) return false;
		foreach (var (type, amount) in recipe.Ingredients)
			Inventory.Instance.RemoveItem(type, amount);
		Inventory.Instance.AddItem(recipe.Result, recipe.ResultAmount);
		return true;
	}
}
