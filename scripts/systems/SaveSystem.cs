using System.Collections.Generic;
using Godot;

/// <summary>
/// Saves and loads all game state to user://ethry_save.json.
/// Call SaveSystem.Save() anywhere; Load() is called by MainMenu on "Continue".
/// Auto-saves at day boundary via DaySystem.DayAdvanced signal.
/// </summary>
public partial class SaveSystem : Node
{
	public static SaveSystem Instance { get; private set; }

	private const string SavePath = "user://ethry_save.json";

	public static bool HasSave() => FileAccess.FileExists(SavePath);

	public override void _Ready()
	{
		Instance = this;
		// Auto-save every new day
		if (DaySystem.Instance != null)
			DaySystem.Instance.DayAdvanced += (_, _, _) => Save();
	}

	// ── Save ───────────────────────────────────────────────────────────────
	public static void Save()
	{
		var inv = new Godot.Collections.Dictionary();
		foreach (var (id, cnt) in Inventory.Instance.Items)
			inv[id] = cnt;

		var equip = new Godot.Collections.Dictionary
		{
			{ "weapon", Equipment.Instance.GetSlotId(EquipSlot.Weapon) ?? "" },
			{ "head",   Equipment.Instance.GetSlotId(EquipSlot.Head)   ?? "" },
			{ "body",   Equipment.Instance.GetSlotId(EquipSlot.Body)   ?? "" },
			{ "boots",  Equipment.Instance.GetSlotId(EquipSlot.Boots)  ?? "" },
		};

		var data = new Godot.Collections.Dictionary
		{
			{ "version", 1 },
			{ "gold",    PlayerData.Instance.Gold },
			{ "day",     DaySystem.Instance.Day },
			{ "season",  DaySystem.Instance.SeasonIndex },
			{ "year",    DaySystem.Instance.Year },
			{ "inventory", inv },
			{ "equipment", equip },
		};

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PrintErr("SaveSystem: cannot open save file for writing.");
			return;
		}
		file.StoreString(Json.Stringify(data, "\t"));
		GD.Print($"SaveSystem: saved (Day {DaySystem.Instance.Day}, {PlayerData.Instance.Gold}g).");
	}

	// ── Load ───────────────────────────────────────────────────────────────
	public static void Load()
	{
		if (!HasSave()) return;

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		if (file == null) { GD.PrintErr("SaveSystem: save file missing."); return; }

		var parsed = Json.ParseString(file.GetAsText());
		if (parsed.VariantType != Variant.Type.Dictionary) { GD.PrintErr("SaveSystem: corrupt save."); return; }

		var data = parsed.AsGodotDictionary();

		// --- Economy & time ---
		int gold   = data.TryGetValue("gold",   out var gv) ? gv.AsInt32()   : 0;
		int day    = data.TryGetValue("day",    out var dv) ? dv.AsInt32()   : 1;
		int season = data.TryGetValue("season", out var sv) ? sv.AsInt32()   : 0;
		int year   = data.TryGetValue("year",   out var yv) ? yv.AsInt32()   : 1;

		// Restore via reflection on private fields — use the public methods where available
		RestoreGold(gold);
		RestoreDay(day, season, year);

		// --- Inventory ---
		if (data.TryGetValue("inventory", out var inv) && inv.VariantType == Variant.Type.Dictionary)
		{
			// Clear current
			foreach (var (id, _) in new Dictionary<string, int>(Inventory.Instance.Items))
				Inventory.Instance.RemoveItem(id, Inventory.Instance.GetCount(id));

			foreach (var (k, v) in inv.AsGodotDictionary())
				Inventory.Instance.AddItem(k.AsString(), v.AsInt32());
		}

		// --- Equipment ---
		if (data.TryGetValue("equipment", out var eq) && eq.VariantType == Variant.Type.Dictionary)
		{
			var eqDict = eq.AsGodotDictionary();
			RestoreEquipSlot(eqDict, "weapon", EquipSlot.Weapon);
			RestoreEquipSlot(eqDict, "head",   EquipSlot.Head);
			RestoreEquipSlot(eqDict, "body",   EquipSlot.Body);
			RestoreEquipSlot(eqDict, "boots",  EquipSlot.Boots);
		}

		GD.Print($"SaveSystem: loaded (Day {day}, {gold}g).");
	}

	// ── Helpers ────────────────────────────────────────────────────────────
	private static void RestoreGold(int amount)
	{
		// Spend all, then add saved amount
		PlayerData.Instance.SpendGold(PlayerData.Instance.Gold);
		if (amount > 0) PlayerData.Instance.AddGold(amount);
	}

	private static void RestoreDay(int day, int season, int year)
	{
		DaySystem.Instance.LoadState(day, season, year);
	}

	private static void RestoreEquipSlot(Godot.Collections.Dictionary d, string key, EquipSlot slot)
	{
		if (!d.TryGetValue(key, out var v)) return;
		string id = v.AsString();
		if (string.IsNullOrEmpty(id)) return;

		// Add temporarily if not in inventory, equip, then the equip call removes it from inv
		// Equipment.Equip() requires the item to be in inventory first
		bool hadIt = Inventory.Instance.HasItem(id);
		if (!hadIt) Inventory.Instance.AddItem(id, 1);
		Equipment.Instance.Equip(id);
	}
}
