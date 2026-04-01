using Godot;

/// <summary>
/// Anvil station — forges metal gear from ingots.
/// </summary>
public partial class Anvil : CraftingStation
{
	public Anvil()
	{
		StationTitle = "ANVIL";
		PromptText   = "Press E to Forge";
		StationType  = 2;
	}
}
