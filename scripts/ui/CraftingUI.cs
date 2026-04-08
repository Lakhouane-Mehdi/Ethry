using Godot;

/// <summary>
/// Crafting UI — reads layout from scenes/ui/crafting_ui.tscn.
/// All visual style comes from the cute_fantasy_ui asset pack.
/// </summary>
public partial class CraftingUI : CanvasLayer
{
	// ── Scene nodes ────────────────────────────────────────────────────────
	private PanelContainer _panel;
	private Control        _blurOverlay;
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
	private CraftingRecipe[] _recipes = CraftingRecipes.Table;

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

		// Blur backdrop (matches inventory / storage UI)
		_blurOverlay = new Control();
		_blurOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_blurOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		_blurOverlay.Visible = false;
		AddChild(_blurOverlay);
		MoveChild(_blurOverlay, 0); // ensure behind the panel
		var bbc = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport };
		_blurOverlay.AddChild(bbc);
		var dim = new ColorRect();
		dim.Color = new Color(0, 0, 0, 0.4f);
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		const string CraftBlurMatPath = "res://shaders/blur_material.tres";
		if (FileAccess.FileExists(CraftBlurMatPath))
			dim.Material = GD.Load<ShaderMaterial>(CraftBlurMatPath);
		_blurOverlay.AddChild(dim);

		// Bitmap pixel font
		var font = new FontFile();
		font.LoadBitmapFont(FontPath);
		var theme = new Theme();
		theme.DefaultFont = font;
		_panel.Theme = theme;

		// Note: do NOT apply the textured GreenBtn region here — that sprite has
		// "SAVE" baked into the pixel art and would bleed through the "CRAFT" label.
		// The scene's StyleBox_CraftBtn (flat green) is used instead.
	}

	// ── Public API ─────────────────────────────────────────────────────────
	public void Open(CraftingRecipe[] recipes = null, string title = null)
	{
		if (recipes != null) _recipes = recipes;
		if (title != null)   _titleLabel.Text = title;
		_isOpen          = true;
		_panel.Visible   = true;
		if (_blurOverlay != null) _blurOverlay.Visible = true;
		_selectedIndex   = 0;
		GetTree().Paused = true;
		AudioManager.Instance?.PlaySfxFlat("ui_click");
		Refresh();
	}

	public void Close()
	{
		_isOpen          = false;
		_panel.Visible   = false;
		if (_blurOverlay != null) _blurOverlay.Visible = false;
		GetTree().Paused = false;
		AudioManager.Instance?.PlaySfxFlat("ui_click");
	}

	// ── Input ──────────────────────────────────────────────────────────────
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isOpen) return;

		if (@event.IsActionPressed("ui_down"))
		{
			_selectedIndex = Mathf.Min(_selectedIndex + 1, _recipes.Length - 1);
			AudioManager.Instance?.PlaySfxFlat("ui_navigate");
			Refresh();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_up"))
		{
			_selectedIndex = Mathf.Max(_selectedIndex - 1, 0);
			AudioManager.Instance?.PlaySfxFlat("ui_navigate");
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
			row.CustomMinimumSize = new Vector2(0, 56);
			row.TextureFilter     = CanvasItem.TextureFilterEnum.Nearest;
			row.AddThemeStyleboxOverride("panel", MakeRecipeStyle(selected));

			var hbox = new HBoxContainer();
			hbox.AddThemeConstantOverride("separation", 12);
			row.AddChild(hbox);

			var icon = new TextureRect();
			icon.CustomMinimumSize = new Vector2(22, 22);
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
			nameLabel.AddThemeFontSizeOverride("font_size", 16);
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
		string desc  = resultData?.Description ?? "";
		int    dmg   = resultData?.WeaponDamage ?? 0;
		int    arm   = resultData?.ArmorRating  ?? 0;
		string stats = "";
		if (dmg > 0) stats += $"\nDamage: {dmg}";
		if (arm > 0) stats += $"\nDefence: +{arm}";
		_resultDesc.Text = desc + stats;

		// Rebuild ingredient list
		foreach (Node child in _ingredientList.GetChildren()) child.QueueFree();

		foreach (var (ingId, amount) in sel.Ingredients)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			row.CustomMinimumSize = new Vector2(0, 44);

			var ingIcon = new TextureRect();
			ingIcon.CustomMinimumSize = new Vector2(52, 52);
			ingIcon.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
			ingIcon.StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered;
			ingIcon.TextureFilter     = CanvasItem.TextureFilterEnum.Nearest;
			ingIcon.Texture           = GetIconTexture(ingId);
			ingIcon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			row.AddChild(ingIcon);

			int  have   = Inventory.Instance.GetCount(ingId);
			bool enough = have >= amount;
			var  lbl    = new Label();
			lbl.Text = $"{GetItemName(ingId)}    {have} / {amount}";
			lbl.AddThemeColorOverride("font_color", enough ? IngredOk : IngredBad);
			lbl.AddThemeFontSizeOverride("font_size", 16);
			lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			lbl.SizeFlagsVertical   = Control.SizeFlags.ShrinkCenter;
			row.AddChild(lbl);

			var statusIcon = new TextureRect();
			statusIcon.CustomMinimumSize = new Vector2(22, 22);
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
			AudioManager.Instance?.PlaySfx("craft_success");
			NotificationManager.Instance?.ShowCraftSuccess(GetItemName(recipe.Result));
			Refresh();
		}
	}

	// ── Helpers ────────────────────────────────────────────────────────────
	private StyleBoxFlat MakeRecipeStyle(bool selected)
	{
		// Flat wood-toned card. Selected gets a lighter fill + accent border.
		var sb = new StyleBoxFlat
		{
			BgColor = selected
				? new Color(0.94f, 0.78f, 0.52f, 1f)   // warm cream
				: new Color(0.74f, 0.55f, 0.32f, 1f),  // wood tan
			BorderColor = selected
				? new Color(0.95f, 0.62f, 0.18f, 1f)   // gold accent
				: new Color(0.36f, 0.20f, 0.08f, 1f),  // dark wood
			BorderWidthLeft   = selected ? 3 : 2,
			BorderWidthTop    = selected ? 3 : 2,
			BorderWidthRight  = selected ? 3 : 2,
			BorderWidthBottom = selected ? 3 : 2,
			CornerRadiusTopLeft     = 6,
			CornerRadiusTopRight    = 6,
			CornerRadiusBottomLeft  = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft   = 10f,
			ContentMarginRight  = 10f,
			ContentMarginTop    = 6f,
			ContentMarginBottom = 6f,
		};
		return sb;
	}

	private Texture2D GetCachedTexture(string path)
	{
		if (!_textureCache.TryGetValue(path, out var tex))
			_textureCache[path] = tex = GD.Load<Texture2D>(path);
		return tex;
	}

	// ── ItemDatabase helpers ───────────────────────────────────────────────
	private static ItemData GetItemData(string id)
		=> ItemDatabase.Instance?.Get(id);

	private static string GetItemName(string id)
	{
		var data = GetItemData(id);
		return data != null ? data.DisplayName : id;
	}

	private Texture2D GetIconTexture(string id)
	{
		var data = GetItemData(id);
		return data?.Icon;
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
