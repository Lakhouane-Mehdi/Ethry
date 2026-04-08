using System.Collections.Generic;
using Godot;

/// <summary>
/// Tracks active and completed quests. Auto-loads all .tres quest definitions
/// from res://data/quests/. Listens to Inventory for Collect-type progress,
/// and exposes ReportKill / ReportTalk for the other objective types.
/// </summary>
public partial class QuestManager : Node
{
	public static QuestManager Instance { get; private set; }

	[Signal] public delegate void QuestStartedEventHandler(string questId);
	[Signal] public delegate void QuestProgressedEventHandler(string questId, int current, int target);
	[Signal] public delegate void QuestCompletedEventHandler(string questId);

	private const string QuestFolder = "res://data/quests/";

	// All known quests (id → data)
	private readonly Dictionary<string, QuestData> _all = new();
	// Active quest progress (id → current count)
	private readonly Dictionary<string, int> _active = new();
	// Completed quests (set of ids)
	private readonly HashSet<string> _completed = new();

	public IReadOnlyDictionary<string, QuestData> All       => _all;
	public IReadOnlyDictionary<string, int>       Active    => _active;
	public IReadOnlyCollection<string>            Completed => _completed;

	public override void _Ready()
	{
		Instance = this;
		LoadAllQuests();

		if (Inventory.Instance != null)
			Inventory.Instance.Changed += OnInventoryChanged;
	}

	private void LoadAllQuests()
	{
		var dir = DirAccess.Open(QuestFolder);
		if (dir == null) { GD.Print("QuestManager: no quests folder."); return; }

		dir.ListDirBegin();
		string fname;
		while (!string.IsNullOrEmpty(fname = dir.GetNext()))
		{
			if (dir.CurrentIsDir()) continue;
			if (!fname.EndsWith(".tres") && !fname.EndsWith(".res")) continue;

			var quest = GD.Load<QuestData>(QuestFolder + fname);
			if (quest != null && !string.IsNullOrEmpty(quest.Id))
				_all[quest.Id] = quest;
		}
		dir.ListDirEnd();
		GD.Print($"QuestManager: loaded {_all.Count} quests.");
	}

	// ── Public API ─────────────────────────────────────────────────────────
	public QuestData Get(string id)
		=> _all.TryGetValue(id, out var q) ? q : null;

	public bool IsActive(string id)    => _active.ContainsKey(id);
	public bool IsCompleted(string id) => _completed.Contains(id);

	public void StartQuest(string id)
	{
		if (IsActive(id) || IsCompleted(id)) return;
		var q = Get(id);
		if (q == null) { GD.PrintErr($"QuestManager: unknown quest '{id}'"); return; }

		_active[id] = 0;
		EmitSignal(SignalName.QuestStarted, id);

		NotificationManager.Instance?.Show(
			$"New Quest: {q.Title}", new Color(0.95f, 0.82f, 0.35f));
		AudioManager.Instance?.PlaySfxFlat("ui_click");

		// Immediately re-check inventory (might already have the items)
		if (q.ObjectiveType == QuestObjectiveType.Collect)
			RefreshCollectProgress(id, q);
	}

	public int GetProgress(string id)
		=> _active.TryGetValue(id, out var v) ? v : 0;

	public int GetTarget(string id)
		=> Get(id)?.TargetCount ?? 0;

	/// <summary>Call when an enemy is defeated. enemyType should match QuestData.TargetId.</summary>
	public void ReportKill(string enemyType)
	{
		foreach (var (id, _) in new Dictionary<string, int>(_active))
		{
			var q = Get(id);
			if (q == null || q.ObjectiveType != QuestObjectiveType.Kill) continue;
			if (q.TargetId != enemyType) continue;
			Bump(id, q, _active[id] + 1);
		}
	}

	/// <summary>Call when an NPC dialogue starts/ends. npcName matches QuestData.TargetId.</summary>
	public void ReportTalk(string npcName)
	{
		foreach (var (id, _) in new Dictionary<string, int>(_active))
		{
			var q = Get(id);
			if (q == null || q.ObjectiveType != QuestObjectiveType.Talk) continue;
			if (q.TargetId != npcName) continue;
			Bump(id, q, q.TargetCount); // Talk = instant complete
		}
	}

	private void OnInventoryChanged()
	{
		foreach (var (id, _) in new Dictionary<string, int>(_active))
		{
			var q = Get(id);
			if (q == null || q.ObjectiveType != QuestObjectiveType.Collect) continue;
			RefreshCollectProgress(id, q);
		}
	}

	private void RefreshCollectProgress(string id, QuestData q)
	{
		int have = Inventory.Instance?.GetCount(q.TargetId) ?? 0;
		Bump(id, q, have);
	}

	private void Bump(string id, QuestData q, int newProgress)
	{
		int clamped = Mathf.Min(newProgress, q.TargetCount);
		if (_active[id] == clamped) return;
		_active[id] = clamped;
		EmitSignal(SignalName.QuestProgressed, id, clamped, q.TargetCount);

		if (clamped >= q.TargetCount)
			Complete(id, q);
	}

	private void Complete(string id, QuestData q)
	{
		_active.Remove(id);
		_completed.Add(id);
		EmitSignal(SignalName.QuestCompleted, id);

		// Pay rewards
		if (q.GoldReward > 0)
			PlayerData.Instance?.AddGold(q.GoldReward);

		for (int i = 0; i < q.ItemRewardIds.Length; i++)
		{
			int amt = i < q.ItemRewardCount.Length ? q.ItemRewardCount[i] : 1;
			Inventory.Instance?.AddItem(q.ItemRewardIds[i], amt);
		}

		NotificationManager.Instance?.Show(
			$"Quest Complete: {q.Title}", new Color(0.42f, 0.85f, 0.32f));
		AudioManager.Instance?.PlaySfx("craft_success");
	}

	// ── Save/Load ──────────────────────────────────────────────────────────
	public Godot.Collections.Dictionary GetSaveData()
	{
		var act = new Godot.Collections.Dictionary();
		foreach (var (id, p) in _active) act[id] = p;

		var done = new Godot.Collections.Array();
		foreach (var id in _completed) done.Add(id);

		return new Godot.Collections.Dictionary { { "active", act }, { "completed", done } };
	}

	public void LoadSaveData(Godot.Collections.Dictionary d)
	{
		_active.Clear();
		_completed.Clear();
		if (d == null) return;

		if (d.TryGetValue("active", out var a) && a.VariantType == Variant.Type.Dictionary)
		{
			foreach (var (k, v) in a.AsGodotDictionary())
				_active[k.AsString()] = v.AsInt32();
		}
		if (d.TryGetValue("completed", out var c) && c.VariantType == Variant.Type.Array)
		{
			foreach (var v in c.AsGodotArray())
				_completed.Add(v.AsString());
		}
	}
}
