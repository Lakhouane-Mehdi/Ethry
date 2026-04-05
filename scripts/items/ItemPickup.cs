using Godot;

/// <summary>
/// Dropped item pickup in the world.
/// Prefer editor-assigned ItemData via the Inspector.
/// Falls back to the legacy ItemType enum for code-spawned pickups
/// (e.g. ResourceNode) that have not yet been migrated.
/// </summary>
public partial class ItemPickup : Area2D
{
	// ── Editor-friendly path (ItemData .tres) ──────────────────────────
	[ExportGroup("Item (preferred)")]
	[Export] public ItemData Data;

	// ── Legacy enum path (backward compat) ────────────────────────────
	[ExportGroup("Item (legacy enum)")]
	[Export] public ItemType Type   = ItemType.Wood;
	[Export] public int      Amount = 1;

	private float   _bobTimer;
	private Vector2 _startPosition;

	private readonly System.Collections.Generic.Dictionary<string, Texture2D> _texCache = new();

	public override void _Ready()
	{
		SetDeferred(PropertyName.CollisionLayer, 0);
		SetDeferred(PropertyName.CollisionMask,  1);
		_startPosition = Position;
		BodyEntered   += OnBodyEntered;

		ApplySprite();
	}

	public override void _Process(double delta)
	{
		_bobTimer += (float)delta;
		Position   = _startPosition + new Vector2(0f, Mathf.Sin(_bobTimer * 3f) * 2f);
	}

	// ── Private ────────────────────────────────────────────────────────
	private void ApplySprite()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");

		// 1. Prefer ItemData icon (editor-assigned resource)
		if (Data != null && Data.Icon != null)
		{
			sprite.Texture = Data.Icon;
			sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			return;
		}

		// 2. Check Database for legacy 'Type' before falling back to registry
		var dbData = ItemDatabase.Instance?.Get(Type.ToString());
		if (dbData != null && dbData.Icon != null)
		{
			sprite.Texture = dbData.Icon;
			sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			return;
		}

		// 3. Final Fallback: legacy hardcoded ItemRegistry lookup
		string path = ItemRegistry.GetIconTexturePath(Type);
		if (!_texCache.TryGetValue(path, out var tex))
			_texCache[path] = tex = GD.Load<Texture2D>(path);

		var atlas   = new AtlasTexture();
		atlas.Atlas  = tex;
		atlas.Region = ItemRegistry.GetIconRegion(Type);

		sprite.Texture       = atlas;
		sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player")) return;

		if (Data != null)
			Inventory.Instance.AddItem(Data, Amount);
		else
			Inventory.Instance.AddItem(Type, Amount);

		AudioManager.Instance?.PlaySfx("item_pickup");
		QueueFree();
	}
}
