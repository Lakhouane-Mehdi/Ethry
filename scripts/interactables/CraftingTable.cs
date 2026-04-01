using Godot;

/// <summary>
/// Basic crafting table — uses the default recipe list.
/// All interaction logic lives in the CraftingStation base class.
/// </summary>
public partial class CraftingTable : CraftingStation
{
	public CraftingTable()
	{
		StationTitle = "CRAFTING TABLE";
		PromptText   = "Press E to Craft";
		StationType  = 0;
	}
}
