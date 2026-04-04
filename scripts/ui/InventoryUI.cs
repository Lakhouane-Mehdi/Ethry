using System.Collections.Generic;
using Godot;

/// <summary>
/// Inventory UI using ui_premade.png as the background texture.
/// Slots are overlaid at exact pixel positions matching the premade layout.
/// Left panel: 4 equipment slots + stat bars.
/// Right panel: 5×6 item grid (30 slots).
///
/// Editor-configurable via [Export] fields.
/// Pauses the game tree while open (standard RPG behavior).
/// Arrow keys navigate both the grid and equipment panel.
/// </summary>
public partial class InventoryUI : CanvasLayer
{
	// ── Exports (editable in editor) ──────────────────────────────────────
	[ExportGroup("Textures")]
	[Export] public Texture2D PremadeTexture;
	[Export] public Texture2D SelectorTexture;
	[Export] public Texture2D FrameTexture;

	[ExportGroup("Layout")]
	[Export(PropertyHint.Range, "1,6")] public int UIScale = 4;

	// ── State ──────────────────────────────────────────────────────────────
	private bool  _isVisible;
	private int   _selectedIndex = -1;
	private readonly List<ItemData> _itemOrder = new();
	private readonly Dictionary<string, Texture2D> _texCache = new();

	// Navigation: which panel is active
	private enum Panel { Grid, Equipment }
	private Panel _activePanel = Panel.Grid;
	private int   _equipSelectedIndex; // 0=Head 1=Body 2=Boots 3=Weapon

	// ── Scene nodes ────────────────────────────────────────────────────────
	private Control       _root;
	private Control       _overlay;
	private TextureRect[] _gridIcons;
	private Label[]       _gridCounts;
	private TextureRect[] _gridSelectors;
	private Button[]      _gridButtons;

	// Equipment
	private TextureRect   _weaponIcon, _headIcon, _bodyIcon, _bootsIcon;
	private Button        _weaponBtn,  _headBtn,  _bodyBtn,  _bootsBtn;
	private TextureRect[] _equipSelectors;

	// Detail panel (shown when item selected)
	private Control        _detailPanel;
	private TextureRect    _detailIcon;
	private Label          _detailName, _detailCategory, _detailDesc, _detailStats;
	private Button         _actionBtn, _dropBtn;

	// Hover tooltip
	private PanelContainer _tooltip;
	private Label          _tooltipName, _tooltipDesc;
	private Label          _dmgLabel, _defLabel;

	// ── Constants ──────────────────────────────────────────────────────────
	private const string DefaultPremadePath   = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_premade.png";
	private const string DefaultSelectorsPath = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_selectors.png";
	private const string DefaultFramesPath    = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png";
	private const string DefaultFontPath      = "res://assets/cute_fantasy_ui/cute_fantasy_ui/font.fnt";
	private const string BlurMatPath          = "res://shaders/blur_material.tres";

	private const int Cols = 5;
	private const int Rows = 6;

	private int SlotW  => 16 * UIScale;
	private int SlotH  => 15 * UIScale;
	private int IconSz => 14 * UIScale;

	// Premade region: left variant at (36,28) size 192×154
	private static readonly Rect2 PremadeRegion = new(36, 28, 192, 154);

	[ExportGroup("Grid Layout")]
	/// <summary>Origin point (top-leftmost slot) in 1x pixel coordinates on the texture.</summary>
	[Export] public Vector2I GridOrigin = new(78, 7);
	/// <summary>Pixels between slots (center-to-center) in 1x scale.</summary>
	[Export] public Vector2I GridSeparation = new(23, 23);

	[ExportGroup("Equipment Layout")]
	[Export] public Vector2I HeadPos   = new(28, 7);
	[Export] public Vector2I BodyPos   = new(28, 29);
	[Export] public Vector2I BootsPos  = new(28, 51);
	[Export] public Vector2I WeaponPos = new(28, 73);

	// Equipment slot order for navigation (matches _equipSelectedIndex)
	private static readonly EquipSlot[] EquipOrder = { EquipSlot.Head, EquipSlot.Body, EquipSlot.Boots, EquipSlot.Weapon };

	// Green selector region from ui_selectors.png
	private static readonly Rect2 SelectorRegion = new(10, 200, 28, 30);

	[ExportGroup("Stats Labels")]
	[Export] public Vector2I DmgLabelPos = new(18, 86);
	[Export] public Vector2I DefLabelPos = new(18, 100);

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		// Keeps processing while tree is paused
		ProcessMode = ProcessModeEnum.Always;

		// ── Blur Backdrop ──────────────────────────────────────────────────
		_overlay = new Control();
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_overlay.Visible = false;
		AddChild(_overlay);

		var bbc = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport };
		_overlay.AddChild(bbc);

		var blur = new ColorRect();
		blur.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		blur.Color = new Color(0, 0, 0, 0.4f);
		if (FileAccess.FileExists(BlurMatPath))
			blur.Material = GD.Load<ShaderMaterial>(BlurMatPath);
		_overlay.AddChild(blur);

		// Root control anchored to center
		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.Center);
		_root.Size = new Vector2(PremadeRegion.Size.X * UIScale, PremadeRegion.Size.Y * UIScale);
		_root.Position = new Vector2(-_root.Size.X / 2f, -_root.Size.Y / 2f);
		_root.PivotOffset = _root.Size / 2;
		_root.Visible = false;
		AddChild(_root);

		// Background: premade texture
		var bg = new TextureRect();
		var bgAtlas = new AtlasTexture();
		bgAtlas.Atlas  = GetPremadeTex();
		bgAtlas.Region = PremadeRegion;
		bg.Texture       = bgAtlas;
		bg.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		bg.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		bg.StretchMode   = TextureRect.StretchModeEnum.Scale;
		bg.Size          = _root.Size;
		_root.AddChild(bg);

		// Build inventory grid slots
		_gridIcons     = new TextureRect[Cols * Rows];
		_gridCounts    = new Label[Cols * Rows];
		_gridSelectors = new TextureRect[Cols * Rows];
		_gridButtons   = new Button[Cols * Rows];

		for (int r = 0; r < Rows; r++)
		{
			for (int c = 0; c < Cols; c++)
			{
				int idx = r * Cols + c;
				int px  = (GridOrigin.X + c * GridSeparation.X) * UIScale;
				int py  = (GridOrigin.Y + r * GridSeparation.Y) * UIScale;
				BuildGridSlot(idx, px, py);
			}
		}

		// Build equipment slots
		_equipSelectors = new TextureRect[4];
		_headIcon   = BuildEquipSlot(HeadPos,   out _headBtn,   0);
		_bodyIcon   = BuildEquipSlot(BodyPos,   out _bodyBtn,   1);
		_bootsIcon  = BuildEquipSlot(BootsPos,  out _bootsBtn,  2);
		_weaponIcon = BuildEquipSlot(WeaponPos, out _weaponBtn, 3);

		_headBtn.Pressed   += () => Unequip(EquipSlot.Head);
		_bodyBtn.Pressed   += () => Unequip(EquipSlot.Body);
		_bootsBtn.Pressed  += () => Unequip(EquipSlot.Boots);
		_weaponBtn.Pressed += () => Unequip(EquipSlot.Weapon);

		// Stats labels on the action bars area
		_dmgLabel = MakeLabel(DmgLabelPos.X * UIScale, DmgLabelPos.Y * UIScale, 70 * UIScale, "DMG: 1");
		_defLabel = MakeLabel(DefLabelPos.X * UIScale, DefLabelPos.Y * UIScale, 70 * UIScale, "DEF: 0");

		// Equipment slot type labels (right-aligned in the space left of the slot)
		foreach (var (pos, text) in new (Vector2I, string)[]
		{
			(HeadPos, "HEAD"), (BodyPos, "BODY"), (BootsPos, "BOOT"), (WeaponPos, "WPN ")
		})
		{
			int labelY = pos.Y * UIScale + (SlotH - 10) / 2;
			var slotLbl = MakeLabel(1, labelY, pos.X * UIScale - 3, text);
			slotLbl.HorizontalAlignment = HorizontalAlignment.Right;
			slotLbl.AddThemeFontSizeOverride("font_size", 8);
			slotLbl.AddThemeColorOverride("font_color", new Color(0.5f, 0.38f, 0.2f));
		}

		// Detail panel (right of inventory — avoids left-edge clipping)
		BuildDetailPanel();

		// Tooltip
		BuildTooltip();

		// Close hint
		var closeHint = new Label();
		closeHint.Text = "[I] or [ESC] to Close";
		closeHint.Position = new Vector2(PremadeRegion.Size.X * UIScale / 2f - 65,
										  PremadeRegion.Size.Y * UIScale - 13);
		closeHint.Size = new Vector2(130, 12);
		closeHint.HorizontalAlignment = HorizontalAlignment.Center;
		closeHint.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.12f, 0.8f));
		closeHint.AddThemeFontSizeOverride("font_size", 9);
		if (FileAccess.FileExists(DefaultFontPath)) 
			closeHint.AddThemeFontOverride("font", GD.Load<Font>(DefaultFontPath));
		closeHint.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(closeHint);

		// Signals
		Inventory.Instance.Changed += Refresh;
		Equipment.Instance.Changed += Refresh;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		bool togglePressed = @event.IsActionPressed("toggle_inventory") ||
							 (@event.IsActionPressed("ui_cancel") && _isVisible);
		if (togglePressed)
		{
			ToggleInventory();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (!_isVisible) return;

		if      (@event.IsActionPressed("ui_right"))  NavigateRight();
		else if (@event.IsActionPressed("ui_left"))   NavigateLeft();
		else if (@event.IsActionPressed("ui_down"))   NavigateDown();
		else if (@event.IsActionPressed("ui_up"))     NavigateUp();
		else if (@event.IsActionPressed("ui_accept")) OnAcceptPressed();
		else return;

		GetViewport().SetInputAsHandled();
	}

	private void ToggleInventory()
	{
		_isVisible = !_isVisible;
		_root.Visible = _isVisible;
		_overlay.Visible = _isVisible;
		if (_tooltip != null) _tooltip.Visible = false;

		if (_isVisible)
		{
			// Fancy opening animation
			_root.Scale = new Vector2(0.95f, 0.95f);
			_root.Modulate = new Color(1, 1, 1, 0);
			_root.PivotOffset = _root.Size / 2;

			var tween = CreateTween().SetParallel(true).SetIgnoreTimeScale();
			tween.TweenProperty(_root, "scale", Vector2.One, 0.18f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
			tween.TweenProperty(_root, "modulate:a", 1.0f, 0.12f);

			_selectedIndex = -1;
			_activePanel = Panel.Grid;
			_equipSelectedIndex = 0;
			GetTree().Paused = true;
			Refresh();
		}
		else
		{
			ClearDetail();
			GetTree().Paused = false;
		}
	}

	// ── Navigation ─────────────────────────────────────────────────────────
	private void NavigateRight()
	{
		if (_activePanel == Panel.Equipment)
		{
			// Move from equipment to grid column 0
			_activePanel = Panel.Grid;
			_selectedIndex = _equipSelectedIndex * Cols; // same row approx
			if (_selectedIndex >= _itemOrder.Count && _itemOrder.Count > 0)
				_selectedIndex = _itemOrder.Count - 1;
			UpdateVisuals();
		}
		else
		{
			MoveGrid(1);
		}
	}

	private void NavigateLeft()
	{
		if (_activePanel == Panel.Grid)
		{
			// If on column 0, switch to equipment panel
			if (_selectedIndex < 0 || _selectedIndex % Cols == 0)
			{
				_activePanel = Panel.Equipment;
				_equipSelectedIndex = _selectedIndex >= 0 ? _selectedIndex / Cols : 0;
				if (_equipSelectedIndex >= 4) _equipSelectedIndex = 3;
				UpdateVisuals();
			}
			else
			{
				MoveGrid(-1);
			}
		}
		// In equipment panel, left does nothing
	}

	private void NavigateDown()
	{
		if (_activePanel == Panel.Equipment)
		{
			_equipSelectedIndex = Mathf.Min(_equipSelectedIndex + 1, 3);
			UpdateVisuals();
		}
		else
		{
			MoveGrid(Cols);
		}
	}

	private void NavigateUp()
	{
		if (_activePanel == Panel.Equipment)
		{
			_equipSelectedIndex = Mathf.Max(_equipSelectedIndex - 1, 0);
			UpdateVisuals();
		}
		else
		{
			MoveGrid(-Cols);
		}
	}

	private void OnAcceptPressed()
	{
		if (_activePanel == Panel.Equipment)
		{
			Unequip(EquipOrder[_equipSelectedIndex]);
		}
		else
		{
			OnActionPressed();
		}
	}

	private void MoveGrid(int delta)
	{
		if (_itemOrder.Count == 0) return;
		_selectedIndex = _selectedIndex < 0
			? 0
			: Mathf.Clamp(_selectedIndex + delta, 0, _itemOrder.Count - 1);
		UpdateVisuals();
	}

	// ── Build helpers ──────────────────────────────────────────────────────
	private void BuildGridSlot(int idx, int px, int py)
	{
		// Item icon
		var icon = new TextureRect();
		icon.Position      = new Vector2(px + 3, py + 2);
		icon.Size          = new Vector2(IconSz, IconSz);
		icon.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode   = TextureRect.StretchModeEnum.KeepAspectCentered;
		icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		icon.MouseFilter   = Control.MouseFilterEnum.Ignore;
		icon.Visible       = false;
		_root.AddChild(icon);
		_gridIcons[idx] = icon;

		// Stack count
		var count = new Label();
		count.Position = new Vector2(px + SlotW - 18, py + SlotH - 16);
		count.Size     = new Vector2(16, 14);
		count.HorizontalAlignment = HorizontalAlignment.Right;
		count.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.7f));
		count.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
		count.AddThemeFontSizeOverride("font_size", 10);
		count.AddThemeConstantOverride("shadow_offset_x", 1);
		count.AddThemeConstantOverride("shadow_offset_y", 1);
		count.MouseFilter = Control.MouseFilterEnum.Ignore;
		count.Visible = false;
		_root.AddChild(count);
		_gridCounts[idx] = count;

		// Green selector overlay
		var sel = new TextureRect();
		sel.Position      = new Vector2(px - 1, py - 1);
		sel.Size          = new Vector2(SlotW + 2, SlotH + 2);
		sel.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		sel.StretchMode   = TextureRect.StretchModeEnum.Scale;
		sel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		sel.MouseFilter   = Control.MouseFilterEnum.Ignore;
		sel.Visible       = false;
		var selAtlas    = new AtlasTexture();
		selAtlas.Atlas  = GetSelectorTex();
		selAtlas.Region = SelectorRegion;
		sel.Texture     = selAtlas;
		_root.AddChild(sel);
		_gridSelectors[idx] = sel;

		// Invisible click button
		var btn = new Button { Flat = true };
		btn.Position    = new Vector2(px, py);
		btn.Size        = new Vector2(SlotW, SlotH);
		btn.MouseFilter = Control.MouseFilterEnum.Stop;
		btn.FocusMode   = Control.FocusModeEnum.None;
		int cap = idx;
		btn.Pressed      += () => { _activePanel = Panel.Grid; _selectedIndex = cap; UpdateVisuals(); };
		btn.MouseEntered += () => { ShowTooltip(cap); AnimateHover(btn); };
		btn.MouseExited  += () => { if (_tooltip != null) _tooltip.Visible = false; btn.Scale = Vector2.One; };
		_root.AddChild(btn);
		_gridButtons[idx] = btn;
	}

	private TextureRect BuildEquipSlot(Vector2I localPos, out Button btn, int equipIdx)
	{
		int px = localPos.X * UIScale;
		int py = localPos.Y * UIScale;

		var icon = new TextureRect();
		icon.Position      = new Vector2(px + 3, py + 2);
		icon.Size          = new Vector2(IconSz, IconSz);
		icon.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode   = TextureRect.StretchModeEnum.KeepAspectCentered;
		icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		icon.MouseFilter   = Control.MouseFilterEnum.Ignore;
		_root.AddChild(icon);

		// Selector for equipment slot
		var sel = new TextureRect();
		sel.Position      = new Vector2(px - 1, py - 1);
		sel.Size          = new Vector2(SlotW + 2, SlotH + 2);
		sel.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		sel.StretchMode   = TextureRect.StretchModeEnum.Scale;
		sel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		sel.MouseFilter   = Control.MouseFilterEnum.Ignore;
		sel.Visible       = false;
		var selAtlas    = new AtlasTexture();
		selAtlas.Atlas  = GetSelectorTex();
		selAtlas.Region = SelectorRegion;
		sel.Texture     = selAtlas;
		_root.AddChild(sel);
		_equipSelectors[equipIdx] = sel;

		btn = new Button { Flat = true };
		var captureBtn = btn; // Capture for lambda
		btn.Position    = new Vector2(px, py);
		btn.Size        = new Vector2(SlotW, SlotH);
		btn.MouseFilter = Control.MouseFilterEnum.Stop;
		btn.FocusMode   = Control.FocusModeEnum.None;
		int cap = equipIdx;
		btn.Pressed += () => { _activePanel = Panel.Equipment; _equipSelectedIndex = cap; UpdateVisuals(); };
		btn.MouseEntered += () => AnimateHover(captureBtn);
		btn.MouseExited  += () => captureBtn.Scale = Vector2.One;
		_root.AddChild(btn);

		return icon;
	}

	private Label MakeLabel(int x, int y, int w, string text)
	{
		var lbl = new Label();
		lbl.Position = new Vector2(x, y);
		lbl.Size     = new Vector2(w, 14);
		lbl.Text     = text;
		lbl.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.12f)); // Dark brown for readability on tan
		lbl.AddThemeColorOverride("font_shadow_color", new Color(1, 1, 1, 0.3f)); // Light shadow for "etched" look
		lbl.AddThemeFontSizeOverride("font_size", 10);
		if (FileAccess.FileExists(DefaultFontPath))
			lbl.AddThemeFontOverride("font", GD.Load<Font>(DefaultFontPath));
		lbl.AddThemeConstantOverride("shadow_offset_x", 1);
		lbl.AddThemeConstantOverride("shadow_offset_y", 1);
		lbl.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(lbl);
		return lbl;
	}

	private void BuildDetailPanel()
	{
		const int detailW = 160;
		const int detailH = 260;
		const int gap     = 6;

		_detailPanel = new Control();
		_detailPanel.Size     = new Vector2(detailW, detailH);
		_detailPanel.Position = new Vector2(PremadeRegion.Size.X * UIScale + gap, 0);
		_detailPanel.Visible  = false;
		_root.AddChild(_detailPanel);

		// Background frame
		var detailBg      = new TextureRect();
		var detailBgAtlas  = new AtlasTexture();
		detailBgAtlas.Atlas  = GetFrameTex();
		detailBgAtlas.Region = new Rect2(52, 49, 40, 46);
		detailBg.Texture       = detailBgAtlas;
		detailBg.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		detailBg.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		detailBg.StretchMode   = TextureRect.StretchModeEnum.Scale;
		detailBg.Size          = new Vector2(detailW, detailH);
		_detailPanel.AddChild(detailBg);

		_detailIcon = new TextureRect();
		_detailIcon.Position      = new Vector2(detailW / 2 - 24, 12);
		_detailIcon.Size          = new Vector2(48, 48);
		_detailIcon.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
		_detailIcon.StretchMode   = TextureRect.StretchModeEnum.KeepAspectCentered;
		_detailIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		_detailIcon.MouseFilter   = Control.MouseFilterEnum.Ignore;
		_detailPanel.AddChild(_detailIcon);

		_detailName = new Label();
		_detailName.Position = new Vector2(8, 64);
		_detailName.Size     = new Vector2(detailW - 16, 18);
		_detailName.HorizontalAlignment = HorizontalAlignment.Center;
		_detailName.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.2f));
		_detailName.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_detailName.AddThemeFontSizeOverride("font_size", 13);
		_detailName.AddThemeConstantOverride("shadow_offset_x", 1);
		_detailName.AddThemeConstantOverride("shadow_offset_y", 1);
		_detailName.MouseFilter = Control.MouseFilterEnum.Ignore;
		_detailPanel.AddChild(_detailName);

		_detailCategory = new Label();
		_detailCategory.Position = new Vector2(8, 82);
		_detailCategory.Size     = new Vector2(detailW - 16, 14);
		_detailCategory.HorizontalAlignment = HorizontalAlignment.Center;
		_detailCategory.AddThemeColorOverride("font_color", new Color(0.6f, 0.5f, 0.35f));
		_detailCategory.AddThemeFontSizeOverride("font_size", 10);
		_detailCategory.MouseFilter = Control.MouseFilterEnum.Ignore;
		_detailPanel.AddChild(_detailCategory);

		_detailDesc = new Label();
		_detailDesc.Position     = new Vector2(10, 100);
		_detailDesc.Size         = new Vector2(detailW - 20, 50);
		_detailDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_detailDesc.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.12f));
		_detailDesc.AddThemeFontSizeOverride("font_size", 10);
		_detailDesc.MouseFilter = Control.MouseFilterEnum.Ignore;
		_detailPanel.AddChild(_detailDesc);

		_detailStats = new Label();
		_detailStats.Position = new Vector2(10, 155);
		_detailStats.Size     = new Vector2(detailW - 20, 30);
		_detailStats.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_detailStats.AddThemeColorOverride("font_color", new Color(0.2f, 0.55f, 0.3f));
		_detailStats.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.5f));
		_detailStats.AddThemeFontSizeOverride("font_size", 11);
		_detailStats.AddThemeConstantOverride("shadow_offset_x", 1);
		_detailStats.AddThemeConstantOverride("shadow_offset_y", 1);
		_detailStats.MouseFilter = Control.MouseFilterEnum.Ignore;
		_detailPanel.AddChild(_detailStats);

		_actionBtn = new Button();
		_actionBtn.Position   = new Vector2(10, 195);
		_actionBtn.Size       = new Vector2(detailW - 20, 26);
		_actionBtn.FocusMode  = Control.FocusModeEnum.None;
		_actionBtn.AddThemeFontSizeOverride("font_size", 11);
		_actionBtn.Pressed += OnActionPressed;
		_detailPanel.AddChild(_actionBtn);

		_dropBtn = new Button();
		_dropBtn.Text      = "Drop";
		_dropBtn.Position  = new Vector2(10, 225);
		_dropBtn.Size      = new Vector2(detailW - 20, 26);
		_dropBtn.FocusMode = Control.FocusModeEnum.None;
		_dropBtn.AddThemeFontSizeOverride("font_size", 11);
		_dropBtn.Pressed  += OnDropPressed;
		_detailPanel.AddChild(_dropBtn);
	}

	private void BuildTooltip()
	{
		_tooltip = new PanelContainer();
		_tooltip.ZIndex  = 50;
		_tooltip.Visible = false;

		var tooltipAtlas   = new AtlasTexture();
		tooltipAtlas.Atlas  = GetFrameTex();
		tooltipAtlas.Region = new Rect2(100, 49, 40, 46);
		var tooltipStyle = new StyleBoxTexture
		{
			Texture = tooltipAtlas,
			TextureMarginLeft = 8, TextureMarginTop = 8,
			TextureMarginRight = 8, TextureMarginBottom = 8,
			ContentMarginLeft = 8, ContentMarginTop = 6,
			ContentMarginRight = 8, ContentMarginBottom = 6,
		};
		_tooltip.AddThemeStyleboxOverride("panel", tooltipStyle);

		var vbox = new VBoxContainer();
		_tooltip.AddChild(vbox);

		_tooltipName = new Label();
		_tooltipName.AddThemeColorOverride("font_color", new Color(0.6f, 0.25f, 0.1f));
		_tooltipName.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(_tooltipName);

		_tooltipDesc = new Label();
		_tooltipDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_tooltipDesc.CustomMinimumSize = new Vector2(180, 0);
		_tooltipDesc.AddThemeColorOverride("font_color", new Color(0.3f, 0.18f, 0.08f));
		_tooltipDesc.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(_tooltipDesc);

		AddChild(_tooltip);
	}

	// ── Refresh ────────────────────────────────────────────────────────────
	private void Refresh()
	{
		_itemOrder.Clear();
		foreach (var (id, cnt) in Inventory.Instance.Items)
		{
			if (cnt <= 0) continue;
			var data = ItemDatabase.Instance?.Get(id);
			if (data != null)
				_itemOrder.Add(data);
		}

		for (int i = 0; i < Cols * Rows; i++)
		{
			if (i < _itemOrder.Count)
			{
				var data  = _itemOrder[i];
				int stack = Inventory.Instance.GetCount(data.Id);
				_gridIcons[i].Texture = data.Icon;
				_gridIcons[i].Visible = true;
				_gridCounts[i].Text    = stack > 1 ? stack.ToString() : "";
				_gridCounts[i].Visible = stack > 1;
			}
			else
			{
				_gridIcons[i].Texture = null;
				_gridIcons[i].Visible = false;
				_gridCounts[i].Visible = false;
			}
		}

		if (_selectedIndex >= _itemOrder.Count)
			_selectedIndex = _itemOrder.Count - 1;

		RefreshEquipment();
		UpdateVisuals();
	}

	private static readonly Color PlaceholderTint = new(1f, 1f, 1f, 0.2f);

	private void RefreshEquipment()
	{
		RefreshEquipIcon(_weaponIcon, EquipSlot.Weapon, "IronSword");
		RefreshEquipIcon(_headIcon,   EquipSlot.Head,   "LeatherHelmet");
		RefreshEquipIcon(_bodyIcon,   EquipSlot.Body,   "LeatherArmor");
		RefreshEquipIcon(_bootsIcon,  EquipSlot.Boots,  "LeatherBoots");

		_dmgLabel.Text = $"DMG: {Equipment.Instance.GetAttackDamage()}";
		_defLabel.Text = $"DEF: {Equipment.Instance.GetTotalDefence()}";
	}

	private void RefreshEquipIcon(TextureRect icon, EquipSlot slot, string placeholderId)
	{
		var data = Equipment.Instance.GetSlotData(slot);
		if (data != null)
		{
			icon.Texture  = data.Icon;
			icon.Modulate = Colors.White;
		}
		else
		{
			var ph = ItemDatabase.Instance?.Get(placeholderId);
			icon.Texture  = ph?.Icon;
			icon.Modulate = PlaceholderTint;
		}
		icon.Visible = true;
	}

	private void UpdateVisuals()
	{
		// Grid selectors
		for (int i = 0; i < Cols * Rows; i++)
		{
			bool isSel = (_activePanel == Panel.Grid && i == _selectedIndex && i < _itemOrder.Count);
			if (isSel && !_gridSelectors[i].Visible)
				AnimatePop(_gridSelectors[i]);
			_gridSelectors[i].Visible = isSel;
		}

		// Equipment selectors
		for (int i = 0; i < 4; i++)
		{
			bool isSel = (_activePanel == Panel.Equipment && i == _equipSelectedIndex);
			if (isSel && !_equipSelectors[i].Visible)
				AnimatePop(_equipSelectors[i]);
			_equipSelectors[i].Visible = isSel;
		}

		// Detail panel
		if (_activePanel == Panel.Grid && _selectedIndex >= 0 && _selectedIndex < _itemOrder.Count)
		{
			ShowDetail(_itemOrder[_selectedIndex]);
		}
		else if (_activePanel == Panel.Equipment)
		{
			var slot = EquipOrder[_equipSelectedIndex];
			var data = Equipment.Instance.GetSlotData(slot);
			if (data != null)
				ShowDetail(data);
			else
				ClearDetail();
		}
		else
		{
			ClearDetail();
		}
	}

	private void ShowDetail(ItemData data)
	{
		_detailPanel.Visible = true;
		_detailIcon.Texture  = data.Icon;
		_detailName.Text     = data.DisplayName;
		_detailCategory.Text = data.Category.ToString();
		_detailDesc.Text     = data.Description;

		string stats = "";
		if (data.WeaponDamage > 0) stats += $"Damage: {data.WeaponDamage}  ";
		if (data.ArmorRating  > 0) stats += $"Defence: +{data.ArmorRating}  ";
		if (data.HealAmount   > 0) stats += $"Heals: {data.HealAmount} HP";
		_detailStats.Text = stats;

		if (data.Category is ItemCategory.Weapon or ItemCategory.Armor or ItemCategory.Tool)
		{
			_actionBtn.Text    = "Equip";
			_actionBtn.Visible = true;
		}
		else if (data.Category is ItemCategory.Food or ItemCategory.Potion)
		{
			_actionBtn.Text    = "Use";
			_actionBtn.Visible = true;
		}
		else
		{
			_actionBtn.Visible = false;
		}
	}

	private void ClearDetail()
	{
		_detailPanel.Visible = false;
	}

	// ── Tooltip ────────────────────────────────────────────────────────────
	private void ShowTooltip(int index)
	{
		if (index >= _itemOrder.Count) { _tooltip.Visible = false; return; }

		var data = _itemOrder[index];
		_tooltipName.Text = data.DisplayName;

		string desc = data.Description;
		if (data.WeaponDamage > 0) desc += $"\nDamage: {data.WeaponDamage}";
		if (data.ArmorRating  > 0) desc += $"\nDefence: +{data.ArmorRating}";
		if (data.HealAmount   > 0) desc += $"\nHeals: {data.HealAmount} HP";
		_tooltipDesc.Text = desc;

		_tooltip.Visible = true;
		var mouse    = _tooltip.GetViewport().GetMousePosition();
		var viewSize = _tooltip.GetViewportRect().Size;
		float x = Mathf.Clamp(mouse.X + 14, 0, viewSize.X - 180);
		float y = Mathf.Clamp(mouse.Y + 14, 0, viewSize.Y - 80);
		_tooltip.Position = new Vector2(x, y);
	}

	// ── Actions ────────────────────────────────────────────────────────────
	private void OnActionPressed()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _itemOrder.Count) return;
		var data = _itemOrder[_selectedIndex];

		if (data.Category is ItemCategory.Weapon or ItemCategory.Armor or ItemCategory.Tool)
		{
			Equipment.Instance.Equip(data.Id);
		}
		else if (data.Category is ItemCategory.Food or ItemCategory.Potion)
		{
			var player = GetTree().GetFirstNodeInGroup("player") as Player;
			if (player != null && player.UseConsumable(data.Id))
				NotificationManager.Instance?.ShowHeal(data.HealAmount);
		}
		Refresh();
	}

	private void OnDropPressed()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _itemOrder.Count) return;
		var data = _itemOrder[_selectedIndex];
		Inventory.Instance.RemoveItem(data.Id, 1);
		Refresh();
	}

	private void Unequip(EquipSlot slot)
	{
		Equipment.Instance.Unequip(slot);
		Refresh();
	}

	// ── Animations ─────────────────────────────────────────────────────────
	private void AnimatePop(Control target)
	{
		target.PivotOffset = target.Size / 2;
		target.Scale = new Vector2(1.3f, 1.3f);
		var tween = CreateTween().SetIgnoreTimeScale();
		tween.TweenProperty(target, "scale", Vector2.One, 0.16f)
			 .SetTrans(Tween.TransitionType.Back)
			 .SetEase(Tween.EaseType.Out);
	}

	private void AnimateHover(Button target)
	{
		target.PivotOffset = target.Size / 2;
		var tween = CreateTween().SetIgnoreTimeScale();
		tween.TweenProperty(target, "scale", new Vector2(1.1f, 1.1f), 0.1f);
	}

	// ── Texture helpers ────────────────────────────────────────────────────
	private Texture2D GetPremadeTex()
	{
		if (PremadeTexture != null) return PremadeTexture;
		return GetTex(DefaultPremadePath);
	}

	private Texture2D GetSelectorTex()
	{
		if (SelectorTexture != null) return SelectorTexture;
		return GetTex(DefaultSelectorsPath);
	}

	private Texture2D GetFrameTex()
	{
		if (FrameTexture != null) return FrameTexture;
		return GetTex(DefaultFramesPath);
	}

	private Texture2D GetTex(string path)
	{
		if (!_texCache.TryGetValue(path, out var tex))
			_texCache[path] = tex = GD.Load<Texture2D>(path);
		return tex;
	}
}
