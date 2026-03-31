using Godot;

/// <summary>
/// Generic interactable crafting station.
/// Subclasses (or exported fields) set the recipe list, title, and prompt text.
/// Reuses the shared CraftingUI scene for all station types.
/// </summary>
public partial class CraftingStation : Area2D
{
	[Export] public string StationTitle  = "CRAFTING TABLE";
	[Export] public string PromptText    = "Press E to Craft";
	[Export] public int    StationType   = 0; // 0 = Table, 1 = Furnace, 2 = Anvil

	private bool       _playerInRange;
	private Label      _prompt;
	private CraftingUI _craftingUI;

	public override void _Ready()
	{
		Monitoring   = true;
		Monitorable  = true;
		CollisionLayer = 0;
		CollisionMask  = 1;
		ProcessMode    = ProcessModeEnum.Always;

		BodyEntered += OnBodyEntered;
		BodyExited  += OnBodyExited;

		var staticBody = GetNodeOrNull<StaticBody2D>("StaticBody2D");
		if (staticBody != null)
			staticBody.CollisionLayer = 1;

		// Prompt label
		_prompt = new Label();
		_prompt.Text     = PromptText;
		_prompt.Position = new Vector2(-40, -50);
		_prompt.AddThemeColorOverride("font_color", Colors.White);
		_prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
		_prompt.AddThemeFontSizeOverride("font_size", 10);
		_prompt.AddThemeConstantOverride("shadow_offset_x", 1);
		_prompt.AddThemeConstantOverride("shadow_offset_y", 1);
		_prompt.Visible = false;
		AddChild(_prompt);

		// Crafting UI — shared scene, different recipes per station
		var craftScene = GD.Load<PackedScene>("res://scenes/ui/crafting_ui.tscn");
		_craftingUI = craftScene.Instantiate<CraftingUI>();
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
				_craftingUI.Open(GetRecipes(), StationTitle);
		}
	}

	private CraftingRecipe[] GetRecipes() => StationType switch
	{
		1 => CraftingRecipes.Furnace,
		2 => CraftingRecipes.Anvil,
		_ => CraftingRecipes.All,
	};

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
		if (_craftingUI.IsOpen)
			_craftingUI.Close();
	}
}
