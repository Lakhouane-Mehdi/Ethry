using Godot;

/// <summary>
/// Dropped item pickup in the world.
/// Assign ItemData in the Inspector, or set ItemId for code-spawned drops.
/// </summary>
public partial class ItemPickup : Area2D
{
	[ExportGroup("Item")]
	[Export] public ItemData Data;
	[Export] public string   ItemId = "";
	[Export] public int      Amount = 1;

	[ExportGroup("Despawn")]
	/// <summary>Seconds before the pickup disappears. 0 = never.</summary>
	[Export] public float Lifetime = 120f;
	/// <summary>Seconds before despawn to start blinking as a warning.</summary>
	[Export] public float BlinkWarning = 10f;

	private float   _bobTimer;
	private float   _age;
	private Vector2 _startPosition;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		SetDeferred(PropertyName.CollisionLayer, 0);
		SetDeferred(PropertyName.CollisionMask,  1);
		_startPosition = Position;
		BodyEntered   += OnBodyEntered;

		// Resolve ItemId → Data when only an ID is provided
		if (Data == null && !string.IsNullOrEmpty(ItemId))
			Data = ItemDatabase.Instance?.Get(ItemId);

		ApplySprite();
	}

	public override void _Process(double delta)
	{
		_bobTimer += (float)delta;
		Position   = _startPosition + new Vector2(0f, Mathf.Sin(_bobTimer * 3f) * 2f);

		if (Lifetime > 0f)
		{
			_age += (float)delta;
			float remaining = Lifetime - _age;
			if (remaining <= 0f) { QueueFree(); return; }

			if (_sprite != null && remaining <= BlinkWarning)
			{
				// Blink faster as it nears despawn
				float speed = Mathf.Lerp(4f, 14f, 1f - remaining / BlinkWarning);
				float alpha = 0.35f + 0.65f * (Mathf.Sin(_age * speed) * 0.5f + 0.5f);
				_sprite.Modulate = new Color(1, 1, 1, alpha);
			}
		}
	}

	private void ApplySprite()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		if (Data?.Icon != null)
		{
			_sprite.Texture = Data.Icon;
			_sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player")) return;

		if (Data != null)
			Inventory.Instance.AddItem(Data, Amount);
		else if (!string.IsNullOrEmpty(ItemId))
			Inventory.Instance.AddItem(ItemId, Amount);

		AudioManager.Instance?.PlaySfx("item_pickup");
		QueueFree();
	}
}
