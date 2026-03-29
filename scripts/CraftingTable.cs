using Godot;

public partial class CraftingTable : Area2D
{
	private bool _playerInRange;
	private Label _prompt;
	private CraftingUI _craftingUI;

	public override void _Ready()
	{
		Monitoring = true;
		Monitorable = true;
		CollisionLayer = 0;
		CollisionMask = 1;

		ProcessMode = ProcessModeEnum.Always;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		// Keep StaticBody2D on layer 1 for physical collision with player,
		// but exclude it from our Area2D detection by setting our mask to only layer 1
		// (the body type filter in OnBodyEntered handles the rest)
		var staticBody = GetNodeOrNull<StaticBody2D>("StaticBody2D");
		if (staticBody != null)
			staticBody.CollisionLayer = 1;

		// Prompt label
		_prompt = new Label();
		_prompt.Text = "Press E to Craft";
		_prompt.Position = new Vector2(-40, -50);
		_prompt.AddThemeColorOverride("font_color", Colors.White);
		_prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_prompt.AddThemeFontSizeOverride("font_size", 10);
		_prompt.AddThemeConstantOverride("shadow_offset_x", 1);
		_prompt.AddThemeConstantOverride("shadow_offset_y", 1);
		_prompt.Visible = false;
		AddChild(_prompt);

		// Crafting UI (hidden by default)
		_craftingUI = new CraftingUI();
		AddChild(_craftingUI);
	}

	public override void _Process(double delta)
	{
		if (!_playerInRange) return;

		if (Input.IsActionJustPressed("interact"))
		{
			if (_craftingUI.IsOpen)
				_craftingUI.Close();
			else
				_craftingUI.Open();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = true;
		_prompt.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not CharacterBody2D || !body.IsInGroup("player")) return;
		_playerInRange = false;
		_prompt.Visible = false;
		if (_craftingUI.IsOpen)
			_craftingUI.Close();
	}
}
