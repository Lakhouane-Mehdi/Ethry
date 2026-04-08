public struct CraftingRecipe
{
	public string Result;
	public int ResultAmount;
	public (string id, int amount)[] Ingredients;
}

/// <summary>
/// Recipe lists for each crafting station type.
/// All items are referenced by string ID (see ItemDatabase / data/items/*.tres).
/// </summary>
public static class CraftingRecipes
{
	// ── Crafting Table (basic tools and gear) ───────────────────────────
	public static readonly CraftingRecipe[] Table = new[]
	{
		new CraftingRecipe { Result = "Axe",           ResultAmount = 1, Ingredients = new[] { ("Wood", 3), ("Stone", 2) } },
		new CraftingRecipe { Result = "Pickaxe",       ResultAmount = 1, Ingredients = new[] { ("Wood", 3), ("Stone", 3) } },
		new CraftingRecipe { Result = "Shovel",        ResultAmount = 1, Ingredients = new[] { ("Wood", 2), ("Stone", 1) } },
		new CraftingRecipe { Result = "WateringCan",   ResultAmount = 1, Ingredients = new[] { ("Stone", 2), ("Wood", 2) } },
		new CraftingRecipe { Result = "FishingRod",    ResultAmount = 1, Ingredients = new[] { ("Wood", 3), ("Fiber", 2) } },
		new CraftingRecipe { Result = "Spear",         ResultAmount = 1, Ingredients = new[] { ("Wood", 4), ("Stone", 2), ("Fiber", 1) } },
		new CraftingRecipe { Result = "Bow",           ResultAmount = 1, Ingredients = new[] { ("Wood", 3), ("Fiber", 3) } },
		new CraftingRecipe { Result = "LeatherHelmet", ResultAmount = 1, Ingredients = new[] { ("Leather", 2), ("Fiber", 1) } },
		new CraftingRecipe { Result = "LeatherArmor",  ResultAmount = 1, Ingredients = new[] { ("Leather", 4), ("Fiber", 2) } },
		new CraftingRecipe { Result = "LeatherBoots",  ResultAmount = 1, Ingredients = new[] { ("Leather", 2), ("Fiber", 1) } },
	};

	// ── Furnace (smelting) ────────────────────────────────────────────────
	public static readonly CraftingRecipe[] Furnace = new[]
	{
		new CraftingRecipe { Result = "IronIngot", ResultAmount = 1, Ingredients = new[] { ("IronOre", 2), ("Coal", 1) } },
		new CraftingRecipe { Result = "GoldIngot", ResultAmount = 1, Ingredients = new[] { ("GoldOre", 2), ("Coal", 1) } },
	};

	// ── Anvil (forging metal gear) ────────────────────────────────────────
	public static readonly CraftingRecipe[] Anvil = new[]
	{
		new CraftingRecipe { Result = "IronSword",  ResultAmount = 1, Ingredients = new[] { ("IronIngot", 3), ("Wood", 2) } },
		new CraftingRecipe { Result = "GoldSword",  ResultAmount = 1, Ingredients = new[] { ("GoldIngot", 3), ("IronIngot", 1), ("Wood", 2) } },
		new CraftingRecipe { Result = "IronHelmet", ResultAmount = 1, Ingredients = new[] { ("IronIngot", 2), ("Leather", 1) } },
		new CraftingRecipe { Result = "IronArmor",  ResultAmount = 1, Ingredients = new[] { ("IronIngot", 4), ("Leather", 2) } },
		new CraftingRecipe { Result = "IronBoots",  ResultAmount = 1, Ingredients = new[] { ("IronIngot", 2), ("Leather", 1) } },
	};

	// ── Cooking (food and potions) ────────────────────────────────────────
	public static readonly CraftingRecipe[] Cooking = new[]
	{
		new CraftingRecipe { Result = "Bread",        ResultAmount = 1, Ingredients = new[] { ("Wheat", 3) } },
		new CraftingRecipe { Result = "CookedMeat",   ResultAmount = 1, Ingredients = new[] { ("RawMeat", 1), ("Coal", 1) } },
		new CraftingRecipe { Result = "HealthPotion", ResultAmount = 1, Ingredients = new[] { ("Herb", 3), ("Crystal", 1) } },
	};

	public static bool CanCraft(CraftingRecipe recipe)
	{
		foreach (var (id, amount) in recipe.Ingredients)
			if (!Inventory.Instance.HasItem(id, amount)) return false;
		return true;
	}

	public static bool Craft(CraftingRecipe recipe)
	{
		if (!CanCraft(recipe)) return false;
		foreach (var (id, amount) in recipe.Ingredients)
			Inventory.Instance.RemoveItem(id, amount);
		Inventory.Instance.AddItem(recipe.Result, recipe.ResultAmount);
		return true;
	}
}
