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
	private static readonly Color ColInfo   = new(1.00f, 0.88f, 0.40f, 1f); // Gold-yellow for info
	private static readonly Color ColWhite  = new(1.00f, 1.00f, 1.00f, 1f);

	[Export] public Font CustomFont;
	private VBoxContainer _stack;
	private Label _actionPrompt;

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance    = this;
		Layer       = 20;
		ProcessMode = ProcessModeEnum.Always;

		// Load custom font if available
		if (CustomFont == null)
		{
			string fontPath = "res://assets/cute_fantasy_ui/cute_fantasy_ui/font.fnt";
			if (FileAccess.FileExists(fontPath))
				CustomFont = GD.Load<Font>(fontPath);
		}

		// Anchor stack to bottom-left, just above the hearts HUD area
		_stack = new VBoxContainer();
		_stack.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		_stack.OffsetLeft   =  12f;
		_stack.OffsetBottom = -12f;
		_stack.GrowVertical = Control.GrowDirection.Begin; // grows upward
		_stack.AddThemeConstantOverride("separation", 3);
		AddChild(_stack);

		// Persistent action prompt label
		_actionPrompt = new Label { Visible = false };
		_actionPrompt.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		_actionPrompt.OffsetLeft = 12f;
		_actionPrompt.OffsetBottom = -56f; // Position it above the stack or in a clear spot
		_actionPrompt.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
		_actionPrompt.AddThemeConstantOverride("outline_size", 4);
		_actionPrompt.AddThemeFontSizeOverride("font_size", 11);
		if (CustomFont != null) _actionPrompt.AddThemeFontOverride("font", CustomFont);
		AddChild(_actionPrompt);

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

	/// <summary>Show a generic danger message (Red).</summary>
	public void ShowWarning(string message)
		=> Show(message, ColDanger);

	/// <summary>Show a generic info/requirement message (Yellow/Gold).</summary>
	public void ShowInfo(string message)
		=> Show(message, ColInfo);

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
		lbl.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.5f));
		lbl.AddThemeFontSizeOverride("font_size", 11); // Pixel font size 11 usually crisper
		
		if (CustomFont != null)
			lbl.AddThemeFontOverride("font", CustomFont);

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

	// ── Action Prompt API ──────────────────────────────────────────────────
	/// <summary>Sets a persistent prompt like "Need Axe" in the corner.</summary>
	public void SetActionPrompt(string text, Color? color = null)
	{
		if (_actionPrompt == null) return;
		_actionPrompt.Text = text;
		_actionPrompt.AddThemeColorOverride("font_color", color ?? ColWhite);
		_actionPrompt.Visible = !string.IsNullOrEmpty(text);
	}

	/// <summary>Clears the current corner prompt.</summary>
	public void ClearActionPrompt()
	{
		if (_actionPrompt != null) _actionPrompt.Visible = false;
	}

	// ── Inventory signal ───────────────────────────────────────────────────
	private void OnItemAdded(string itemId, string itemName, int amount)
	{
		var data = ItemDatabase.Instance?.Get(itemId);
		ShowPickup(itemName, amount, data?.Icon);
	}
}
