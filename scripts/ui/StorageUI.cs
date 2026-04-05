using Godot;
using System.Collections.Generic;

/// <summary>
/// UI for transferring items between the player's inventory and a storage container (chest).
/// Shows fixed-size grids with empty slot backgrounds, item icons, counts, names on hover,
/// and supports mouse + keyboard navigation with single or bulk transfer.
/// </summary>
public partial class StorageUI : CanvasLayer
{
	public static StorageUI Instance { get; private set; }

	private Control       _root;
	private GridContainer _playerGrid;
	private GridContainer _chestGrid;
	private Label         _chestLabel;
	private Label         _hintLabel;
	private Inventory     _chestInventory;
	private Node2D        _activeSource;

	// Tooltip
	private PanelContainer _tooltip;
	private Label          _tooltipName;
	private Label          _tooltipDesc;

	// Keyboard navigation
	private enum Side { Player, Chest }
	private Side _activeSide = Side.Player;
	private int  _cursorIndex;
	private bool _keyboardActive; // true when last input was keyboard

	// Slot data for each grid (rebuilt on Refresh)
	private readonly List<SlotInfo> _playerSlots = new();
	private readonly List<SlotInfo> _chestSlots  = new();

	private struct SlotInfo
	{
		public string ItemId;
		public int    Count;
		public Control Node;
		public ColorRect Highlight;
	}

	private const float CloseDistance = 100f;
	private const int   GridColumns  = 5;
	private const int   GridRows     = 6;
	private const int   SlotSize     = 64;
	private const int   IconSize     = 48;

	// Colors
	private static readonly Color SlotHover     = new(0.95f, 0.85f, 0.55f, 0.35f);
	private static readonly Color SlotSelected  = new(0.95f, 0.80f, 0.40f, 0.50f);
	private static readonly Color CountColor    = new(1f, 1f, 1f, 1f);
	private static readonly Color TooltipBg     = new(0.12f, 0.08f, 0.04f, 0.92f);
	private static readonly Color TooltipBorder = new(0.55f, 0.38f, 0.18f, 0.9f);

	private const string FramesPath = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png";
	private const string FontPath   = "res://assets/cute_fantasy_ui/cute_fantasy_ui/font.fnt";

	private Font _font;
	private Texture2D _framesTexture;

	public override void _Ready()
	{
		Instance = this;
		Visible  = false;

		_font = GD.Load<Font>(FontPath);
		_framesTexture = GD.Load<Texture2D>(FramesPath);

		_root       = GetNode<Control>("Root");
		_playerGrid = _root.GetNode<GridContainer>("Panels/PlayerPanel/VBox/Margin/PlayerGrid");
		_chestGrid  = _root.GetNode<GridContainer>("Panels/ChestPanel/VBox/Margin/ChestGrid");
		_chestLabel = _root.GetNode<Label>("Panels/ChestPanel/VBox/RibbonBox/ChestLabel");
		_hintLabel  = _root.GetNode<Label>("Hint");

		_playerGrid.Columns = GridColumns;
		_chestGrid.Columns  = GridColumns;

		UpdateHint();
		BuildTooltip();
	}

	public void Open(Inventory chestInv, string chestName = "Storage", Node2D source = null)
	{
		_chestInventory = chestInv;
		_activeSource   = source;
		_chestLabel.Text = chestName;
		_activeSide  = Side.Player;
		_cursorIndex = 0;
		_keyboardActive = false;
		Visible = true;
		GetTree().Paused = true;
		AudioManager.Instance?.PlaySfxFlat("ui_click");
		Refresh();
	}

	public override void _Process(double delta)
	{
		if (!Visible || _activeSource == null) return;

		var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (player == null) return;

		if (player.GlobalPosition.DistanceTo(_activeSource.GlobalPosition) > CloseDistance)
			Close();
	}

	public void Close()
	{
		HideTooltip();
		AudioManager.Instance?.PlaySfxFlat("ui_click");

		if (Visible && _activeSource != null)
			_activeSource.Call("CloseStorage");

		Visible = false;
		GetTree().Paused = false;
		_chestInventory = null;
		_activeSource   = null;
	}

	public void Refresh()
	{
		if (_chestInventory == null) return;

		ClearGrid(_playerGrid);
		ClearGrid(_chestGrid);
		_playerSlots.Clear();
		_chestSlots.Clear();

		PopulateGrid(_playerGrid, Inventory.Instance, true, _playerSlots);
		PopulateGrid(_chestGrid,  _chestInventory, false, _chestSlots);

		// Clamp cursor after refresh
		var activeList = _activeSide == Side.Player ? _playerSlots : _chestSlots;
		_cursorIndex = Mathf.Clamp(_cursorIndex, 0, activeList.Count - 1);

		if (_keyboardActive)
			UpdateCursorVisual();
	}

	private void ClearGrid(GridContainer grid)
	{
		foreach (Node child in grid.GetChildren())
			child.QueueFree();
	}

	private void PopulateGrid(GridContainer grid, Inventory inv, bool isPlayerSide, List<SlotInfo> slotList)
	{
		var itemList = new List<(string id, int count)>();
		foreach (var (id, count) in inv.Items)
		{
			if (count > 0)
				itemList.Add((id, count));
		}

		int totalSlots = GridColumns * GridRows;

		for (int i = 0; i < totalSlots; i++)
		{
			bool hasItem = i < itemList.Count;
			string itemId = hasItem ? itemList[i].id : null;
			int itemCount = hasItem ? itemList[i].count : 0;

			var (slot, highlight) = CreateSlot(itemId, itemCount, inv, isPlayerSide, slotList.Count);
			grid.AddChild(slot);

			slotList.Add(new SlotInfo
			{
				ItemId    = itemId,
				Count     = itemCount,
				Node      = slot,
				Highlight = highlight
			});
		}
	}

	private (Control slot, ColorRect highlight) CreateSlot(string itemId, int count, Inventory sourceInv, bool isPlayerSide, int slotIndex)
	{
		var slot = new Control
		{
			CustomMinimumSize = new Vector2(SlotSize, SlotSize),
			MouseFilter = Control.MouseFilterEnum.Stop
		};

		// Slot background
		var bg = new NinePatchRect
		{
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			PatchMarginLeft = 12,
			PatchMarginTop = 12,
			PatchMarginRight = 12,
			PatchMarginBottom = 12,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		var bgAtlas = new AtlasTexture
		{
			Atlas = _framesTexture,
			Region = new Rect2(48, 0, 32, 32)
		};
		bg.Texture = bgAtlas;
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		slot.AddChild(bg);

		// Highlight overlay (always present for keyboard cursor)
		var highlight = new ColorRect
		{
			Color = Colors.Transparent,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		highlight.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		slot.AddChild(highlight);

		if (itemId == null || count <= 0)
			return (slot, highlight);

		// Item data
		var data = ItemDatabase.Instance?.Get(itemId);

		// Icon
		if (data?.Icon != null)
		{
			var icon = new TextureRect
			{
				Texture = data.Icon,
				CustomMinimumSize = new Vector2(IconSize, IconSize),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			icon.SetAnchorsPreset(Control.LayoutPreset.Center);
			icon.OffsetLeft   = -IconSize / 2f;
			icon.OffsetTop    = -IconSize / 2f;
			icon.OffsetRight  = IconSize / 2f;
			icon.OffsetBottom = IconSize / 2f;
			slot.AddChild(icon);
		}

		// Count label
		if (count > 1)
		{
			var countLabel = new Label
			{
				Text = count.ToString(),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			countLabel.AddThemeColorOverride("font_color", CountColor);
			countLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
			countLabel.AddThemeConstantOverride("shadow_offset_x", 1);
			countLabel.AddThemeConstantOverride("shadow_offset_y", 1);
			countLabel.AddThemeFontSizeOverride("font_size", 11);
			if (_font != null) countLabel.AddThemeFontOverride("font", _font);
			countLabel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
			countLabel.OffsetLeft   = -40;
			countLabel.OffsetTop    = -20;
			countLabel.OffsetRight  = -4;
			countLabel.OffsetBottom = -2;
			slot.AddChild(countLabel);
		}

		// Mouse hover events
		string capturedId = itemId;
		int capturedCount = count;
		Side capturedSide = isPlayerSide ? Side.Player : Side.Chest;
		int capturedIndex = slotIndex;

		slot.MouseEntered += () =>
		{
			_keyboardActive = false;
			_activeSide  = capturedSide;
			_cursorIndex = capturedIndex;
			UpdateCursorVisual();
			ShowTooltip(capturedId, capturedCount, slot);
		};
		slot.MouseExited += () =>
		{
			highlight.Color = Colors.Transparent;
			HideTooltip();
		};
		slot.GuiInput += (ev) =>
		{
			if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				TransferAt(capturedSide, capturedIndex, mb.ShiftPressed);
			}
		};

		return (slot, highlight);
	}

	// ── Transfer ───────────────────────────────────────────────────────────

	private void TransferAt(Side side, int index, bool fullStack)
	{
		var slotList = side == Side.Player ? _playerSlots : _chestSlots;
		if (index < 0 || index >= slotList.Count) return;

		var info = slotList[index];
		if (info.ItemId == null || info.Count <= 0) return;

		int amount = fullStack ? info.Count : 1;
		Inventory source = side == Side.Player ? Inventory.Instance : _chestInventory;
		Inventory target = side == Side.Player ? _chestInventory : Inventory.Instance;

		source.TransferTo(target, info.ItemId, amount);
		HideTooltip();
		Refresh();
	}

	// ── Keyboard cursor ────────────────────────────────────────────────────

	private void ClearAllHighlights()
	{
		foreach (var s in _playerSlots)
			if (s.Highlight != null) s.Highlight.Color = Colors.Transparent;
		foreach (var s in _chestSlots)
			if (s.Highlight != null) s.Highlight.Color = Colors.Transparent;
	}

	private void UpdateCursorVisual()
	{
		ClearAllHighlights();

		var activeList = _activeSide == Side.Player ? _playerSlots : _chestSlots;
		if (_cursorIndex < 0 || _cursorIndex >= activeList.Count) return;

		var info = activeList[_cursorIndex];
		if (info.Highlight != null)
			info.Highlight.Color = _keyboardActive ? SlotSelected : SlotHover;

		// Show tooltip for current slot
		if (info.ItemId != null && info.Count > 0)
			ShowTooltip(info.ItemId, info.Count, info.Node);
		else
			HideTooltip();
	}

	private void MoveCursor(int dx, int dy)
	{
		_keyboardActive = true;

		int col = _cursorIndex % GridColumns;
		int row = _cursorIndex / GridColumns;

		// Horizontal: switch panels at edges
		if (dx != 0)
		{
			int newCol = col + dx;
			if (newCol < 0 && _activeSide == Side.Chest)
			{
				_activeSide = Side.Player;
				newCol = GridColumns - 1;
			}
			else if (newCol >= GridColumns && _activeSide == Side.Player)
			{
				_activeSide = Side.Chest;
				newCol = 0;
			}
			else
			{
				newCol = Mathf.Clamp(newCol, 0, GridColumns - 1);
			}
			col = newCol;
		}

		// Vertical: wrap within panel
		if (dy != 0)
		{
			row = Mathf.Clamp(row + dy, 0, GridRows - 1);
		}

		_cursorIndex = row * GridColumns + col;
		AudioManager.Instance?.PlaySfxFlat("ui_navigate");
		UpdateCursorVisual();
	}

	// ── Tooltip ────────────────────────────────────────────────────────────

	private void BuildTooltip()
	{
		_tooltip = new PanelContainer
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 100
		};

		var style = new StyleBoxFlat
		{
			BgColor = TooltipBg,
			BorderColor = TooltipBorder,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ContentMarginLeft = 10,
			ContentMarginTop = 6,
			ContentMarginRight = 10,
			ContentMarginBottom = 6
		};
		_tooltip.AddThemeStyleboxOverride("panel", style);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 2);
		_tooltip.AddChild(vbox);

		_tooltipName = new Label();
		_tooltipName.AddThemeColorOverride("font_color", new Color(1f, 0.92f, 0.7f));
		_tooltipName.AddThemeFontSizeOverride("font_size", 11);
		if (_font != null) _tooltipName.AddThemeFontOverride("font", _font);
		vbox.AddChild(_tooltipName);

		_tooltipDesc = new Label();
		_tooltipDesc.AddThemeColorOverride("font_color", new Color(0.8f, 0.75f, 0.65f));
		_tooltipDesc.AddThemeFontSizeOverride("font_size", 10);
		if (_font != null) _tooltipDesc.AddThemeFontOverride("font", _font);
		vbox.AddChild(_tooltipDesc);

		_root.AddChild(_tooltip);
	}

	private void ShowTooltip(string itemId, int count, Control slot)
	{
		var data = ItemDatabase.Instance?.Get(itemId);
		string name = data?.DisplayName ?? itemId;
		_tooltipName.Text = count > 1 ? $"{name}  x{count}" : name;

		string desc = data?.Description ?? "";
		if (data != null)
		{
			if (data.WeaponDamage > 0) desc += $"\nDamage: {data.WeaponDamage}";
			if (data.ArmorRating > 0)  desc += $"\nDefence: +{data.ArmorRating}";
			if (data.HealAmount > 0)   desc += $"\nHeals: +{data.HealAmount} HP";
		}
		_tooltipDesc.Text = desc;
		_tooltipDesc.Visible = !string.IsNullOrEmpty(desc);

		var slotRect = slot.GetGlobalRect();
		_tooltip.Visible = true;
		_tooltip.ResetSize();

		float tooltipX = slotRect.Position.X + slotRect.Size.X / 2f - _tooltip.Size.X / 2f;
		float tooltipY = slotRect.Position.Y - _tooltip.Size.Y - 8f;

		var viewport = GetViewport().GetVisibleRect();
		tooltipX = Mathf.Clamp(tooltipX, 8, viewport.Size.X - _tooltip.Size.X - 8);
		if (tooltipY < 8) tooltipY = slotRect.Position.Y + slotRect.Size.Y + 8f;

		_tooltip.GlobalPosition = new Vector2(tooltipX, tooltipY);
	}

	private void HideTooltip()
	{
		if (_tooltip != null)
			_tooltip.Visible = false;
	}

	// ── Hint label ─────────────────────────────────────────────────────────

	private void UpdateHint()
	{
		if (_hintLabel != null)
			_hintLabel.Text = "Arrows/Mouse to navigate  •  [E]/Click to transfer  •  Hold Shift for stack  •  [ESC] to close";
	}

	// ── Input ──────────────────────────────────────────────────────────────

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible) return;

		if (@event.IsActionPressed("toggle_inventory") || @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Arrow key navigation
		if (@event.IsActionPressed("ui_left"))
		{
			MoveCursor(-1, 0);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_right"))
		{
			MoveCursor(1, 0);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_up"))
		{
			MoveCursor(0, -1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_down"))
		{
			MoveCursor(0, 1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("interact"))
		{
			// Transfer item at cursor
			bool shift = Input.IsKeyPressed(Key.Shift);
			TransferAt(_activeSide, _cursorIndex, shift);
			GetViewport().SetInputAsHandled();
		}
	}
}
