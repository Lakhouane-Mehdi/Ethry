using System.Collections.Generic;
using Godot;

/// <summary>
/// Always-visible hotbar at the bottom of the screen.
/// Displays the first 5 inventory items.
/// Keys 1–5 activate the slot: consumables are used, equippable items are equipped.
/// </summary>
public partial class Hotbar : CanvasLayer
{
	private const int SlotCount  = 5;
	private const int SlotScale = 3;
	private const int SlotW     = 16 * SlotScale;   // 48 px
	private const int SlotH     = 15 * SlotScale;   // 45 px
	private const int IconSz    = 14 * SlotScale;   // 42 px
	private const int Gap       = 4;

	private const string PremadePath   = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_premade.png";
	private const string SelectorsPath = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_selectors.png";
	private static readonly Rect2 SelectorRegion = new(10, 200, 28, 30);

	// Slot background region from ui_premade (a single empty grid cell)
	private static readonly Rect2 SlotBgRegion = new(78, 7, 16, 15); // one grid slot from premade

	private TextureRect[] _icons;
	private Label[]       _counts;
	private TextureRect[] _selectors;
	private Label[]       _keyLabels;

	private readonly List<string> _slotIds = new();

	private readonly Dictionary<string, Texture2D> _texCache = new();

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Layer       = 5;
		ProcessMode = ProcessModeEnum.Always;

		_icons     = new TextureRect[SlotCount];
		_keyLabels = new Label[SlotCount];
		_counts    = new Label[SlotCount];
		_selectors = new TextureRect[SlotCount];
	
		int totalW = SlotCount * SlotW + (SlotCount - 1) * Gap;

		// Root Control — bottom-centre, just above the HUD hearts
		var root = new Control();
		root.AnchorLeft   = 0.5f;
		root.AnchorRight  = 0.5f;
		root.AnchorTop    = 1.0f;
		root.AnchorBottom = 1.0f;
		root.OffsetLeft   = -totalW / 2f;
		root.OffsetRight  =  totalW / 2f;
		root.OffsetBottom = -54f;   // 54px from bottom (above the heart HUD)
		root.OffsetTop    = root.OffsetBottom - SlotH - 4;
		root.ProcessMode  = ProcessModeEnum.Always;
		AddChild(root);

		var premadeTex  = GetTex(PremadePath);
		var selectorTex = GetTex(SelectorsPath);

		for (int i = 0; i < SlotCount; i++)
		{
			int x = i * (SlotW + Gap);

			// Slot background
			var bg = new TextureRect();
			bg.Position      = new Vector2(x, 0);
			bg.Size          = new Vector2(SlotW, SlotH);
			bg.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
			bg.StretchMode   = TextureRect.StretchModeEnum.Scale;
			bg.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			bg.MouseFilter   = Control.MouseFilterEnum.Ignore;
			var bgAtlas = new AtlasTexture { Atlas = premadeTex, Region = SlotBgRegion };
			bg.Texture = bgAtlas;
			root.AddChild(bg);

			// Item icon
			var icon = new TextureRect();
			icon.Position      = new Vector2(x + 3, 2);
			icon.Size          = new Vector2(IconSz, IconSz);
			icon.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
			icon.StretchMode   = TextureRect.StretchModeEnum.KeepAspectCentered;
			icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			icon.MouseFilter   = Control.MouseFilterEnum.Ignore;
			icon.Visible       = false;
			root.AddChild(icon);
			_icons[i] = icon;

			// Stack count
			var count = new Label();
			count.Position = new Vector2(x + SlotW - 14, SlotH - 13);
			count.Size     = new Vector2(13, 11);
			count.HorizontalAlignment = HorizontalAlignment.Right;
			count.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.7f));
			count.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
			count.AddThemeFontSizeOverride("font_size", 9);
			count.AddThemeConstantOverride("shadow_offset_x", 1);
			count.AddThemeConstantOverride("shadow_offset_y", 1);
			count.MouseFilter = Control.MouseFilterEnum.Ignore;
			count.Visible = false;
			root.AddChild(count);
			_counts[i] = count;

			// Selector (visible on keyboard activation)
			var sel = new TextureRect();
			sel.Position      = new Vector2(x - 1, -1);
			sel.Size          = new Vector2(SlotW + 2, SlotH + 2);
			sel.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;
			sel.StretchMode   = TextureRect.StretchModeEnum.Scale;
			sel.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			sel.MouseFilter   = Control.MouseFilterEnum.Ignore;
			sel.Visible       = false;
			sel.Texture       = new AtlasTexture { Atlas = selectorTex, Region = SelectorRegion };
			root.AddChild(sel);
			_selectors[i] = sel;

			// Key number label (1–5) — hidden by default
			var keyLbl = new Label();
			keyLbl.Text     = (i + 1).ToString();
			keyLbl.Position = new Vector2(x + 2, 1);
			keyLbl.Size     = new Vector2(12, 11);
			keyLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.6f, 0.35f, 0.8f));
			keyLbl.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
			keyLbl.AddThemeFontSizeOverride("font_size", 8);
			keyLbl.AddThemeConstantOverride("shadow_offset_x", 1);
			keyLbl.AddThemeConstantOverride("shadow_offset_y", 1);
			keyLbl.MouseFilter = Control.MouseFilterEnum.Ignore;
			keyLbl.Visible = false;
			root.AddChild(keyLbl);
			_keyLabels[i] = keyLbl;
		}

		Inventory.Instance.Changed += Refresh;
		Refresh();
	}

	// ── Input: keys 1–5 ───────────────────────────────────────────────────
	public override void _UnhandledInput(InputEvent @event)
	{
		for (int i = 0; i < SlotCount; i++)
		{
			string action = $"hotbar_{i + 1}";
			// Fall back to checking numeric keys directly if action not mapped
			bool pressed = false;
			if (InputMap.HasAction(action))
				pressed = @event.IsActionPressed(action);
			else if (@event is InputEventKey key && !key.Echo && key.Pressed)
				pressed = key.PhysicalKeycode == (Key.Key1 + i);

			if (!pressed) continue;

			ActivateSlot(i);
			GetViewport().SetInputAsHandled();
			break;
		}
	}

	// ── Refresh ────────────────────────────────────────────────────────────
	private void Refresh()
	{
		_slotIds.Clear();
		foreach (var (id, cnt) in Inventory.Instance.Items)
		{
			if (cnt > 0) _slotIds.Add(id);
			if (_slotIds.Count >= SlotCount) break;
		}

		for (int i = 0; i < SlotCount; i++)
		{
			if (i < _slotIds.Count)
			{
				string id   = _slotIds[i];
				var    data = ItemDatabase.Instance?.Get(id);
				int    cnt  = Inventory.Instance.GetCount(id);

				_icons[i].Texture = data?.Icon;
				_icons[i].Visible = data?.Icon != null;
				_counts[i].Text    = cnt > 1 ? cnt.ToString() : "";
				_counts[i].Visible = cnt > 1;
			}
			else
			{
				_icons[i].Visible  = false;
				_counts[i].Visible = false;
			}
		}
	}

	// ── Activate ──────────────────────────────────────────────────────────
	private void ActivateSlot(int index)
	{
		if (index >= _slotIds.Count) return;
		string id   = _slotIds[index];
		var    data = ItemDatabase.Instance?.Get(id);
		if (data == null) return;

		// Flash selector briefly
		FlashSelector(index);

		if (data.Category is ItemCategory.Weapon or ItemCategory.Armor or ItemCategory.Tool)
		{
			Equipment.Instance.Equip(id);
		}
		else if (data.Category is ItemCategory.Food or ItemCategory.Potion)
		{
			var player = GetTree().GetFirstNodeInGroup("player") as Player;
			if (player != null && player.UseConsumable(id))
			{
				AudioManager.Instance?.PlaySfx("eat");
				NotificationManager.Instance?.ShowHeal(data.HealAmount);
			}
		}
	}

	private void FlashSelector(int index)
	{
		if (index >= SlotCount) return;
		var sel = _selectors[index];
		sel.Visible = true;
		GetTree().CreateTimer(0.25).Timeout += () => { if (IsInstanceValid(sel)) sel.Visible = false; };
	}

	private Texture2D GetTex(string path)
	{
		if (!_texCache.TryGetValue(path, out var t))
			_texCache[path] = t = GD.Load<Texture2D>(path);
		return t;
	}
}
