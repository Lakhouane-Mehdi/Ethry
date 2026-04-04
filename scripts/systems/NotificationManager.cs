using System.Collections.Generic;
using Godot;

/// <summary>
/// Autoload singleton (CanvasLayer) that shows "+1 Wood" style pop-up messages.
/// Subscribe by calling NotificationManager.Instance.Show(msg, color).
/// Automatically subscribes to Inventory.ItemAdded and CraftingSystem craft events.
/// </summary>
public partial class NotificationManager : CanvasLayer
{
	public static NotificationManager Instance { get; private set; }

	// ── Visual constants ───────────────────────────────────────────────────
	private const float  LifetimeSeconds = 2.4f;
	private const float  RisePixels      = 40f;
	private const int    MaxVisible      = 6;
	private const int    SlotHeightPx    = 24;

	private static readonly Color ColPickup = new(0.95f, 0.88f, 0.55f, 1f);
	private static readonly Color ColCraft  = new(0.45f, 0.90f, 0.38f, 1f);
	private static readonly Color ColHeal   = new(0.42f, 0.88f, 0.52f, 1f);
	private static readonly Color ColDanger = new(0.92f, 0.30f, 0.28f, 1f);

	private VBoxContainer _stack;

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance    = this;
		Layer       = 20;
		ProcessMode = ProcessModeEnum.Always;

		// Anchor stack to bottom-left, just above the hearts HUD area
		_stack = new VBoxContainer();
		_stack.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		_stack.OffsetLeft   =  12f;
		_stack.OffsetBottom = -12f;
		_stack.GrowVertical = Control.GrowDirection.Begin; // grows upward
		_stack.AddThemeConstantOverride("separation", 3);
		AddChild(_stack);

		// Subscribe to inventory pickups
		// (Inventory is an autoload, guaranteed to exist before this node is ready)
		Inventory.Instance.ItemAdded += OnItemAdded;
	}

	// ── Public API ─────────────────────────────────────────────────────────
	/// <summary>Show a pickup notification: "+2 Wood".</summary>
	public void ShowPickup(string itemName, int amount, Texture2D icon = null)
		=> Show($"+{amount}  {itemName}", ColPickup, icon);

	/// <summary>Show a craft success notification.</summary>
	public void ShowCraftSuccess(string itemName)
		=> Show($"✓  Crafted {itemName}", ColCraft);

	/// <summary>Show a heal notification.</summary>
	public void ShowHeal(int amount)
		=> Show($"♥  +{amount} HP", ColHeal);

	/// <summary>Show a generic danger / failure message.</summary>
	public void ShowWarning(string message)
		=> Show(message, ColDanger);

	/// <summary>Low-level: show any string with any colour and optional icon.</summary>
	public void Show(string message, Color color, Texture2D icon = null)
	{
		// Each notification is wrapped in a Control (anchor).
		if (_stack.GetChildCount() >= MaxVisible)
		{
			var old = _stack.GetChild(0);
			old.SetProcess(false); // Stop any further logic
			old.QueueFree();
		}

		var anchor = new Control { CustomMinimumSize = new Vector2(250, SlotHeightPx) };
		_stack.AddChild(anchor);

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 6);
		hbox.ProcessMode = ProcessModeEnum.Always; 
		anchor.AddChild(hbox);

		if (icon != null)
		{
			var rect = new TextureRect
			{
				Texture = icon,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(16, 16),
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest
			};
			hbox.AddChild(rect);
		}

		var lbl = new Label();
		lbl.Text = message;
		lbl.AddThemeColorOverride("font_color", color);
		lbl.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.7f));
		lbl.AddThemeFontSizeOverride("font_size", 16);
		lbl.AddThemeConstantOverride("shadow_offset_x", 1);
		lbl.AddThemeConstantOverride("shadow_offset_y", 1);
		hbox.AddChild(lbl);

		// Tween: fade out + rise upward over LifetimeSeconds.
		var tween = hbox.CreateTween().SetIgnoreTimeScale();
		tween.SetParallel(true);
		tween.TweenProperty(hbox, "modulate:a", 0f, LifetimeSeconds)
			 .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		tween.TweenProperty(hbox, "position:y", -RisePixels, LifetimeSeconds)
			 .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		
		// Fallback Cleanup: Ensures node is freed even if tween is interrupted
		tween.Chain().TweenCallback(Callable.From(anchor.QueueFree));
		
		// Backup timer cleanup (just in case)
		GetTree().CreateTimer(LifetimeSeconds + 0.1f, false).Timeout += () => {
			if (IsInstanceValid(anchor)) anchor.QueueFree();
		};
	}

	// ── Inventory signal ───────────────────────────────────────────────────
	private void OnItemAdded(string itemId, string itemName, int amount)
	{
		var data = ItemDatabase.Instance?.Get(itemId);
		ShowPickup(itemName, amount, data?.Icon);
	}
}
