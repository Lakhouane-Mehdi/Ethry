using System.Collections.Generic;
using Godot;

/// <summary>
/// Shop entry — defines one purchasable item.
/// Add an array of these to ShopInventory and attach it to an NPC.
/// </summary>
[GlobalClass]
public partial class ShopEntry : Resource
{
	[Export] public string  ItemId    = "";
	[Export] public int     Price     = 10;
	[Export] public int     Stock     = -1;  // -1 = unlimited
}

/// <summary>
/// Attach to an NPC (or any Node2D) to give it a shop.
/// The shop opens when the player interacts if the NPC Role == Merchant.
/// Can also be used standalone on a shop-building Area2D.
/// </summary>
public partial class ShopUI : CanvasLayer
{
	[Export] public string        ShopTitle   = "General Store";
	[Export] public ShopEntry[]   Stock       = System.Array.Empty<ShopEntry>();

	// ── Public open/close ──────────────────────────────────────────────────
	public bool IsOpen => _open;

	[Export] public bool CanSell = true;
	[Export] public float SellPriceRatio = 0.5f; // Sell at 50% of buy price

	// ── State ──────────────────────────────────────────────────────────────
	private bool _open;
	private bool _sellMode;
	private int  _selectedIdx;
	private readonly List<ShopEntry> _stock = new();
	private readonly List<(string id, int count)> _sellableItems = new();

	// ── UI refs ────────────────────────────────────────────────────────────
	private Control        _screen;
	private Label          _titleLabel;
	private Label          _goldLabel;
	private VBoxContainer  _list;
	private Label          _descLabel;
	private Label          _hint;

	private static readonly Texture2D FramesTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png");

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Layer       = 55;
		ProcessMode = ProcessModeEnum.Always;

		// Auto-populate stock if none configured in editor
		if (Stock == null || Stock.Length == 0)
			Stock = GetDefaultStock();

		BuildUI();
	}

	private ShopEntry[] GetDefaultStock()
	{
		string shopName = ShopTitle?.ToLower() ?? "";
		if (shopName.Contains("katy") || shopName.Contains("general"))
		{
			return new[]
			{
				new ShopEntry { ItemId = "Wood",       Price = 5,  Stock = -1 },
				new ShopEntry { ItemId = "Stone",      Price = 8,  Stock = -1 },
				new ShopEntry { ItemId = "Fiber",      Price = 4,  Stock = -1 },
				new ShopEntry { ItemId = "Coal",       Price = 10, Stock = -1 },
				new ShopEntry { ItemId = "Herb",       Price = 12, Stock = -1 },
				new ShopEntry { ItemId = "WheatSeeds", Price = 6,  Stock = -1 },
			};
		}
		if (shopName.Contains("mike") || shopName.Contains("smith"))
		{
			return new[]
			{
				new ShopEntry { ItemId = "IronOre",     Price = 15,  Stock = -1 },
				new ShopEntry { ItemId = "GoldOre",     Price = 30,  Stock = -1 },
				new ShopEntry { ItemId = "Coal",        Price = 10,  Stock = -1 },
				new ShopEntry { ItemId = "IronIngot",   Price = 40,  Stock = -1 },
				new ShopEntry { ItemId = "IronSword",   Price = 120, Stock = 3 },
				new ShopEntry { ItemId = "IronHelmet",  Price = 80,  Stock = 2 },
				new ShopEntry { ItemId = "IronArmor",   Price = 100, Stock = 2 },
				new ShopEntry { ItemId = "IronBoots",   Price = 70,  Stock = 2 },
				new ShopEntry { ItemId = "Pickaxe",     Price = 50,  Stock = 3 },
			};
		}
		return System.Array.Empty<ShopEntry>();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_open) return;

		if (@event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Tab to switch Buy/Sell
		if (CanSell && @event is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.Tab)
		{
			_sellMode = !_sellMode;
			_selectedIdx = 0;
			if (_sellMode) RefreshSellList();
			AudioManager.Instance?.PlaySfxFlat("ui_click");
			Refresh();
			GetViewport().SetInputAsHandled();
			return;
		}

		int maxIdx = _sellMode ? _sellableItems.Count - 1 : _stock.Count - 1;

		if (@event.IsActionPressed("ui_up"))
		{
			_selectedIdx = Mathf.Max(0, _selectedIdx - 1);
			AudioManager.Instance?.PlaySfxFlat("ui_navigate");
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_down"))
		{
			_selectedIdx = Mathf.Min(maxIdx, _selectedIdx + 1);
			AudioManager.Instance?.PlaySfxFlat("ui_navigate");
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_accept"))
		{
			if (_sellMode) Sell(); else Buy();
			GetViewport().SetInputAsHandled();
		}
	}

	// ── Public API ─────────────────────────────────────────────────────────
	public void Open()
	{
		_stock.Clear();
		if (Stock != null)
			foreach (var e in Stock)
				if (e != null && !string.IsNullOrEmpty(e.ItemId) && e.Stock != 0)
					_stock.Add(e);

		if (_stock.Count == 0)
		{
			NotificationManager.Instance?.ShowWarning("Nothing for sale.");
			return;
		}

		_open = true;
		_sellMode = false;
		_selectedIdx = 0;
		_screen.Visible = true;
		GetTree().Paused = true;
		AudioManager.Instance?.PlaySfxFlat("ui_click");
		Refresh();
	}

	public void Close()
	{
		_open           = false;
		_screen.Visible = false;
		GetTree().Paused = false;
		AudioManager.Instance?.PlaySfxFlat("ui_click");
	}

	// ── Buy logic ──────────────────────────────────────────────────────────
	private void Buy()
	{
		if (_selectedIdx >= _stock.Count) return;
		var entry = _stock[_selectedIdx];
		var data  = ItemDatabase.Instance?.Get(entry.ItemId);
		string name = data?.DisplayName ?? entry.ItemId;

		if (!PlayerData.Instance.SpendGold(entry.Price)) return;

		AudioManager.Instance?.PlaySfx("buy_item");
		Inventory.Instance.AddItem(entry.ItemId, 1);

		if (entry.Stock > 0)
		{
			entry.Stock--;
			if (entry.Stock == 0)
			{
				_stock.RemoveAt(_selectedIdx);
				_selectedIdx = Mathf.Min(_selectedIdx, _stock.Count - 1);
				if (_stock.Count == 0) { Close(); return; }
			}
		}

		NotificationManager.Instance?.Show(
			$"Bought {name}  -{entry.Price}g", new Color(0.45f, 0.88f, 0.55f));
		Refresh();
	}

	// ── Sell logic ─────────────────────────────────────────────────────────
	private void RefreshSellList()
	{
		_sellableItems.Clear();
		foreach (var (id, cnt) in Inventory.Instance.Items)
		{
			if (cnt <= 0) continue;
			var data = ItemDatabase.Instance?.Get(id);
			if (data == null) continue;
			// Don't allow selling equipped items or tools that are currently equipped
			_sellableItems.Add((id, cnt));
		}
	}

	private int GetSellPrice(string itemId)
	{
		// Check if the shop buys this item at a known price
		if (Stock != null)
			foreach (var e in Stock)
				if (e.ItemId == itemId)
					return Mathf.Max(1, (int)(e.Price * SellPriceRatio));

		// Default sell price based on category
		var data = ItemDatabase.Instance?.Get(itemId);
		if (data == null) return 1;
		return data.Category switch
		{
			ItemCategory.Tool or ItemCategory.Weapon => Mathf.Max(1, (int)(15 * SellPriceRatio)),
			ItemCategory.Armor => Mathf.Max(1, (int)(12 * SellPriceRatio)),
			ItemCategory.Food or ItemCategory.Potion => Mathf.Max(1, (int)(8 * SellPriceRatio)),
			_ => Mathf.Max(1, (int)(5 * SellPriceRatio)) // Resources
		};
	}

	private void Sell()
	{
		if (_selectedIdx >= _sellableItems.Count) return;
		var (id, cnt) = _sellableItems[_selectedIdx];
		var data = ItemDatabase.Instance?.Get(id);
		string name = data?.DisplayName ?? id;
		int price = GetSellPrice(id);

		Inventory.Instance.RemoveItem(id, 1);
		PlayerData.Instance.AddGold(price);
		AudioManager.Instance?.PlaySfx("sell_item");
		NotificationManager.Instance?.Show(
			$"Sold {name}  +{price}g", new Color(0.45f, 0.88f, 0.55f));

		RefreshSellList();
		if (_sellableItems.Count == 0) { _sellMode = false; }
		_selectedIdx = Mathf.Min(_selectedIdx, Mathf.Max(0, (_sellMode ? _sellableItems.Count : _stock.Count) - 1));
		Refresh();
	}

	// ── UI refresh ─────────────────────────────────────────────────────────
	private void Refresh()
	{
		_goldLabel.Text = $"{PlayerData.Instance.Gold} g";
		_titleLabel.Text = _sellMode ? $"{ShopTitle}  —  SELL" : ShopTitle;

		foreach (Node c in _list.GetChildren()) c.QueueFree();

		if (_sellMode)
		{
			if (_sellableItems.Count == 0)
			{
				var empty = new Label { Text = "Nothing to sell." };
				empty.AddThemeColorOverride("font_color", new Color(0.5f, 0.35f, 0.2f, 0.7f));
				empty.AddThemeFontSizeOverride("font_size", 12);
				_list.AddChild(empty);
				_descLabel.Text = "";
			}
			else
			{
				for (int i = 0; i < _sellableItems.Count; i++)
				{
					var (id, cnt) = _sellableItems[i];
					var data = ItemDatabase.Instance?.Get(id);
					string name = data?.DisplayName ?? id;
					int price = GetSellPrice(id);
					bool sel = i == _selectedIdx;

					var row = new Label();
					row.Text = sel
						? $"▶  {name} x{cnt}   +{price}g"
						: $"    {name} x{cnt}   +{price}g";
					row.AddThemeColorOverride("font_color", sel
						? new Color(0.45f, 0.88f, 0.55f)
						: new Color(0.32f, 0.18f, 0.06f));
					row.AddThemeFontSizeOverride("font_size", 12);
					_list.AddChild(row);
				}

				if (_selectedIdx < _sellableItems.Count)
				{
					var data = ItemDatabase.Instance?.Get(_sellableItems[_selectedIdx].id);
					_descLabel.Text = data?.Description ?? "";
				}
			}
		}
		else
		{
			for (int i = 0; i < _stock.Count; i++)
			{
				var entry = _stock[i];
				var data = ItemDatabase.Instance?.Get(entry.ItemId);
				string name = data?.DisplayName ?? entry.ItemId;
				string stock = entry.Stock < 0 ? "∞" : entry.Stock.ToString();
				bool sel = i == _selectedIdx;
				bool canAfford = PlayerData.Instance.Gold >= entry.Price;

				var row = new Label();
				row.Text = sel
					? $"▶  {name}   {entry.Price}g   [{stock}]"
					: $"    {name}   {entry.Price}g   [{stock}]";
				Color col = sel
					? (canAfford ? new Color(1f, 0.88f, 0.28f) : new Color(0.85f, 0.32f, 0.28f))
					: (canAfford ? new Color(0.32f, 0.18f, 0.06f) : new Color(0.5f, 0.35f, 0.28f, 0.7f));
				row.AddThemeColorOverride("font_color", col);
				row.AddThemeFontSizeOverride("font_size", 12);
				_list.AddChild(row);
			}

			if (_selectedIdx < _stock.Count)
			{
				var data = ItemDatabase.Instance?.Get(_stock[_selectedIdx].ItemId);
				_descLabel.Text = data?.Description ?? "";
			}
		}

		_hint.Text = CanSell
			? "[↑↓] browse   [Enter] " + (_sellMode ? "sell" : "buy") + "   [Tab] " + (_sellMode ? "buy" : "sell") + "   [Esc] close"
			: "[↑↓] browse   [Enter] buy   [Esc] close";
	}

	// ── Build UI ───────────────────────────────────────────────────────────
	private void BuildUI()
	{
		_screen = new Control();
		_screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screen.ProcessMode = ProcessModeEnum.Always;
		_screen.Visible = false;
		AddChild(_screen);

		// Blur backdrop (matches inventory / storage UI)
		var shopBbc = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport };
		_screen.AddChild(shopBbc);
		var shopDim = new ColorRect();
		shopDim.Color = new Color(0, 0, 0, 0.4f);
		shopDim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		const string ShopBlurMatPath = "res://shaders/blur_material.tres";
		if (FileAccess.FileExists(ShopBlurMatPath))
			shopDim.Material = GD.Load<ShaderMaterial>(ShopBlurMatPath);
		_screen.AddChild(shopDim);

		var atlas = new AtlasTexture { Atlas = FramesTex, Region = new Rect2(4, 49, 40, 46) };
		var style = new StyleBoxTexture
		{
			Texture = atlas,
			TextureMarginLeft = 8, TextureMarginTop = 8,
			TextureMarginRight = 8, TextureMarginBottom = 10,
			ContentMarginLeft = 22, ContentMarginTop = 14,
			ContentMarginRight = 22, ContentMarginBottom = 14,
		};
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", style);
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft   = -260; panel.OffsetRight  = 260;
		panel.OffsetTop    = -210; panel.OffsetBottom = 210;
		_screen.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);
		panel.AddChild(vbox);

		// Title row
		var titleRow = new HBoxContainer();
		vbox.AddChild(titleRow);

		_titleLabel = new Label { Text = ShopTitle };
		_titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.48f, 0.18f));
		_titleLabel.AddThemeFontSizeOverride("font_size", 16);
		titleRow.AddChild(_titleLabel);

		var goldRow = new HBoxContainer();
		goldRow.AddThemeConstantOverride("separation", 4);
		var coinIcon = new TextureRect();
		coinIcon.Texture = GD.Load<Texture2D>("res://assets/cute_fantasy/cute_fantasy/icons/no outline/coin_icon.png");
		coinIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		coinIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		coinIcon.CustomMinimumSize = new Vector2(14, 14);
		coinIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		goldRow.AddChild(coinIcon);
		_goldLabel = new Label { Text = "0 g" };
		_goldLabel.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.28f));
		_goldLabel.AddThemeFontSizeOverride("font_size", 12);
		goldRow.AddChild(_goldLabel);
		titleRow.AddChild(goldRow);

		var sep = new HSeparator();
		sep.Modulate = new Color(0.55f, 0.38f, 0.18f, 0.5f);
		vbox.AddChild(sep);

		_list = new VBoxContainer();
		_list.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_list.AddThemeConstantOverride("separation", 5);
		vbox.AddChild(_list);

		var sep2 = new HSeparator();
		sep2.Modulate = new Color(0.55f, 0.38f, 0.18f, 0.4f);
		vbox.AddChild(sep2);

		_descLabel = new Label();
		_descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_descLabel.AddThemeColorOverride("font_color", new Color(0.42f, 0.28f, 0.12f));
		_descLabel.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(_descLabel);

		_hint = new Label { Text = "[↑↓] browse   [Enter] buy   [Esc] close" };
		_hint.HorizontalAlignment = HorizontalAlignment.Center;
		_hint.AddThemeColorOverride("font_color", new Color(0.42f, 0.28f, 0.1f, 0.7f));
		_hint.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(_hint);
	}
}
