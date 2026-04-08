using Godot;

public enum QuestObjectiveType
{
	Collect,   // Have N of an item in inventory
	Kill,      // Defeat N enemies of a type
	Talk,      // Speak to a specific NPC
}

/// <summary>
/// Resource-based quest definition. Drop .tres files in res://data/quests/
/// and they'll be auto-loaded by QuestManager at startup.
/// </summary>
[GlobalClass]
public partial class QuestData : Resource
{
	[Export] public string Id = "";
	[Export] public string Title = "";
	[Export(PropertyHint.MultilineText)] public string Description = "";
	[Export(PropertyHint.MultilineText)] public string CompletionText = "Thank you, traveller!";

	[ExportGroup("Objective")]
	[Export] public QuestObjectiveType ObjectiveType = QuestObjectiveType.Collect;
	/// <summary>For Collect: ItemId. For Kill: enemy type name (e.g. "Slime"). For Talk: NPC name.</summary>
	[Export] public string TargetId = "";
	[Export] public int    TargetCount = 1;

	[ExportGroup("Rewards")]
	[Export] public int      GoldReward;
	[Export] public string[] ItemRewardIds   = System.Array.Empty<string>();
	[Export] public int[]    ItemRewardCount = System.Array.Empty<int>();

	[ExportGroup("Visuals")]
	[Export] public Texture2D Icon;
}
