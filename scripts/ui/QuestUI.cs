using Godot;

/// <summary>
/// Press Q to open the quest journal — a parchment-styled panel showing
/// active and completed quests with their objectives, progress and rewards.
/// </summary>
public partial class QuestUI : CanvasLayer
{
	private bool _isOpen;

	private Control       _root;
	private Control       _overlay;
	private VBoxContainer _activeList;
	private VBoxContainer _completedList;
	private Label         _emptyHint;

	// Wood/parchment palette to match the rest of the game
	private static readonly Color Parchment    = new(0.96f, 0.86f, 0.62f, 1f);
	private static readonly Color WoodDark     = new(0.36f, 0.20f, 0.08f, 1f);
	private static readonly Color WoodMid      = new(0.62f, 0.40f, 0.18f, 1f);
	private static readonly Color GoldAccent   = new(0.95f, 0.62f, 0.18f, 1f);
	private static readonly Color TextPrimary  = new(0.22f, 0.10f, 0.04f, 1f);
	private static readonly Color TextSecond   = new(0.40f, 0.22f, 0.08f, 0.85f);
	private static readonly Color GreenOk      = new(0.22f, 0.55f, 0.18f, 1f);

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 40;

		// ── Backdrop ──
		_overlay = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.45f),
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_overlay);

		// ── Root panel ──
		_root = new Control
		{
			Visible = false,
		};
		_root.SetAnchorsPreset(Control.LayoutPreset.Center);
		_root.Size = new Vector2(720, 560);
		_root.Position = new Vector2(-360, -280);
		AddChild(_root);

		var panel = new PanelContainer();
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.AddThemeStyleboxOverride("panel", MakeParchment());
		panel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_root.AddChild(panel);

		var mainVBox = new VBoxContainer();
		mainVBox.AddThemeConstantOverride("separation", 12);
		panel.AddChild(mainVBox);

		// ── Title ──
		var title = new Label
		{
			Text = "QUEST JOURNAL",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeColorOverride("font_color", TextPrimary);
		title.AddThemeFontSizeOverride("font_size", 26);
		mainVBox.AddChild(title);

		mainVBox.AddChild(MakeSeparator());

		// ── Scrollable body ──
		var scroll = new ScrollContainer
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		mainVBox.AddChild(scroll);

		var body = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", 10);
		scroll.AddChild(body);

		body.AddChild(MakeSectionHeader("Active"));
		_activeList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_activeList.AddThemeConstantOverride("separation", 8);
		body.AddChild(_activeList);

		_emptyHint = new Label
		{
			Text = "  No active quests. Talk to villagers or read notice boards.",
		};
		_emptyHint.AddThemeColorOverride("font_color", TextSecond);
		_emptyHint.AddThemeFontSizeOverride("font_size", 13);
		_activeList.AddChild(_emptyHint);

		body.AddChild(MakeSectionHeader("Completed"));
		_completedList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_completedList.AddThemeConstantOverride("separation", 6);
		body.AddChild(_completedList);

		mainVBox.AddChild(MakeSeparator());

		var hint = new Label
		{
			Text = "[Q] or [ESC] to Close",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		hint.AddThemeColorOverride("font_color", TextSecond);
		hint.AddThemeFontSizeOverride("font_size", 12);
		mainVBox.AddChild(hint);

		// Listen for quest events
		if (QuestManager.Instance != null)
		{
			QuestManager.Instance.QuestStarted    += _ => Refresh();
			QuestManager.Instance.QuestProgressed += (_, _, _) => Refresh();
			QuestManager.Instance.QuestCompleted  += _ => Refresh();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		bool toggle = @event.IsActionPressed("toggle_quests") ||
					  (@event.IsActionPressed("ui_cancel") && _isOpen);
		if (!toggle) return;

		_isOpen = !_isOpen;
		_root.Visible = _isOpen;
		_overlay.Visible = _isOpen;
		GetTree().Paused = _isOpen;
		AudioManager.Instance?.PlaySfxFlat("ui_click");
		if (_isOpen) Refresh();
		GetViewport().SetInputAsHandled();
	}

	// ── Refresh ────────────────────────────────────────────────────────────
	private void Refresh()
	{
		if (QuestManager.Instance == null) return;

		// Clear existing rows (except the empty hint, which we keep at index 0)
		for (int i = _activeList.GetChildCount() - 1; i >= 0; i--)
		{
			var child = _activeList.GetChild(i);
			if (child != _emptyHint) child.QueueFree();
		}
		foreach (Node n in _completedList.GetChildren()) n.QueueFree();

		// Active
		bool anyActive = false;
		foreach (var (id, progress) in QuestManager.Instance.Active)
		{
			var q = QuestManager.Instance.Get(id);
			if (q == null) continue;
			_activeList.AddChild(BuildQuestCard(q, progress, completed: false));
			anyActive = true;
		}
		_emptyHint.Visible = !anyActive;

		// Completed
		foreach (var id in QuestManager.Instance.Completed)
		{
			var q = QuestManager.Instance.Get(id);
			if (q == null) continue;
			_completedList.AddChild(BuildQuestCard(q, q.TargetCount, completed: true));
		}

		if (_completedList.GetChildCount() == 0)
		{
			var none = new Label { Text = "  None yet." };
			none.AddThemeColorOverride("font_color", TextSecond);
			none.AddThemeFontSizeOverride("font_size", 13);
			_completedList.AddChild(none);
		}
	}

	private Control BuildQuestCard(QuestData q, int progress, bool completed)
	{
		var card = new PanelContainer();
		card.AddThemeStyleboxOverride("panel", MakeCardStyle(completed));

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 14);
		card.AddChild(hbox);

		// Icon column
		var iconRect = new TextureRect
		{
			CustomMinimumSize = new Vector2(48, 48),
			ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode   = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		iconRect.Texture = q.Icon ?? GetTargetIcon(q);
		hbox.AddChild(iconRect);

		// Text column
		var vbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		vbox.AddThemeConstantOverride("separation", 4);
		hbox.AddChild(vbox);

		var titleLbl = new Label { Text = q.Title };
		titleLbl.AddThemeColorOverride("font_color", completed ? TextSecond : TextPrimary);
		titleLbl.AddThemeFontSizeOverride("font_size", 17);
		vbox.AddChild(titleLbl);

		var descLbl = new Label
		{
			Text = q.Description,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		descLbl.AddThemeColorOverride("font_color", TextSecond);
		descLbl.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(descLbl);

		// Progress / objective row
		string verb = q.ObjectiveType switch
		{
			QuestObjectiveType.Collect => "Gather",
			QuestObjectiveType.Kill    => "Defeat",
			QuestObjectiveType.Talk    => "Speak with",
			_ => "",
		};
		string targetName = q.ObjectiveType == QuestObjectiveType.Collect
			? (ItemDatabase.Instance?.Get(q.TargetId)?.DisplayName ?? q.TargetId)
			: q.TargetId;

		var objLbl = new Label
		{
			Text = completed
				? $"✓ {verb} {targetName}  ({q.TargetCount}/{q.TargetCount})"
				: $"• {verb} {targetName}  ({progress}/{q.TargetCount})",
		};
		objLbl.AddThemeColorOverride("font_color", completed ? GreenOk : TextPrimary);
		objLbl.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(objLbl);

		// Rewards row
		string rewards = "";
		if (q.GoldReward > 0) rewards += $"{q.GoldReward}g  ";
		for (int i = 0; i < q.ItemRewardIds.Length; i++)
		{
			int amt = i < q.ItemRewardCount.Length ? q.ItemRewardCount[i] : 1;
			string name = ItemDatabase.Instance?.Get(q.ItemRewardIds[i])?.DisplayName ?? q.ItemRewardIds[i];
			rewards += $"{name}×{amt}  ";
		}
		if (!string.IsNullOrEmpty(rewards))
		{
			var rewardLbl = new Label { Text = "Reward:  " + rewards };
			rewardLbl.AddThemeColorOverride("font_color", GoldAccent);
			rewardLbl.AddThemeFontSizeOverride("font_size", 12);
			vbox.AddChild(rewardLbl);
		}

		return card;
	}

	private Texture2D GetTargetIcon(QuestData q)
	{
		if (q.ObjectiveType == QuestObjectiveType.Collect)
			return ItemDatabase.Instance?.Get(q.TargetId)?.Icon;
		return null;
	}

	// ── Style helpers ──────────────────────────────────────────────────────
	private StyleBoxFlat MakeParchment()
	{
		return new StyleBoxFlat
		{
			BgColor = Parchment,
			BorderColor = WoodDark,
			BorderWidthLeft = 4, BorderWidthTop = 4,
			BorderWidthRight = 4, BorderWidthBottom = 4,
			CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
			ContentMarginLeft = 22, ContentMarginRight = 22,
			ContentMarginTop = 18, ContentMarginBottom = 18,
			ShadowColor = new Color(0, 0, 0, 0.4f),
			ShadowSize = 6,
		};
	}

	private StyleBoxFlat MakeCardStyle(bool completed)
	{
		return new StyleBoxFlat
		{
			BgColor = completed
				? new Color(0.78f, 0.66f, 0.45f, 0.7f)
				: new Color(0.92f, 0.78f, 0.50f, 1f),
			BorderColor = completed ? WoodMid : WoodDark,
			BorderWidthLeft = 2, BorderWidthTop = 2,
			BorderWidthRight = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
			ContentMarginLeft = 12, ContentMarginRight = 12,
			ContentMarginTop = 10, ContentMarginBottom = 10,
		};
	}

	private Control MakeSectionHeader(string text)
	{
		var lbl = new Label { Text = "— " + text + " —" };
		lbl.AddThemeColorOverride("font_color", WoodDark);
		lbl.AddThemeFontSizeOverride("font_size", 16);
		return lbl;
	}

	private HSeparator MakeSeparator()
	{
		var sep = new HSeparator();
		sep.AddThemeColorOverride("color", new Color(WoodDark, 0.6f));
		return sep;
	}
}
