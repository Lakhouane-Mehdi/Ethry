public struct CraftingRecipe
{
	public ItemType Result;
	public int ResultAmount;
	public (ItemType type, int amount)[] Ingredients;
}

public static class CraftingRecipes
{
	public static readonly CraftingRecipe[] All = new[]
	{
		// Tools
		new CraftingRecipe
		{
			Result = ItemType.Axe,
			ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 3), (ItemType.Stone, 2) }
		},
		new CraftingRecipe
		{
			Result = ItemType.Pickaxe,
			ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 3), (ItemType.Stone, 3) }
		},
		new CraftingRecipe
		{
			Result = ItemType.Shovel,
			ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 2), (ItemType.Stone, 1) }
		},

		// Weapons
		new CraftingRecipe
		{
			Result = ItemType.IronSword,
			ResultAmount = 1,
			Ingredients = new[] { (ItemType.Wood, 2), (ItemType.IronOre, 3) }
		},
	};

	public static bool CanCraft(CraftingRecipe recipe)
	{
		foreach (var (type, amount) in recipe.Ingredients)
		{
			if (!Inventory.Instance.HasItem(type, amount))
				return false;
		}
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
