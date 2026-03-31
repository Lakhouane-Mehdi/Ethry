using Godot;

/// <summary>
/// Interactable NPC with dialogue and optional merchant role.
/// Shows a speech bubble popup when talked to.
/// </summary>
public partial class NPC : CharacterBody2D
{
	[Export] public string NpcName = "Villager";
	[Export] public NpcRole Role = NpcRole.Dialogue;
	[Export(PropertyHint.MultilineText)]
	public string[] DialogueLines = new[] { "Hello, traveller!" };

	// Merchant inventory — item enum values as ints. Only used when Role == Merchant.
	[Export] public int[] ShopItems  = System.Array.Empty<int>();

	private bool  _playerInRange;
	private Label _prompt;
	private int   _dialogueIndex;

	// Speech bubble nodes
	private NinePatchRect _bubble;
	private Label         _bubbleLabel;
	private float         _bubbleTimer;
	private const float   BubbleDuration = 3.5f;

	private static readonly Texture2D PopUpTex =
		GD.Load<Texture2D>("res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_pop_up.png");

	public override void _Ready()
	{
		// Interaction prompt
		_prompt = new Label();
		_prompt.Text = $"Press E to Talk";
		_prompt.Position = new Vector2(-50, -80);
		_prompt.AddThemeColorOverride("font_color", Colors.White);
		_prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_prompt.AddThemeFontSizeOverride("font_size", 10);
		_prompt.AddThemeConstantOverride("shadow_offset_x", 1);
		_prompt.AddThemeConstantOverride("shadow_offset_y", 1);
		_prompt.Visible = false;
		AddChild(_prompt);

		// Speech bubble (from ui_pop_up.png, 96x96, use center 9-patch region)
		_bubble = new NinePatchRect();
		_bubble.Texture = PopUpTex;
		_bubble.RegionRect   = new Rect2(0, 0, 32, 32);
		_bubble.PatchMarginLeft   = 8;
		_bubble.PatchMarginTop    = 8;
		_bubble.PatchMarginRight  = 8;
		_bubble.PatchMarginBottom = 12;
		_bubble.CustomMinimumSize = new Vector2(120, 40);
		_bubble.Position = new Vector2(-60, -120);
		_bubble.Visible  = false;
		AddChild(_bubble);

		_bubbleLabel = new Label();
		_bubbleLabel.Position = new Vector2(6, 4);
		_bubbleLabel.Size     = new Vector2(108, 32);
		_bubbleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_bubbleLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.12f, 0.05f));
		_bubbleLabel.AddThemeFontSizeOverride("font_size", 8);
		_bubble.AddChild(_bubbleLabel);

		// Detection area for player proximity
		var area = new Area2D();
		area.CollisionLayer = 0;
		area.CollisionMask  = 1;
		AddChild(area);

		var shape = new CollisionShape2D();
		var circle = new CircleShape2D();
		circle.Radius = 30f;
		shape.Shape = circle;
		area.AddChild(shape);

		area.BodyEntered += OnBodyEntered;
		area.BodyExited  += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		// Bubble auto-hide timer
		if (_bubble.Visible)
		{
			_bubbleTimer -= (float)delta;
			if (_bubbleTimer <= 0)
				_bubble.Visible = false;
		}

		if (!_playerInRange) return;

		if (Input.IsActionJustPressed("interact"))
			Interact();
	}

	private void Interact()
	{
		if (DialogueLines.Length == 0) return;

		string line = DialogueLines[_dialogueIndex % DialogueLines.Length];
		_dialogueIndex++;

		ShowBubble(line);

		if (Role == NpcRole.Merchant)
			ShowMerchantHint();
	}

	private void ShowBubble(string text)
	{
		_bubbleLabel.Text = text;
		_bubble.Visible   = true;
		_bubbleTimer      = BubbleDuration;

		// Resize bubble to fit text
		float textHeight = Mathf.Max(32, _bubbleLabel.GetMinimumSize().Y + 12);
		_bubble.CustomMinimumSize = new Vector2(120, textHeight);
	}

	private void ShowMerchantHint()
	{
		// Simple merchant: give the player items for free on interact (demo)
		// In a full game this would open a shop UI.
		if (ShopItems.Length > 0 && _dialogueIndex > 1)
		{
			int idx = (_dialogueIndex - 2) % ShopItems.Length;
			var item = (ItemType)ShopItems[idx];
			Inventory.Instance.AddItem(item, 1);
			NotificationManager.Instance?.ShowPickup(ItemRegistry.GetName(item), 1);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange  = true;
		_prompt.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange  = false;
		_prompt.Visible = false;
	}
}

public enum NpcRole
{
	Dialogue,
	Merchant,
	QuestGiver,
}
