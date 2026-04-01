using Godot;

/// <summary>
/// Resource-based item definition. Create .tres files in the editor
/// to add or modify items without touching code.
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public ItemCategory Category = ItemCategory.Resource;
	[Export(PropertyHint.MultilineText)] public string Description = "";

	[ExportGroup("Stats")]
	[Export] public int WeaponDamage;
	[Export] public int ArmorRating;
	[Export] public int HealAmount;
	[Export] public int MaxStack = 99;

	[ExportGroup("Equipment")]
	[Export] public bool CanEquip;
	[Export] public EquipSlot EquipSlotType;

	[ExportGroup("Visuals")]
	[Export] public Texture2D Icon;

	public EquipSlot? GetEquipSlot() => CanEquip ? EquipSlotType : null;
}
