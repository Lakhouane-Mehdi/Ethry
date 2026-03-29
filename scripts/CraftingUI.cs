using Godot;

public partial class CraftingUI : CanvasLayer
{
	private PanelContainer _panel;
	private VBoxContainer _recipeList;
	private TextureRect _resultIcon;
	private Label _resultName;
	private Label _resultDesc;
	private VBoxContainer _ingredientList;
	private Button _craftButton;
	private int _selectedIndex;
	private bool _isOpen;

	public bool IsOpen => _isOpen;

	private static readonly Color BgColor = new(0.12f, 0.08f, 0.06f, 0.95f);
	private static readonly Color SlotColor = new(0.25f, 0.18f, 0.12f, 0.8f);
	private static readonly Color SlotSelectedColor = new(0.6f, 0.45f, 0.2f, 0.9f);
	private static readonly Color SlotBorder = new(0.45f, 0.32f, 0.18f, 1f);
	private static readonly Color SlotSelectedBorder = new(0.9f, 0.7f, 0.3f, 1f);
	private static readonly Color TitleColor = new(0.95f, 0.85f, 0.6f);
	private static readonly Color TextColor = new(0.9f, 0.82f, 0.65f);
	private static readonly Color TextDim = new(0.65f, 0.55f, 0.4f);
	private static readonly Color GreenColor = new(0.4f, 0.85f, 0.4f);
	private static readonly Color RedColor = new(0.9f, 0.35f, 0.3f);

	private readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = new();

	public override void _Ready()
	{
		Layer = 50;
		ProcessMode = ProcessModeEnum.Always;
		BuildUI();
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
		// Center panel
		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", MakeStyleBox(BgColor, SlotBorder, 3, 8, 14));
		_panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		_panel.OffsetLeft = -200;
		_panel.OffsetTop = -180;
		_panel.OffsetRight = 200;
		_panel.OffsetBottom = 180;
		_panel.Visible = false;
		AddChild(_panel);

		var mainVBox = new VBoxContainer();
		mainVBox.AddThemeConstantOverride("separation", 8);
		_panel.AddChild(mainVBox);

		// Title
		var title = new Label();
		title.Text = "CRAFTING TABLE";
		title.AddThemeColorOverride("font_color", TitleColor);
		title.AddThemeFontSizeOverride("font_size", 16);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(title);

		// Separator
		var sep = new HSeparator();
		sep.AddThemeConstantOverride("separation", 4);
		mainVBox.AddChild(sep);

		// Content: left recipes + right details
		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 12);
		hbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		mainVBox.AddChild(hbox);

		// Left: recipe list in scroll
		var leftPanel = new PanelContainer();
		leftPanel.AddThemeStyleboxOverride("panel", MakeStyleBox(new Color(0.15f, 0.1f, 0.07f, 0.6f), SlotBorder, 1, 4, 4));
		leftPanel.CustomMinimumSize = new Vector2(150, 0);
		leftPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		hbox.AddChild(leftPanel);

		var scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		leftPanel.AddChild(scroll);

		_recipeList = new VBoxContainer();
		_recipeList.AddThemeConstantOverride("separation", 3);
		_recipeList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		scroll.AddChild(_recipeList);

		// Right: detail panel
		var rightPanel = new VBoxContainer();
		rightPanel.AddThemeConstantOverride("separation", 8);
		rightPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		hbox.AddChild(rightPanel);

		// Result header
		var resultHBox = new HBoxContainer();
		resultHBox.AddThemeConstantOverride("separation", 8);
		rightPanel.AddChild(resultHBox);

		_resultIcon = new TextureRect();
		_resultIcon.CustomMinimumSize = new Vector2(40, 40);
		_resultIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_resultIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_resultIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		resultHBox.AddChild(_resultIcon);

		var resultVBox = new VBoxContainer();
		resultVBox.AddThemeConstantOverride("separation", 2);
		resultHBox.AddChild(resultVBox);

		_resultName = new Label();
		_resultName.AddThemeColorOverride("font_color", TitleColor);
		_resultName.AddThemeFontSizeOverride("font_size", 14);
		resultVBox.AddChild(_resultName);

		_resultDesc = new Label();
		_resultDesc.AddThemeColorOverride("font_color", TextDim);
		_resultDesc.AddThemeFontSizeOverride("font_size", 10);
		_resultDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		resultVBox.AddChild(_resultDesc);

		// Separator
		var sep2 = new HSeparator();
		sep2.AddThemeConstantOverride("separation", 4);
		rightPanel.AddChild(sep2);

		// Ingredients label
		var ingLabel = new Label();
		ingLabel.Text = "INGREDIENTS";
		ingLabel.AddThemeColorOverride("font_color", TextDim);
		ingLabel.AddThemeFontSizeOverride("font_size", 10);
		rightPanel.AddChild(ingLabel);

		// Ingredient list
		_ingredientList = new VBoxContainer();
		_ingredientList.AddThemeConstantOverride("separation", 4);
		_ingredientList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		rightPanel.AddChild(_ingredientList);

		// Craft button
		_craftButton = new Button();
		_craftButton.Text = "CRAFT";
		_craftButton.CustomMinimumSize = new Vector2(0, 32);
		_craftButton.AddThemeColorOverride("font_color", TitleColor);
		_craftButton.AddThemeFontSizeOverride("font_size", 13);
		_craftButton.AddThemeStyleboxOverride("normal", MakeStyleBox(new Color(0.3f, 0.5f, 0.2f, 0.9f), new Color(0.5f, 0.7f, 0.3f), 2, 4, 4));
		_craftButton.AddThemeStyleboxOverride("hover", MakeStyleBox(new Color(0.35f, 0.6f, 0.25f, 0.95f), new Color(0.6f, 0.8f, 0.35f), 2, 4, 4));
		_craftButton.AddThemeStyleboxOverride("pressed", MakeStyleBox(new Color(0.2f, 0.4f, 0.15f, 1f), new Color(0.4f, 0.6f, 0.2f), 2, 4, 4));
		_craftButton.AddThemeStyleboxOverride("disabled", MakeStyleBox(new Color(0.2f, 0.15f, 0.1f, 0.7f), SlotBorder, 2, 4, 4));
		_craftButton.Pressed += OnCraftPressed;
		rightPanel.AddChild(_craftButton);

		// Close hint
		var closeLabel = new Label();
		closeLabel.Text = "[E] Close";
		closeLabel.AddThemeColorOverride("font_color", TextDim);
		closeLabel.AddThemeFontSizeOverride("font_size", 9);
		closeLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(closeLabel);
	}

	public void Open()
	{
		_isOpen = true;
		_panel.Visible = true;
		_selectedIndex = 0;
		GetTree().Paused = true;
		Refresh();
	}

	public void Close()
	{
		_isOpen = false;
		_panel.Visible = false;
		GetTree().Paused = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isOpen) return;

		if (@event.IsActionPressed("ui_down"))
		{
			_selectedIndex = Mathf.Min(_selectedIndex + 1, CraftingRecipes.All.Length - 1);
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_up"))
		{
			_selectedIndex = Mathf.Max(_selectedIndex - 1, 0);
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_accept"))
		{
			OnCraftPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Refresh()
	{
		// Clear recipe list
		foreach (Node child in _recipeList.GetChildren())
			child.QueueFree();

		// Populate recipes
		for (int i = 0; i < CraftingRecipes.All.Length; i++)
		{
			var recipe = CraftingRecipes.All[i];
			bool canCraft = CraftingRecipes.CanCraft(recipe);
			bool selected = i == _selectedIndex;

			var recipeBtn = new PanelContainer();
			recipeBtn.AddThemeStyleboxOverride("panel", MakeStyleBox(
				selected ? SlotSelectedColor : SlotColor,
				selected ? SlotSelectedBorder : SlotBorder,
				selected ? 2 : 1, 3, 4));
			recipeBtn.CustomMinimumSize = new Vector2(0, 30);

			var hbox = new HBoxContainer();
			hbox.AddThemeConstantOverride("separation", 6);
			recipeBtn.AddChild(hbox);

			// Recipe icon
			var icon = new TextureRect();
			icon.CustomMinimumSize = new Vector2(24, 24);
			icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			icon.Texture = GetIconAtlas(recipe.Result);
			icon.Modulate = canCraft ? Colors.White : new Color(1, 1, 1, 0.4f);
			hbox.AddChild(icon);

			// Recipe name
			var nameLabel = new Label();
			nameLabel.Text = ItemRegistry.GetName(recipe.Result);
			nameLabel.AddThemeColorOverride("font_color", canCraft ? TextColor : TextDim);
			nameLabel.AddThemeFontSizeOverride("font_size", 11);
			nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			hbox.AddChild(nameLabel);

			// Click handler
			var btn = new Button();
			btn.Flat = true;
			btn.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			btn.MouseFilter = Control.MouseFilterEnum.Stop;
			int capturedI = i;
			btn.Pressed += () => { _selectedIndex = capturedI; Refresh(); };
			recipeBtn.AddChild(btn);

			_recipeList.AddChild(recipeBtn);
		}

		// Update detail panel
		if (_selectedIndex >= 0 && _selectedIndex < CraftingRecipes.All.Length)
		{
			var recipe = CraftingRecipes.All[_selectedIndex];
			bool canCraft = CraftingRecipes.CanCraft(recipe);

			_resultIcon.Texture = GetIconAtlas(recipe.Result);
			_resultName.Text = recipe.ResultAmount > 1
				? $"{ItemRegistry.GetName(recipe.Result)} x{recipe.ResultAmount}"
				: ItemRegistry.GetName(recipe.Result);
			_resultDesc.Text = ItemRegistry.GetDescription(recipe.Result);

			int dmg = ItemRegistry.GetWeaponDamage(recipe.Result);
			if (dmg > 0)
				_resultDesc.Text += $"\nDamage: {dmg}";

			// Ingredients
			foreach (Node child in _ingredientList.GetChildren())
				child.QueueFree();

			foreach (var (type, amount) in recipe.Ingredients)
			{
				var ingHBox = new HBoxContainer();
				ingHBox.AddThemeConstantOverride("separation", 6);

				var ingIcon = new TextureRect();
				ingIcon.CustomMinimumSize = new Vector2(18, 18);
				ingIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
				ingIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
				ingIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
				ingIcon.Texture = GetIconAtlas(type);
				ingHBox.AddChild(ingIcon);

				int have = Inventory.Instance.GetCount(type);
				bool enough = have >= amount;

				var ingLabel = new Label();
				ingLabel.Text = $"{ItemRegistry.GetName(type)}  {have}/{amount}";
				ingLabel.AddThemeColorOverride("font_color", enough ? GreenColor : RedColor);
				ingLabel.AddThemeFontSizeOverride("font_size", 11);
				ingHBox.AddChild(ingLabel);

				_ingredientList.AddChild(ingHBox);
			}

			_craftButton.Disabled = !canCraft;
			_craftButton.Text = canCraft ? "CRAFT" : "NOT ENOUGH MATERIALS";
		}
	}

	private void OnCraftPressed()
	{
		if (_selectedIndex < 0 || _selectedIndex >= CraftingRecipes.All.Length) return;

		var recipe = CraftingRecipes.All[_selectedIndex];
		if (CraftingRecipes.Craft(recipe))
			Refresh();
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
