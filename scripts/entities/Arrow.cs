using Godot;

/// <summary>
/// Simple projectile fired by the player's bow. Moves in a straight line,
/// damages the first enemy it touches, and despawns on hit or after a lifetime.
/// </summary>
public partial class Arrow : Area2D
{
	[Export] public float Speed    = 360f;
	[Export] public int   Damage   = 2;
	[Export] public float Lifetime = 1.2f;

	public Vector2 Direction = Vector2.Right;

	private float _age;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask  = 4; // enemies live on layer 4
		Monitoring     = true;
		BodyEntered   += OnBodyEntered;
		AreaEntered   += OnAreaEntered;

		Rotation = Direction.Angle();
	}

	public override void _PhysicsProcess(double delta)
	{
		Position += Direction * Speed * (float)delta;
		_age += (float)delta;
		if (_age >= Lifetime) QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("player")) return;
		TryDamage(body);
	}

	private void OnAreaEntered(Area2D area)
	{
		var parent = area.GetParent();
		if (parent != null) TryDamage(parent);
	}

	private void TryDamage(Node target)
	{
		Vector2 knock = Direction;
		bool hit = false;

		if (target is Slime slime)           { slime.TakeDamage(Damage, knock);           hit = true; }
		else if (target is Skeleton skel)    { skel.TakeDamage(Damage, knock);            hit = true; }
		else if (target is Bombshroom bomb)  { bomb.TakeDamage(Damage, knock);            hit = true; }
		else if (target is ResourceNode res) { res.TakeDamage(Damage, null);              hit = true; }

		if (hit) QueueFree();
	}
}
