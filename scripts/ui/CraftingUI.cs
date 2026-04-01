using Godot;

/// <summary>
/// Crafting UI — reads layout from scenes/ui/crafting_ui.tscn.
/// All visual style comes from the cute_fantasy_ui asset pack.
/// </summary>
public partial class CraftingUI : CanvasLayer
{
	// ── Scene nodes ────────────────────────────────────────────────────────
	private PanelContainer _panel;
	private Label          _titleLabel;
	private VBoxContainer  _recipeList;
	private TextureRect    _resultIcon;
	private Label          _resultName;
	private Label          _resultDesc;
	private VBoxContainer  _ingredientList;
	private Button         _craftButton;

	// ── State ──────────────────────────────────────────────────────────────
	private int  _selectedIndex;
	private bool _isOpen;
	public  bool IsOpen => _isOpen;
	private CraftingRecipe[] _recipes = CraftingRecipes.All;

	// ── Asset constants ────────────────────────────────────────────────────
	private readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = new();

	private const string FramesPath   = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_frames.png";
	private const string IconsPath   = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_icons.png";
	private const string ButtonsPath = "res://assets/cute_fantasy_ui/cute_fantasy_ui/ui_buttons.png";
	private const string FontPath    = "res://assets/cute_fantasy_ui/cute_fantasy_ui/font.fnt";

	// Button regions from ui_buttons.png — 47×14 cells
	private static readonly Rect2 GreenBtnNormal  = new(288, 337, 47, 14);
	private static readonly Rect2 GreenBtnHover   = new(336, 337, 47, 14);
	private static readonly Rect2 GreenBtnPressed = new(384, 337, 47, 14);
	private static readonly Rect2 BrownBtnNormal  = new(0,   337, 47, 14);
	private static readonly Rect2 BrownBtnHover   = new(48,  337, 47, 14);
	private static readonly Rect2 BrownBtnPressed = new(96,  337, 47, 14);

	// Green checkmark and red X from ui_icons.png (16×16 grid)
	private static readonly Rect2 CheckRegion = new(0, 32, 16, 16);
	private static readonly Rect2 CrossRegion = new(96, 48, 16, 16);

	// Recipe row styles colours matched to wood palette
	private static readonly Color TextPrimary   = new(0.32f, 0.16f, 0.06f, 1f);
	private static readonly Color TextSecondary = new(0.42f, 0.24f, 0.08f, 0.85f);
	private static readonly Color IngredOk      = new(0.22f, 0.52f, 0.16f, 1f);
	private static readonly Color IngredBad     = new(0.72f, 0.18f, 0.12f, 1f);

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		_panel          = GetNode<PanelContainer>("Panel");
		_titleLabel     = GetNode<Label>("Panel/MainVBox/TitleLabel");
		_recipeList     = GetNode<VBoxContainer>("Panel/MainVBox/ContentHBox/LeftPanel/RecipeScroll/RecipeList");
		_resultIcon     = GetNode<TextureRect>("Panel/MainVBox/ContentHBox/RightPanel/RightVBox/ResultHBox/ResultIcon");
		_resultName     = GetNode<Label>("Panel/MainVBox/ContentHBox/RightPanel/RightVBox/ResultHBox/ResultVBox/ResultName");
		_resultDesc     = GetNode<Label>("Panel/MainVBox/ContentHBox/RightPanel/RightVBox/ResultHBox/ResultVBox/ResultDesc");
		_ingredientList = GetNode<VBoxContainer>("Panel/MainVBox/ContentHBox/RightPanel/RightVBox/IngredientList");
		_craftButton    = GetNode<Button>("Panel/MainVBox/ContentHBox/RightPanel/RightVBox/CraftButton");

		_craftButton.Pressed += OnCraftPressed;

		// Bitmap pixel font
		var font = new FontFile();
		font.LoadBitmapFont(FontPath);
		var theme = new Theme();
		theme.DefaultFont = font;
		_panel.Theme = theme;

		// Textured button styles
		ApplyButtonStyle(_craftButton, GreenBtnNormal, GreenBtnHover, GreenBtnPressed);
		_craftButton.AddThemeStyleboxOverride("disabled", MakeBtnStyle(BrownBtnPressed));
	}

	// ── Public API ─────────────────────────────────────────────────────────
	public void Open(CraftingRecipe[] recipes = null, string title = null)
	{
		if (recipes != null) _recipes = recipes;
		if (title != null)   _titleLabel.Text = title;
		_isOpen          = true;
		_panel.Visible   = true;
		_selectedIndex   = 0;
		GetTree().Paused = true;
		Refresh();
	}

	public void Close()
	{
		_isOpen          = false;
		_panel.Visible   = false;
		GetTree().Paused = false;
	}

	// ── Input ──────────────────────────────────────────────────────────────
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isOpen) return;

		if (@event.IsActionPressed("ui_down"))
		{
			_selectedIndex = Mathf.Min(_selectedIndex + 1, _recipes.Length - 1);
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

	// ── Refresh ────────────────────────────────────────────────────────────
	private void Refresh()
	{
		// Rebuild recipe list
		foreach (Node child in _recipeList.GetChildren()) child.QueueFree();

		for (int i = 0; i < _recipes.Length; i++)
		{
			var  recipe   = _recipes[i];
			bool canCraft = CraftingRecipes.CanCraft(recipe);
			bool selected = i == _selectedIndex;

			var row = new PanelContainer();
			row.CustomMinimumSize = new Vector2(0, 40);
			row.AddThemeStyleboxOverride("panel", MakeRecipeStyle(selected));

			var hbox = new HBoxContainer();
			hbox.AddThemeConstantOverride("separation", 8);
			row.AddChild(hbox);

			var icon = new TextureRect();
			icon.CustomMinimumSize = new Vector2(32, 32);
			icon.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
			icon.StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered;
			icon.TextureFilter     = CanvasItem.TextureFilterEnum.Nearest;
			icon.Texture           = GetIconTexture(recipe.Result);
			icon.Modulate          = canCraft ? Colors.White : new Color(1, 1, 1, 0.38f);
			icon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			hbox.AddChild(icon);

			var nameLabel = new Label();
			nameLabel.Text = GetItemName(recipe.Result);
			nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			nameLabel.SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter;
			nameLabel.AddThemeColorOverride("font_color", canCraft ? TextPrimary : TextSecondary);
			nameLabel.AddThemeFontSizeOverride("font_size", 11);
			hbox.AddChild(nameLabel);

			// Invisible click button for mouse selection
			var btn = new Button();
			btn.Flat = true;
			btn.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			btn.MouseFilter = Control.MouseFilterEnum.Stop;
			int captured = i;
			btn.Pressed += () => { _selectedIndex = captured; Refresh(); };
			row.AddChild(btn);

			_recipeList.AddChild(row);
		}

		// Update right-side detail panel
		if (_recipes.Length == 0 || _selectedIndex >= _recipes.Length)
			return;

		var sel      = _recipes[_selectedIndex];
		bool canMake = CraftingRecipes.CanCraft(sel);

		_resultIcon.Texture = GetIconTexture(sel.Result);
		string resultName = GetItemName(sel.Result);
		_resultName.Text    = sel.ResultAmount > 1
			? $"{resultName}  ×{sel.ResultAmount}"
			: resultName;

		var resultData = GetItemData(sel.Result);
		string desc  = resultData?.Description ?? ItemRegistry.GetDescription(sel.Result);
		int    dmg   = resultData?.WeaponDamage ?? ItemRegistry.GetWeaponDamage(sel.Result);
		int    arm   = resultData?.ArmorRating  ?? ItemRegistry.GetArmorRating(sel.Result);
		string stats = "";
		if (dmg > 0) stats += $"\nDamage: {dmg}";
		if (arm > 0) stats += $"\nDefence: +{arm}";
		_resultDesc.Text = desc + stats;

		// Rebuild ingredient list
		foreach (Node child in _ingredientList.GetChildren()) child.QueueFree();

		foreach (var (ingType, amount) in sel.Ingredients)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 6);

			var ingIcon = new TextureRect();
			ingIcon.CustomMinimumSize = new Vector2(28, 28);
			ingIcon.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
			ingIcon.StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered;
			ingIcon.TextureFilter     = CanvasItem.TextureFilterEnum.Nearest;
			ingIcon.Texture           = GetIconTexture(ingType);
			ingIcon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			row.AddChild(ingIcon);

			int  have   = Inventory.Instance.GetCount(ingType);
			bool enough = have >= amount;
			var  lbl    = new Label();
			lbl.Text = $"{GetItemName(ingType)}   {have} / {amount}";
			lbl.AddThemeColorOverride("font_color", enough ? IngredOk : IngredBad);
			lbl.AddThemeFontSizeOverride("font_size", 11);
			lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			lbl.SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter;
			row.AddChild(lbl);

			var statusIcon = new TextureRect();
			statusIcon.CustomMinimumSize = new Vector2(14, 14);
			statusIcon.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
			statusIcon.StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered;
			statusIcon.TextureFilter     = CanvasItem.TextureFilterEnum.Nearest;
			statusIcon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			var statusAtlas = new AtlasTexture();
			statusAtlas.Atlas  = GetCachedTexture(IconsPath);
			statusAtlas.Region = enough ? CheckRegion : CrossRegion;
			statusIcon.Texture = statusAtlas;
			row.AddChild(statusIcon);

			_ingredientList.AddChild(row);
		}

		_craftButton.Disabled = !canMake;
		_craftButton.Text     = canMake ? "   CRAFT" : "NOT ENOUGH MATERIALS";
	}

	// ── Craft ──────────────────────────────────────────────────────────────
	private void OnCraftPressed()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _recipes.Length) return;
		var recipe = _recipes[_selectedIndex];
		if (CraftingRecipes.Craft(recipe))
		{
			NotificationManager.Instance?.ShowCraftSuccess(GetItemName(recipe.Result));
			Refresh();
		}
	}

	// ── Helpers ────────────────────────────────────────────────────────────
	private StyleBoxTexture MakeRecipeStyle(bool selected)
	{
		// Frame[0,0] = normal row, Frame[0,1] = selected (lighter) from ui_frames.png
		var atlas   = new AtlasTexture();
		atlas.Atlas  = GetCachedTexture(FramesPath);
		atlas.Region = selected ? new Rect2(52, 7, 40, 36) : new Rect2(4, 7, 40, 36);

		return new StyleBoxTexture
		{
			Texture             = atlas,
			TextureMarginLeft   = 6f,
			TextureMarginTop    = 5f,
			TextureMarginRight  = 6f,
			TextureMarginBottom = 5f,
			ContentMarginLeft   = 4f,
			ContentMarginTop    = 4f,
			ContentMarginRight  = 4f,
			ContentMarginBottom = 4f,
		};
	}

	private Texture2D GetCachedTexture(string path)
	{
		if (!_textureCache.TryGetValue(path, out var tex))
			_textureCache[path] = tex = GD.Load<Texture2D>(path);
		return tex;
	}

	// ── ItemDatabase helpers ───────────────────────────────────────────────
	private static ItemData GetItemData(ItemType type)
		=> ItemDatabase.Instance?.Get(type.ToString());

	private string GetItemName(ItemType type)
	{
		var data = GetItemData(type);
		return data != null ? data.DisplayName : ItemRegistry.GetName(type);
	}

	private Texture2D GetIconTexture(ItemType type)
	{
		var data = GetItemData(type);
		if (data?.Icon != null) return data.Icon;

		// Fallback: atlas slice from ItemRegistry
		var atlas   = new AtlasTexture();
		atlas.Atlas  = GetCachedTexture(ItemRegistry.GetIconTexturePath(type));
		atlas.Region = ItemRegistry.GetIconRegion(type);
		return atlas;
	}

	private StyleBoxTexture MakeBtnStyle(Rect2 region)
	{
		var atlas   = new AtlasTexture();
		atlas.Atlas  = GetCachedTexture(ButtonsPath);
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
