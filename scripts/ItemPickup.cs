using Godot;

/// <summary>
/// Dropped item pickup in the world.
/// Uses ItemRegistry to automatically determine sprite from any ItemType.
/// </summary>
public partial class ItemPickup : Area2D
{
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

	// ── Private ────────────────────────────────────────────────────────────
	private void ApplySprite()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");

		string path = ItemRegistry.GetIconTexturePath(Type);
		if (!_texCache.TryGetValue(path, out var tex))
			_texCache[path] = tex = GD.Load<Texture2D>(path);

		var atlas   = new AtlasTexture();
		atlas.Atlas  = tex;
		atlas.Region = ItemRegistry.GetIconRegion(Type);

		sprite.Texture         = atlas;
		sprite.TextureFilter   = CanvasItem.TextureFilterEnum.Nearest;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player")) return;
		Inventory.Instance.AddItem(Type, Amount);
		QueueFree();
	}
}
