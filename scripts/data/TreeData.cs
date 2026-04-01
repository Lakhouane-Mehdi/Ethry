using Godot;

/// <summary>
/// Data resource for a tree type (oak, birch, fruit tree, etc.).
/// Create .tres files in res://data/trees/ via the editor.
///
/// Regular trees: chopped for wood, leave a stump, don't regrow.
/// Fruit trees:   shake/interact to harvest fruit, fruit regrows each season.
///                Chopping destroys the tree (like Stardew Valley).
/// </summary>
[GlobalClass]
public partial class TreeData : Resource
{
	[Export] public string Id = "";
	[Export] public string DisplayName = "";

	[ExportGroup("Chopping")]
	[Export] public int    Health       = 3;
	[Export] public string PrimaryDropId = "Wood";   // ItemData ID for main drop
	[Export] public int    PrimaryDropMin = 2;
	[Export] public int    PrimaryDropMax = 4;

	[ExportGroup("Fruit (optional)")]
	[Export] public bool   IsFruitTree    = false;
	[Export] public string FruitDropId    = "";       // e.g. "Apple", "Cherry"
	[Export] public int    FruitDropMin   = 1;
	[Export] public int    FruitDropMax   = 3;
	[Export] public int    FruitRegrowDays = 3;       // days between harvests
	[Export] public int[]  FruitSeasons   = { 0, 1, 2 }; // which seasons produce fruit

	[ExportGroup("Visuals")]
	[Export] public Texture2D TreeTexture;             // full-grown tree sprite
	[Export] public Texture2D StumpTexture;             // stump after chopping
	[Export] public Texture2D FruitTexture;             // tree with fruit on it (optional overlay)
	[Export] public Texture2D[] GrowthStages = System.Array.Empty<Texture2D>(); // for plantable trees
}
