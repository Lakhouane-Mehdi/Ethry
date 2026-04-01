using System.Collections.Generic;
using Godot;

/// <summary>
/// Shipping box interactable — place items to sell them for gold.
/// Press [E] to open, choose items with arrows, [E] again to ship selected item,
/// [Esc] to close. Gold is awarded immediately (simplified vs. Stardew's next-day payout).
/// </summary>
public partial class ShippingBox : Node2D
{
	// ── Item sell prices (gold per unit). Keys match ItemData.Id / ItemType.ToString()
	private static readonly Dictionary<string, int> Prices = new()
	{
		// Resources
		{ "Wood",       2  }, { "Stone",     1  }, { "Herb",      5  },
		{ "IronOre",    8  }, { "GoldOre",   18 }, { "Crystal",   25 },
		{ "Coal",       4  }, { "Leather",   10 }, { "Bone",      6  },
		{ "Fiber",      3  }, { "IronIngot", 20 }, { "GoldIngot", 45 },
		// Food
		{ "Apple",      8  }, { "Mushroom",  6  }, { "Bread",     12 },
		{ "Cheese",     14 }, { "CookedMeat",16 }, { "Carrot",    7  },
		{ "Berry",      5  }, { "HealthPotion", 30 },
	};
	private const int DefaultPrice = 3; // fallback for unknown items

	// ── Prompt / UI ─────────────────────────────────────────────────────────
	private bool    _playerNear;
	private bool    _open;
	private int     _selectedIdx;

	// Baked item list from inventory at open-time
	private readonly List<(string id, int count, int price)> _listing = new();

	// CanvasLayer UI
	private CanvasLayer    _layer;
	private Control        _screen;
	private Label          _titleLabel;
	private VBoxContainer  _itemList;
	private Label          _totalLabel;
	private Label          _hint;

	// Floating prompt
	private Control        _promptRoot;
	private Label          _promptLabel;

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		BuildPrompt();
		BuildShopUI();

		var area   = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var shape  = new CollisionShape2D();
		var circle = new CircleShape2D { Radius = 28f };
		shape.Shape = circle;
		area.AddChild(shape);
		AddChild(area);
		area.BodyEntered += b => { if (b.IsInGroup("player")) { _playerNear = true;  _promptLabel.Visible = true; } };
		area.BodyExited  += b => { if (b.IsInGroup("player")) { _playerNear = false; _promptLabel.Visible = false; if (_open) CloseBox(); } };
	}

	public override void _Process(double delta)
	{
		_promptRoot.GlobalPosition = GlobalPosition + new Vector2(-60, -38);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerNear && !_open && @event.IsActionPressed("interact"))
		{
			OpenBox();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (!_open) return;

		if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("interact"))
		{
			CloseBox();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (@event.IsActionPressed("ui_up"))
		{
			_selectedIdx = Mathf.Max(0, _selectedIdx - 1);
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_down"))
		{
			_selectedIdx = Mathf.Min(_listing.Count - 1, _selectedIdx + 1);
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_accept"))
		{
			ShipSelected();
			GetViewport().SetInputAsHandled();
		}
	}

	// ── Box flow ─────────────────────────────────────────────────────────
	private void OpenBox()
	{
		_listing.Clear();
		foreach (var (id, cnt) in Inventory.Instance.Items)
		{
			if (cnt <= 0) continue;
			int price = Prices.TryGetValue(id, out int p) ? p : DefaultPrice;
			_listing.Add((id, cnt, price));
		}

		if (_listing.Count == 0)
		{
			NotificationManager.Instance?.ShowWarning("Inventory is empty!");
			return;
		}

		_open        = true;
		_selectedIdx = 0;
		_screen.Visible = true;
		_promptLabel.Visible = false;
		GetTree().Paused = true;
		Refresh();
	}

	private void CloseBox()
	{
		_open           = false;
		_screen.Visible = false;
		GetTree().Paused   = false;
		if (_playerNear) _promptLabel.Visible = true;
	}

	private void ShipSelected()
	{
		if (_listing.Count == 0 || _selectedIdx >= _listing.Count) return;

		var (id, cnt, price) = _listing[_selectedIdx];
		int total = cnt * price;

		Inventory.Instance.RemoveItem(id, cnt);
		PlayerData.Instance.AddGold(total);

		var data = ItemDatabase.Instance?.Get(id);
		string name = data?.DisplayName ?? id;
		NotificationManager.Instance?.Show(
			$"Shipped {cnt}× {name}  +{total}g",
			new Color(1f, 0.88f, 0.28f));

		_listing.RemoveAt(_selectedIdx);
		if (_listing.Count == 0) { CloseBox(); return; }
		_selectedIdx = Mathf.Min(_selectedIdx, _listing.Count - 1);
		Refresh();
	}

	// ── UI refresh ─────────────────────────────────────────────────────────
	private void Refresh()
	{
		foreach (Node c in _itemList.GetChildren()) c.QueueFree();

		int grandTotal = 0;
		for (int i = 0; i < _listing.Count; i++)
		{
			var (id, cnt, price) = _listing[i];
			var data  = ItemDatabase.Instance?.Get(id);
			string nm = data?.DisplayName ?? id;
			int    val = cnt * price;
			grandTotal += val;

			bool selected = i == _selectedIdx;
			var row = new Label();
			row.Text = selected
				? $"▶  {nm}  ×{cnt}   →  {val} g   [Enter] ship all"
				: $"    {nm}  ×{cnt}   →  {val} g";
			row.AddThemeColorOverride("font_color",
				selected ? new Color(1f, 0.88f, 0.28f) : new Color(0.42f, 0.28f, 0.1f));
			row.AddThemeFontSizeOverride("font_size", 12);
			_itemList.AddChild(row);
		}

		_totalLabel.Text = $"Total value: {grandTotal} g";
		_hint.Text       = "[↑↓] select   [Enter] ship   [Esc] close";
	}

	// ── Build helpers ──────────────────────────────────────────────────────
	private void BuildPrompt()
	{
		var anchor = new Node2D { TopLevel = true };
		AddChild(anchor);
		_promptRoot = new Control();
		anchor.AddChild(_promptRoot);

		_promptLabel = new Label { Text = "[E] Shipping Box", Visible = false };
		_promptLabel.AddThemeColorOverride("font_color", Colors.White);
		_promptLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		_promptLabel.AddThemeFontSizeOverride("font_size", 11);
		_promptLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_promptLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		_promptRoot.AddChild(_promptLabel);
	}

	private void BuildShopUI()
	{
		_layer = new CanvasLayer { Layer = 55 };
		_layer.ProcessMode = ProcessModeEnum.Always;
		AddChild(_layer);

		_screen = new Control();
		_screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screen.ProcessMode = ProcessModeEnum.Always;
		_screen.Visible = false;
		_layer.AddChild(_screen);

		// Centre panel
		var atlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(4, 49, 40, 46) };
		var style = new StyleBoxTexture
		{
			Texture = atlas,
			TextureMarginLeft = 8, TextureMarginTop = 8,
			TextureMarginRight = 8, TextureMarginBottom = 10,
			ContentMarginLeft = 20, ContentMarginTop = 14,
			ContentMarginRight = 20, ContentMarginBottom = 14,
		};
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", style);
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft   = -280; panel.OffsetRight  = 280;
		panel.OffsetTop    = -200; panel.OffsetBottom = 200;
		_screen.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		panel.AddChild(vbox);

		_titleLabel = new Label { Text = "📦  SHIPPING BOX" };
		_titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.48f, 0.18f));
		_titleLabel.AddThemeFontSizeOverride("font_size", 16);
		_titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_titleLabel);

		var sep = new HSeparator();
		sep.Modulate = new Color(0.55f, 0.38f, 0.18f, 0.5f);
		vbox.AddChild(sep);

		_itemList = new VBoxContainer();
		_itemList.AddThemeConstantOverride("separation", 4);
		vbox.AddChild(_itemList);

		var sep2 = new HSeparator();
		sep2.Modulate = new Color(0.55f, 0.38f, 0.18f, 0.5f);
		vbox.AddChild(sep2);

		_totalLabel = new Label { Text = "Total value: 0 g" };
		_totalLabel.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.28f));
		_totalLabel.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(_totalLabel);

		_hint = new Label { Text = "[↑↓] select   [Enter] ship   [Esc] close" };
		_hint.AddThemeColorOverride("font_color", new Color(0.42f, 0.28f, 0.1f, 0.8f));
		_hint.AddThemeFontSizeOverride("font_size", 10);
		_hint.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_hint);
	}
}
