using Godot;

public partial class ItemPickup : Area2D
{
	[Export] public ItemType Type = ItemType.Wood;
	[Export] public int Amount = 1;

	private float _bobTimer;
	private Vector2 _startPosition;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1;
		_startPosition = Position;
		BodyEntered += OnBodyEntered;
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
