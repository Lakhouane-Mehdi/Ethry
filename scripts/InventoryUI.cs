using System.Collections.Generic;
using Godot;

public partial class InventoryUI : CanvasLayer
{
	private PanelContainer _panel;
	private GridContainer _grid;
	private TextureRect _detailIcon;
	private Label _detailName;
	private Label _detailDesc;
	private Label _detailCategory;
	private PanelContainer _detailPanel;
	private bool _isVisible;
	private int _selectedIndex = -1;
	private readonly List<ItemType> _itemOrder = new();

	private readonly Dictionary<string, Texture2D> _textureCache = new();

	private const int SlotSize = 48;
	private const int IconSize = 32;
	private const int Columns = 5;
	private const int Rows = 4;

	private static readonly Color BgColor = new(0.12f, 0.08f, 0.06f, 0.92f);
	private static readonly Color SlotColor = new(0.25f, 0.18f, 0.12f, 0.8f);
	private static readonly Color SlotSelectedColor = new(0.6f, 0.45f, 0.2f, 0.9f);
	private static readonly Color SlotBorder = new(0.45f, 0.32f, 0.18f, 1f);
	private static readonly Color SlotSelectedBorder = new(0.9f, 0.7f, 0.3f, 1f);
	private static readonly Color TitleColor = new(0.95f, 0.85f, 0.6f);
	private static readonly Color TextColor = new(0.9f, 0.82f, 0.65f);
	private static readonly Color TextDim = new(0.65f, 0.55f, 0.4f);

	public override void _Ready()
	{
		BuildUI();
		Inventory.Instance.Changed += Refresh;
	}

	private StyleBoxFlat MakeStyleBox(Color bg, Color border, int borderWidth = 2, int cornerRadius = 4, int padding = 0)
	{
		var style = new StyleBoxFlat();
		style.BgColor = bg;
		style.BorderColor = border;
		style.BorderWidthBottom = borderWidth;
		style.BorderWidthTop = borderWidth;
		style.BorderWidthLeft = borderWidth;
		style.BorderWidthRight = borderWidth;
		style.CornerRadiusTopLeft = cornerRadius;
		style.CornerRadiusTopRight = cornerRadius;
		style.CornerRadiusBottomLeft = cornerRadius;
		style.CornerRadiusBottomRight = cornerRadius;
		if (padding > 0)
		{
			style.ContentMarginLeft = padding;
			style.ContentMarginRight = padding;
			style.ContentMarginTop = padding;
			style.ContentMarginBottom = padding;
		}
		return style;
	}

	private void BuildUI()
	{
		// Main panel
		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", MakeStyleBox(BgColor, SlotBorder, 3, 8, 12));
		_panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_panel.OffsetLeft = -290;
		_panel.OffsetTop = 10;
		_panel.OffsetRight = -10;
		_panel.OffsetBottom = 420;
		_panel.Visible = false;
		AddChild(_panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		_panel.AddChild(vbox);

		// Title
		var title = new Label();
		title.Text = "INVENTORY";
		title.AddThemeColorOverride("font_color", TitleColor);
		title.AddThemeFontSizeOverride("font_size", 16);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		// Separator
		var sep = new HSeparator();
		sep.AddThemeStyleboxOverride("separator", MakeStyleBox(SlotBorder, SlotBorder, 0, 0));
		sep.AddThemeConstantOverride("separation", 4);
		vbox.AddChild(sep);

		// Grid for item slots
		_grid = new GridContainer();
		_grid.Columns = Columns;
		_grid.AddThemeConstantOverride("h_separation", 4);
		_grid.AddThemeConstantOverride("v_separation", 4);
		_grid.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		vbox.AddChild(_grid);

		// Create empty slots
		for (int i = 0; i < Columns * Rows; i++)
			CreateEmptySlot(i);

		// Separator before detail
		var sep2 = new HSeparator();
		sep2.AddThemeStyleboxOverride("separator", MakeStyleBox(SlotBorder, SlotBorder, 0, 0));
		sep2.AddThemeConstantOverride("separation", 4);
		vbox.AddChild(sep2);

		// Detail panel
		_detailPanel = new PanelContainer();
		_detailPanel.AddThemeStyleboxOverride("panel", MakeStyleBox(new Color(0.18f, 0.12f, 0.08f, 0.7f), SlotBorder, 1, 4, 8));
		_detailPanel.CustomMinimumSize = new Vector2(0, 90);
		vbox.AddChild(_detailPanel);

		var detailHBox = new HBoxContainer();
		detailHBox.AddThemeConstantOverride("separation", 10);
		_detailPanel.AddChild(detailHBox);

		// Detail icon
		_detailIcon = new TextureRect();
		_detailIcon.CustomMinimumSize = new Vector2(40, 40);
		_detailIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_detailIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_detailIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		detailHBox.AddChild(_detailIcon);

		var detailVBox = new VBoxContainer();
		detailVBox.AddThemeConstantOverride("separation", 2);
		detailVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		detailHBox.AddChild(detailVBox);

		// Detail name
		_detailName = new Label();
		_detailName.AddThemeColorOverride("font_color", TitleColor);
		_detailName.AddThemeFontSizeOverride("font_size", 13);
		detailVBox.AddChild(_detailName);

		// Detail category
		_detailCategory = new Label();
		_detailCategory.AddThemeColorOverride("font_color", TextDim);
		_detailCategory.AddThemeFontSizeOverride("font_size", 10);
		detailVBox.AddChild(_detailCategory);

		// Detail description
		_detailDesc = new Label();
		_detailDesc.AddThemeColorOverride("font_color", TextColor);
		_detailDesc.AddThemeFontSizeOverride("font_size", 11);
		_detailDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		detailVBox.AddChild(_detailDesc);

		ClearDetail();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_inventory"))
		{
			_isVisible = !_isVisible;
			_panel.Visible = _isVisible;
			if (_isVisible)
			{
				_selectedIndex = -1;
				Refresh();
			}
		}

		if (!_isVisible) return;

		if (@event.IsActionPressed("ui_right"))
			MoveSelection(1);
		else if (@event.IsActionPressed("ui_left"))
			MoveSelection(-1);
		else if (@event.IsActionPressed("ui_down"))
			MoveSelection(Columns);
		else if (@event.IsActionPressed("ui_up"))
			MoveSelection(-Columns);
	}

	private void MoveSelection(int offset)
	{
		if (_itemOrder.Count == 0) return;

		if (_selectedIndex < 0)
			_selectedIndex = 0;
		else
			_selectedIndex = Mathf.Clamp(_selectedIndex + offset, 0, _itemOrder.Count - 1);

		UpdateSlotVisuals();
	}

	private void Refresh()
	{
		_itemOrder.Clear();

		foreach (var (type, count) in Inventory.Instance.Items)
		{
			if (count <= 0) continue;
			_itemOrder.Add(type);
		}

		// Update all slots
		for (int i = 0; i < Columns * Rows; i++)
		{
			var slot = _grid.GetChild(i) as PanelContainer;
			var icon = slot.GetNode<TextureRect>("Icon");
			var countLabel = slot.GetNode<Label>("Count");

			if (i < _itemOrder.Count)
			{
				var type = _itemOrder[i];
				var count = Inventory.Instance.GetCount(type);
				icon.Texture = GetIconAtlas(type);
				icon.Visible = true;
				countLabel.Text = count > 1 ? count.ToString() : "";
				countLabel.Visible = count > 1;
			}
			else
			{
				icon.Texture = null;
				icon.Visible = false;
				countLabel.Text = "";
				countLabel.Visible = false;
			}
		}

		if (_selectedIndex >= _itemOrder.Count)
			_selectedIndex = _itemOrder.Count - 1;

		UpdateSlotVisuals();
	}

	private void CreateEmptySlot(int index)
	{
		var slot = new PanelContainer();
		slot.AddThemeStyleboxOverride("panel", MakeStyleBox(SlotColor, SlotBorder, 2, 3, 2));
		slot.CustomMinimumSize = new Vector2(SlotSize, SlotSize);

		// Icon
		var icon = new TextureRect();
		icon.Name = "Icon";
		icon.SetAnchorsPreset(Control.LayoutPreset.Center);
		icon.OffsetLeft = -IconSize / 2;
		icon.OffsetTop = -IconSize / 2;
		icon.OffsetRight = IconSize / 2;
		icon.OffsetBottom = IconSize / 2;
		icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		icon.MouseFilter = Control.MouseFilterEnum.Ignore;
		icon.Visible = false;
		slot.AddChild(icon);

		// Count label
		var countLabel = new Label();
		countLabel.Name = "Count";
		countLabel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
		countLabel.OffsetLeft = -24;
		countLabel.OffsetTop = -16;
		countLabel.HorizontalAlignment = HorizontalAlignment.Right;
		countLabel.AddThemeColorOverride("font_color", Colors.White);
		countLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		countLabel.AddThemeFontSizeOverride("font_size", 11);
		countLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		countLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		countLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		countLabel.Visible = false;
		slot.AddChild(countLabel);

		// Invisible click button
		var button = new Button();
		button.Flat = true;
		button.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		button.MouseFilter = Control.MouseFilterEnum.Stop;
		int capturedIndex = index;
		button.Pressed += () =>
		{
			_selectedIndex = capturedIndex;
			UpdateSlotVisuals();
		};
		slot.AddChild(button);

		_grid.AddChild(slot);
	}

	private void UpdateSlotVisuals()
	{
		for (int i = 0; i < Columns * Rows; i++)
		{
			var slot = _grid.GetChild(i) as PanelContainer;
			bool selected = i == _selectedIndex && i < _itemOrder.Count;
			slot.AddThemeStyleboxOverride("panel", MakeStyleBox(
				selected ? SlotSelectedColor : SlotColor,
				selected ? SlotSelectedBorder : SlotBorder,
				selected ? 3 : 2, 3, 2));
		}

		if (_selectedIndex >= 0 && _selectedIndex < _itemOrder.Count)
		{
			var type = _itemOrder[_selectedIndex];
			var count = Inventory.Instance.GetCount(type);
			_detailIcon.Texture = GetIconAtlas(type);
			_detailName.Text = $"{ItemRegistry.GetName(type)}  x{count}";
			_detailCategory.Text = ItemRegistry.GetCategory(type).ToString().ToUpper();
			_detailDesc.Text = ItemRegistry.GetDescription(type);
		}
		else
		{
			ClearDetail();
		}
	}

	private void ClearDetail()
	{
		_detailIcon.Texture = null;
		_detailName.Text = "";
		_detailCategory.Text = "";
		_detailDesc.Text = "Select an item to see details.";
	}

	private Texture2D GetCachedTexture(string path)
	{
		if (!_textureCache.TryGetValue(path, out var tex))
		{
			tex = GD.Load<Texture2D>(path);
			_textureCache[path] = tex;
		}
		return tex;
	}

	private AtlasTexture GetIconAtlas(ItemType type)
	{
		var atlas = new AtlasTexture();
		atlas.Atlas = GetCachedTexture(ItemRegistry.GetIconTexturePath(type));
		atlas.Region = ItemRegistry.GetIconRegion(type);
		return atlas;
	}
}
