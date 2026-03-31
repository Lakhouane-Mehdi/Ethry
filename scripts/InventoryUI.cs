using System.Collections.Generic;
using Godot;

/// <summary>
/// Inventory UI driven by scenes/ui/inventory_ui.tscn.
/// Features: 4 equipment slots, 5×4 item grid, hover tooltip,
/// context-sensitive Use/Equip/Drop buttons, keyboard navigation.
/// </summary>
public partial class InventoryUI : CanvasLayer
{
	// ── Scene node references ──────────────────────────────────────────────
	private PanelContainer _panel;
	private GridContainer  _slotsGrid;
	// Equipment display slots
	private TextureRect    _weaponIcon,  _headIcon,  _bodyIcon,  _bootsIcon;
	// Stats row
	private Label          _dmgLabel,    _defLabel;
	// Detail panel
	private TextureRect    _detailIcon;
	private Label          _detailName,  _detailCategory, _detailDesc;
	private Button         _actionBtn,   _dropBtn;
	// Tooltip
	private PanelContainer _tooltip;
	private Label          _tooltipName, _tooltipDesc;

	// ── State ──────────────────────────────────────────────────────────────
	private bool               _isVisible;
	private int                _selectedIndex = -1;
	private readonly List<ItemType> _itemOrder = new();

	// ── Assets ────────────────────────────────────────────────────────────
	private readonly Dictionary<string, Texture2D> _texCache = new();

	private const string FramesPath    = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png";
	private const string SelectorsPath = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_selectors.png";
	private const string ButtonsPath   = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_buttons.png";
	private const string FontPath      = "res://assets/cute_fantasy_ui/cute_fantasy_ui/font.fnt";

	// Button regions from ui_buttons.png — 47×14 cells at bottom rows
	private static readonly Rect2 GreenBtnNormal  = new(288, 337, 47, 14);
	private static readonly Rect2 GreenBtnHover   = new(336, 337, 47, 14);
	private static readonly Rect2 GreenBtnPressed = new(384, 337, 47, 14);
	private static readonly Rect2 RedBtnNormal    = new(720, 337, 47, 14);
	private static readonly Rect2 RedBtnHover     = new(768, 337, 47, 14);
	private static readonly Rect2 RedBtnPressed   = new(816, 337, 47, 14);

	// Slot frame regions (ui_frames.png, measured in pixels)
	private static readonly Rect2 FrameNormal   = new( 4,  7, 40, 36);
	private static readonly Rect2 FrameSelected = new(52,  7, 40, 36);
	// Green corner-bracket selector (ui_selectors.png)
	private static readonly Rect2 SelectorRegion = new(0, 192, 16, 16);

	private const int SlotPx = 60;
	private const int IconPx = 40;
	private const int Cols   = 5;
	private const int Rows   = 4;

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		_panel     = GetNode<PanelContainer>("Panel");
		_slotsGrid = GetNode<GridContainer>("Panel/VBox/SlotsGrid");

		// Equipment icons
		_weaponIcon = GetNode<TextureRect>("Panel/VBox/EquipGrid/WeaponSlot/WeaponIcon");
		_headIcon   = GetNode<TextureRect>("Panel/VBox/EquipGrid/HeadSlot/HeadIcon");
		_bodyIcon   = GetNode<TextureRect>("Panel/VBox/EquipGrid/BodySlot/BodyIcon");
		_bootsIcon  = GetNode<TextureRect>("Panel/VBox/EquipGrid/BootsSlot/BootsIcon");

		// Stats
		_dmgLabel = GetNode<Label>("Panel/VBox/StatsRow/DmgLabel");
		_defLabel = GetNode<Label>("Panel/VBox/StatsRow/DefLabel");

		// Detail panel
		_detailIcon     = GetNode<TextureRect>("Panel/VBox/DetailPanel/DetailVBox/DetailTopRow/DetailIcon");
		_detailName     = GetNode<Label>("Panel/VBox/DetailPanel/DetailVBox/DetailTopRow/DetailTextVBox/DetailName");
		_detailCategory = GetNode<Label>("Panel/VBox/DetailPanel/DetailVBox/DetailTopRow/DetailTextVBox/DetailCategory");
		_detailDesc     = GetNode<Label>("Panel/VBox/DetailPanel/DetailVBox/DetailTopRow/DetailTextVBox/DetailDesc");
		_actionBtn      = GetNode<Button>("Panel/VBox/DetailPanel/DetailVBox/ActionRow/ActionBtn");
		_dropBtn        = GetNode<Button>("Panel/VBox/DetailPanel/DetailVBox/ActionRow/DropBtn");

		// Tooltip (child of InventoryUI CanvasLayer, not of Panel)
		_tooltip     = GetNode<PanelContainer>("Tooltip");
		_tooltipName = GetNode<Label>("Tooltip/TooltipVBox/TooltipName");
		_tooltipDesc = GetNode<Label>("Tooltip/TooltipVBox/TooltipDesc");

		// Bitmap pixel font
		var font = new FontFile();
		font.LoadBitmapFont(FontPath);
		var theme = new Theme();
		theme.DefaultFont = font;
		_panel.Theme = theme;

		// Textured button styles
		ApplyButtonStyle(_actionBtn, GreenBtnNormal, GreenBtnHover, GreenBtnPressed);
		ApplyButtonStyle(_dropBtn,   RedBtnNormal,   RedBtnHover,   RedBtnPressed);

		// Action button handlers
		_actionBtn.Pressed += OnActionPressed;
		_dropBtn.Pressed   += OnDropPressed;

		// Equipment slot unequip buttons
		GetNode<Button>("Panel/VBox/EquipGrid/WeaponSlot/WeaponBtn").Pressed += () => Unequip(EquipSlot.Weapon);
		GetNode<Button>("Panel/VBox/EquipGrid/HeadSlot/HeadBtn").Pressed     += () => Unequip(EquipSlot.Head);
		GetNode<Button>("Panel/VBox/EquipGrid/BodySlot/BodyBtn").Pressed     += () => Unequip(EquipSlot.Body);
		GetNode<Button>("Panel/VBox/EquipGrid/BootsSlot/BootsBtn").Pressed   += () => Unequip(EquipSlot.Boots);

		// Build slot grid nodes (structure fixed, content populated in Refresh)
		for (int i = 0; i < Cols * Rows; i++)
			_slotsGrid.AddChild(BuildSlot(i));

		Inventory.Instance.Changed += Refresh;
		Equipment.Instance.Changed += Refresh;

		ClearDetail();
	}

	// ── Input ──────────────────────────────────────────────────────────────
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_inventory"))
		{
			_isVisible     = !_isVisible;
			_panel.Visible = _isVisible;
			_tooltip.Visible = false;
			if (_isVisible) { _selectedIndex = -1; Refresh(); }
			GetViewport().SetInputAsHandled();
			return;
		}

		if (!_isVisible) return;

		if      (@event.IsActionPressed("ui_right")) Move( 1);
		else if (@event.IsActionPressed("ui_left"))  Move(-1);
		else if (@event.IsActionPressed("ui_down"))  Move( Cols);
		else if (@event.IsActionPressed("ui_up"))    Move(-Cols);
		else if (@event.IsActionPressed("ui_accept")) OnActionPressed();
	}

	// ── Slot construction ──────────────────────────────────────────────────
	private Control BuildSlot(int index)
	{
		var slot = new PanelContainer();
		slot.CustomMinimumSize = new Vector2(SlotPx, SlotPx);
		slot.AddThemeStyleboxOverride("panel", MakeSlotStyle(false));

		// Item icon
		var icon = new TextureRect { Name = "Icon" };
		icon.SetAnchorsPreset(Control.LayoutPreset.Center);
		icon.OffsetLeft   = -IconPx / 2f;  icon.OffsetTop    = -IconPx / 2f;
		icon.OffsetRight  =  IconPx / 2f;  icon.OffsetBottom =  IconPx / 2f;
		icon.ExpandMode   = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode  = TextureRect.StretchModeEnum.KeepAspectCentered;
		icon.TextureFilter= CanvasItem.TextureFilterEnum.Nearest;
		icon.MouseFilter  = Control.MouseFilterEnum.Ignore;
		icon.Visible = false;
		slot.AddChild(icon);

		// Stack count label
		var count = new Label { Name = "Count" };
		count.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
		count.OffsetLeft = -22; count.OffsetTop = -14;
		count.HorizontalAlignment = HorizontalAlignment.Right;
		count.AddThemeColorOverride("font_color",        new Color(0.9f, 0.85f, 0.6f));
		count.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.85f));
		count.AddThemeFontSizeOverride("font_size", 10);
		count.AddThemeConstantOverride("shadow_offset_x", 1);
		count.AddThemeConstantOverride("shadow_offset_y", 1);
		count.MouseFilter = Control.MouseFilterEnum.Ignore;
		count.Visible = false;
		slot.AddChild(count);

		// Green corner-bracket selection overlay
		var sel = new TextureRect { Name = "Selector" };
		sel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		sel.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		sel.StretchMode   = TextureRect.StretchModeEnum.Scale;
		sel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		sel.MouseFilter   = Control.MouseFilterEnum.Ignore;
		sel.Visible = false;
		var selAtlas = new AtlasTexture();
		selAtlas.Atlas  = GetTex(SelectorsPath);
		selAtlas.Region = SelectorRegion;
		sel.Texture = selAtlas;
		slot.AddChild(sel);

		// Invisible button on top to handle click + hover
		var btn = new Button { Flat = true };
		btn.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		btn.MouseFilter = Control.MouseFilterEnum.Stop;
		int cap = index;
		btn.Pressed               += () => { _selectedIndex = cap; UpdateSlotVisuals(); };
		btn.MouseEntered          += () => ShowTooltip(cap);
		btn.MouseExited           += () => _tooltip.Visible = false;
		slot.AddChild(btn);

		return slot;
	}

	// ── Refresh ────────────────────────────────────────────────────────────
	private void Refresh()
	{
		// Rebuild ordered item list
		_itemOrder.Clear();
		foreach (var (type, cnt) in Inventory.Instance.Items)
			if (cnt > 0) _itemOrder.Add(type);

		// Update grid slots
		for (int i = 0; i < Cols * Rows; i++)
		{
			var slot     = _slotsGrid.GetChild<PanelContainer>(i);
			var icon     = slot.GetNode<TextureRect>("Icon");
			var countLbl = slot.GetNode<Label>("Count");

			if (i < _itemOrder.Count)
			{
				var type  = _itemOrder[i];
				int stack = Inventory.Instance.GetCount(type);
				icon.Texture     = GetIconAtlas(type);
				icon.Visible     = true;
				countLbl.Text    = stack > 1 ? stack.ToString() : "";
				countLbl.Visible = stack > 1;
			}
			else
			{
				icon.Texture     = null;
				icon.Visible     = false;
				countLbl.Visible = false;
			}
		}

		// Clamp selection
		if (_selectedIndex >= _itemOrder.Count)
			_selectedIndex = _itemOrder.Count - 1;

		RefreshEquipSlots();
		UpdateSlotVisuals();
	}

	private void RefreshEquipSlots()
	{
		RefreshOneEquipSlot(_weaponIcon, EquipSlot.Weapon);
		RefreshOneEquipSlot(_headIcon,   EquipSlot.Head);
		RefreshOneEquipSlot(_bodyIcon,   EquipSlot.Body);
		RefreshOneEquipSlot(_bootsIcon,  EquipSlot.Boots);

		_dmgLabel.Text = $"DMG: {Equipment.Instance.GetAttackDamage()}";
		_defLabel.Text = $"DEF: {Equipment.Instance.GetTotalDefence()}";
	}

	private static readonly Color PlaceholderTint = new(1f, 1f, 1f, 0.2f);

	private static ItemType GetPlaceholderType(EquipSlot slot) => slot switch
	{
		EquipSlot.Weapon => ItemType.IronSword,
		EquipSlot.Head   => ItemType.LeatherHelmet,
		EquipSlot.Body   => ItemType.LeatherArmor,
		EquipSlot.Boots  => ItemType.LeatherBoots,
		_                => ItemType.IronSword
	};

	private void RefreshOneEquipSlot(TextureRect icon, EquipSlot slot)
	{
		var item = Equipment.Instance.GetSlot(slot);
		if (item.HasValue)
		{
			icon.Texture  = GetIconAtlas(item.Value);
			icon.Modulate = Colors.White;
			icon.Visible  = true;
		}
		else
		{
			icon.Texture  = GetIconAtlas(GetPlaceholderType(slot));
			icon.Modulate = PlaceholderTint;
			icon.Visible  = true;
		}
	}

	private void UpdateSlotVisuals()
	{
		for (int i = 0; i < Cols * Rows; i++)
		{
			bool selected = (i == _selectedIndex && i < _itemOrder.Count);
			var  slot     = _slotsGrid.GetChild<PanelContainer>(i);
			slot.AddThemeStyleboxOverride("panel", MakeSlotStyle(selected));
			slot.GetNode<TextureRect>("Selector").Visible = selected;
		}

		if (_selectedIndex >= 0 && _selectedIndex < _itemOrder.Count)
			ShowDetail(_itemOrder[_selectedIndex]);
		else
			ClearDetail();
	}

	// ── Tooltip ────────────────────────────────────────────────────────────
	private void ShowTooltip(int index)
	{
		if (index >= _itemOrder.Count) { _tooltip.Visible = false; return; }

		var   type = _itemOrder[index];
		_tooltipName.Text = ItemRegistry.GetName(type);
		_tooltipDesc.Text = ItemRegistry.GetDescription(type);
		_tooltip.Visible  = true;

		// Position near but not under the cursor, clamped to screen
		var mouse    = _tooltip.GetViewport().GetMousePosition();
		var viewSize = _tooltip.GetViewportRect().Size;
		var tipSize  = _tooltip.Size;
		float x = Mathf.Clamp(mouse.X + 12f, 0, viewSize.X - tipSize.X);
		float y = Mathf.Clamp(mouse.Y + 12f, 0, viewSize.Y - tipSize.Y);
		_tooltip.Position = new Vector2(x, y);
	}

	// ── Detail panel ───────────────────────────────────────────────────────
	private void ShowDetail(ItemType type)
	{
		int stack = Inventory.Instance.GetCount(type);
		int dmg   = ItemRegistry.GetWeaponDamage(type);
		int arm   = ItemRegistry.GetArmorRating(type);
		int heal  = ItemRegistry.GetHealAmount(type);
		var cat   = ItemRegistry.GetCategory(type);

		_detailIcon.Texture  = GetIconAtlas(type);
		_detailName.Text     = $"{ItemRegistry.GetName(type)}  ×{stack}";
		_detailCategory.Text = cat.ToString().ToUpper();

		string desc = ItemRegistry.GetDescription(type);
		if (dmg  > 0) desc += $"\nDamage: {dmg}";
		if (arm  > 0) desc += $"\nDefence: +{arm}";
		_detailDesc.Text = desc;

		// Action button — label changes based on category, hides if irrelevant
		EquipSlot? slot = ItemRegistry.GetEquipSlot(type);
		if (slot.HasValue)
		{
			_actionBtn.Text    = cat == ItemCategory.Armor ? "Equip Armor" : "Equip";
			_actionBtn.Visible = true;
		}
		else if (heal > 0)
		{
			var player = GetTree().GetFirstNodeInGroup("player") as Player;
			_actionBtn.Text    = "Use";
			_actionBtn.Visible = player != null && player.Health < player.MaxHealth;
		}
		else
		{
			_actionBtn.Visible = false;
		}

		_dropBtn.Visible = true;
	}

	private void ClearDetail()
	{
		_detailIcon.Texture  = null;
		_detailName.Text     = "";
		_detailCategory.Text = "";
		_detailDesc.Text     = "Select an item to see details.";
		_actionBtn.Visible   = false;
		_dropBtn.Visible     = false;
	}

	// ── Actions ────────────────────────────────────────────────────────────
	private void OnActionPressed()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _itemOrder.Count) return;
		var type = _itemOrder[_selectedIndex];
		var cat  = ItemRegistry.GetCategory(type);

		if (cat is ItemCategory.Weapon or ItemCategory.Armor or ItemCategory.Tool)
		{
			Equipment.Instance.Equip(type);
		}
		else if (cat is ItemCategory.Food or ItemCategory.Potion)
		{
			var player = GetTree().GetFirstNodeInGroup("player") as Player;
			if (player != null && player.UseConsumable(type))
				NotificationManager.Instance?.ShowHeal(ItemRegistry.GetHealAmount(type));
		}

		Refresh();
	}

	private void OnDropPressed()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _itemOrder.Count) return;
		var type = _itemOrder[_selectedIndex];

		if (!Inventory.Instance.RemoveItem(type)) return;

		// Spawn pickup near player in the current scene
		var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (player != null)
		{
			var scene  = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");
			var pickup = scene?.Instantiate<Node2D>();
			if (pickup != null)
			{
				pickup.Set("Type",   (int)type);
				pickup.Set("Amount", 1);
				pickup.GlobalPosition = player.GlobalPosition + new Vector2(12f, 0f);
				player.GetParent().AddChild(pickup);
			}
		}

		Refresh();
	}

	private void Unequip(EquipSlot slot)
	{
		Equipment.Instance.Unequip(slot);
		Refresh();
	}

	private void Move(int delta)
	{
		if (_itemOrder.Count == 0) return;
		_selectedIndex = _selectedIndex < 0
			? 0
			: Mathf.Clamp(_selectedIndex + delta, 0, _itemOrder.Count - 1);
		UpdateSlotVisuals();
	}

	// ── Helpers ────────────────────────────────────────────────────────────
	private StyleBoxTexture MakeSlotStyle(bool selected)
	{
		var atlas   = new AtlasTexture();
		atlas.Atlas  = GetTex(FramesPath);
		atlas.Region = selected ? FrameSelected : FrameNormal;

		return new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 6f, TextureMarginTop    = 5f,
			TextureMarginRight  = 6f, TextureMarginBottom = 5f,
			ContentMarginLeft   = 3f, ContentMarginTop    = 3f,
			ContentMarginRight  = 3f, ContentMarginBottom = 3f,
		};
	}

	private Texture2D GetTex(string path)
	{
		if (!_texCache.TryGetValue(path, out var tex))
			_texCache[path] = tex = GD.Load<Texture2D>(path);
		return tex;
	}

	private AtlasTexture GetIconAtlas(ItemType type)
	{
		var atlas   = new AtlasTexture();
		atlas.Atlas  = GetTex(ItemRegistry.GetIconTexturePath(type));
		atlas.Region = ItemRegistry.GetIconRegion(type);
		return atlas;
	}

	private StyleBoxTexture MakeBtnStyle(Rect2 region)
	{
		var atlas   = new AtlasTexture();
		atlas.Atlas  = GetTex(ButtonsPath);
		atlas.Region = region;
		return new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 4f, TextureMarginTop    = 4f,
			TextureMarginRight  = 4f, TextureMarginBottom = 4f,
			ContentMarginLeft   = 6f, ContentMarginTop    = 4f,
			ContentMarginRight  = 6f, ContentMarginBottom = 4f,
		};
	}

	private void ApplyButtonStyle(Button btn, Rect2 normal, Rect2 hover, Rect2 pressed)
	{
		btn.AddThemeStyleboxOverride("normal",  MakeBtnStyle(normal));
		btn.AddThemeStyleboxOverride("hover",   MakeBtnStyle(hover));
		btn.AddThemeStyleboxOverride("pressed", MakeBtnStyle(pressed));
	}
}
