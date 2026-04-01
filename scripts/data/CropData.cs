using Godot;

/// <summary>
/// Data resource for a single crop type.
/// Create .tres files in res://data/crops/ via the editor.
/// GrowthStages[0] = seedling … GrowthStages[last] = mature.
/// </summary>
[GlobalClass]
public partial class CropData : Resource
{
	[Export] public string    Id            = "";
	[Export] public string    DisplayName   = "";
	[Export] public string    HarvestItemId = "";   // ItemData Id of what you harvest
	[Export] public int       GrowthDays    = 4;    // days to reach maturity
	[Export] public int       HarvestMin    = 1;
	[Export] public int       HarvestMax    = 3;
	[Export] public bool      Regrows       = false; // replants after harvest (berry-style)
	[Export] public int[]     ValidSeasons  = { 0, 1, 2, 3 }; // 0=Spring … 3=Winter

	[ExportGroup("Visuals")]
	// Sprite for each growth stage — assign in editor from crops.png atlas
	[Export] public Texture2D[] GrowthStages = System.Array.Empty<Texture2D>();
	// Icon shown in hotbar / inventory (same as harvested item usually)
	[Export] public Texture2D   SeedIcon;
}
