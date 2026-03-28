using Godot;

public partial class ItemPickup : Area2D
{
	[Export] public ItemType Type = ItemType.Wood;
	[Export] public int Amount = 1;

	[ExportGroup("Item Textures")]
	[Export] public Texture2D WoodTexture;
	[Export] public Texture2D StoneTexture;
	[Export] public Texture2D HerbTexture;

	private float _bobTimer;
	private Vector2 _startPosition;

	public override void _Ready()
	{
		SetDeferred(PropertyName.CollisionLayer, 0);
		SetDeferred(PropertyName.CollisionMask, 1);
		_startPosition = Position;
		BodyEntered += OnBodyEntered;

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = Type switch
		{
			ItemType.Wood => WoodTexture,
			ItemType.Stone => StoneTexture,
			ItemType.Herb => HerbTexture,
			_ => null
		};
	}

	public override void _Process(double delta)
	{
		_bobTimer += (float)delta;
		Position = _startPosition + new Vector2(0f, Mathf.Sin(_bobTimer * 3f) * 2f);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player"))
			return;

		Inventory.Instance.AddItem(Type, Amount);
		QueueFree();
	}
}
