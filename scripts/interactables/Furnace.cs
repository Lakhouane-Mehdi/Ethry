using Godot;

/// <summary>
/// Furnace station — smelts ores into ingots.
/// </summary>
public partial class Furnace : CraftingStation
{
	public Furnace()
	{
		StationTitle = "FURNACE";
		PromptText   = "Press E to Smelt";
		StationType  = 1;
	}
}
