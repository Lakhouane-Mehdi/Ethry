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

	// ── State ──────────────────────────────────────────────────────────────
	private bool _open;
	private int  _selectedIdx;
	private readonly List<ShopEntry> _stock = new();

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
		BuildUI();
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
		if (@event.IsActionPressed("ui_up"))
		{
			_selectedIdx = Mathf.Max(0, _selectedIdx - 1);
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_down"))
		{
			_selectedIdx = Mathf.Min(_stock.Count - 1, _selectedIdx + 1);
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_accept"))
		{
			Buy();
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
		_selectedIdx = 0;
		_screen.Visible = true;
		GetTree().Paused = true;
		Refresh();
	}

	public void Close()
	{
		_open           = false;
		_screen.Visible = false;
		GetTree().Paused = false;
	}

	// ── Buy logic ──────────────────────────────────────────────────────────
	private void Buy()
	{
		if (_selectedIdx >= _stock.Count) return;
		var entry = _stock[_selectedIdx];
		var data  = ItemDatabase.Instance?.Get(entry.ItemId);
		string name = data?.DisplayName ?? entry.ItemId;

		if (!PlayerData.Instance.SpendGold(entry.Price)) return;

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

	// ── UI refresh ─────────────────────────────────────────────────────────
	private void Refresh()
	{
		_goldLabel.Text = $"Gold: {PlayerData.Instance.Gold} g";

		foreach (Node c in _list.GetChildren()) c.QueueFree();

		for (int i = 0; i < _stock.Count; i++)
		{
			var    entry = _stock[i];
			var    data  = ItemDatabase.Instance?.Get(entry.ItemId);
			string name  = data?.DisplayName ?? entry.ItemId;
			string stock = entry.Stock < 0 ? "∞" : entry.Stock.ToString();
			bool   sel   = i == _selectedIdx;
			bool   canAfford = PlayerData.Instance.Gold >= entry.Price;

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

		// Description of selected item
		if (_selectedIdx < _stock.Count)
		{
			var data = ItemDatabase.Instance?.Get(_stock[_selectedIdx].ItemId);
			_descLabel.Text = data?.Description ?? "";
		}
	}

	// ── Build UI ───────────────────────────────────────────────────────────
	private void BuildUI()
	{
		_screen = new Control();
		_screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_screen.ProcessMode = ProcessModeEnum.Always;
		_screen.Visible = false;
		AddChild(_screen);

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

		_goldLabel = new Label { Text = "Gold: 0 g" };
		_goldLabel.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.28f));
		_goldLabel.AddThemeFontSizeOverride("font_size", 12);
		titleRow.AddChild(_goldLabel);

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
