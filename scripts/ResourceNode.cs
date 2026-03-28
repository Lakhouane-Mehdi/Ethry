using Godot;

/// <summary>
/// A world object (tree, rock, etc.) that takes damage from player attacks
/// and drops item pickups when destroyed.
/// Attach to a Node2D scene that has:
///   - Sprite2D child
///   - StaticBody2D child (with CollisionShape2D) for physics blocking
///   - HurtBox (Area2D child, with CollisionShape2D) for hit detection
/// </summary>
public partial class ResourceNode : Node2D
{
	[Export] public int Health = 3;
	[Export] public ItemType DropType = ItemType.Wood;
	[Export] public int DropMin = 1;
	[Export] public int DropMax = 3;
	[Export] public PackedScene ItemPickupScene;

	private Sprite2D _sprite;
	private int _maxHealth;

	public override void _Ready()
	{
		_maxHealth = Health;
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		var hurtBox = GetNode<Area2D>("HurtBox");
		hurtBox.CollisionLayer = 0;
		hurtBox.CollisionMask = 2; // detects the player's HitBox (layer 2)
		hurtBox.AreaEntered += OnHurtBoxAreaEntered;
	}

	private void OnHurtBoxAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player player)
			TakeDamage(player.AttackDamage);
	}

	public void TakeDamage(int damage)
	{
		if (Health <= 0)
			return;

		Health -= damage;
		FlashHit();

		if (Health <= 0)
			Harvest();
	}

	private void FlashHit()
	{
		if (_sprite == null)
			return;

		_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
		GetTree().CreateTimer(0.12).Timeout += () =>
		{
			if (IsInstanceValid(_sprite))
				_sprite.Modulate = Colors.White;
		};
	}

	private void Harvest()
	{
		if (ItemPickupScene != null)
		{
			int amount = (int)GD.RandRange(DropMin, DropMax + 1);
			var pickup = ItemPickupScene.Instantiate<ItemPickup>();
			pickup.Type = DropType;
			pickup.Amount = amount;
			pickup.GlobalPosition = GlobalPosition;
			GetParent().CallDeferred(Node.MethodName.AddChild, pickup);
		}

		QueueFree();
	}
}
