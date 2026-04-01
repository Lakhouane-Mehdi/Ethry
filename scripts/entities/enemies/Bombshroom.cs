using Godot;

/// <summary>
/// Bombshroom enemy — charges at the player and explodes on contact.
/// Deals area damage when it detonates. Drops herbs and mushrooms.
/// </summary>
public partial class Bombshroom : CharacterBody2D
{
	[Export] public int   Health          = 4;
	[Export] public float Speed           = 40f;
	[Export] public float ChargeSpeed     = 120f;
	[Export] public float DetonateRange   = 32f;
	[Export] public int   ExplosionDamage = 3;
	[Export] public float ExplosionRadius = 60f;
	[Export] public float WanderTime      = 3f;
	[Export] public float KnockbackForce  = 200f;

	private AnimatedSprite2D _sprite;
	private Area2D           _hurtBox;
	private Area2D           _sight;
	private Sprite2D         _lifeBarFull;
	private int              _maxHealth;
	private float            _wanderTimer;
	private Vector2          _wanderDirection;
	private bool             _isDead;
	private bool             _isCharging;
	private bool             _isExploding;
	private bool             _isKnockedBack;
	private float            _knockbackTimer;
	private Node2D           _target;

	public override void _Ready()
	{
		_sprite      = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_hurtBox     = GetNode<Area2D>("HurtBox");
		_sight       = GetNode<Area2D>("Sight");

		_hurtBox.AreaEntered      += OnHurtBoxAreaEntered;
		_sight.BodyEntered        += OnSightBodyEntered;
		_sight.BodyExited         += OnSightBodyExited;
		_sprite.AnimationFinished += OnAnimationFinished;

		_lifeBarFull = GetNode<Sprite2D>("LifeBar/Full");
		_maxHealth   = Health;
		_wanderTimer = WanderTime;
		PickRandomDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || _isExploding) return;

		if (_isKnockedBack)
		{
			_knockbackTimer -= (float)delta;
			MoveAndSlide();
			if (_knockbackTimer <= 0)
			{
				_isKnockedBack = false;
				Velocity       = Vector2.Zero;
			}
			return;
		}

		if (_target != null)
		{
			float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);

			if (dist <= DetonateRange)
			{
				Explode();
				return;
			}

			// Charge towards the player
			_isCharging = true;
			Vector2 dir = (_target.GlobalPosition - GlobalPosition).Normalized();
			Velocity    = dir * ChargeSpeed;
			_sprite.FlipH = dir.X < 0;

			// Flash red while charging to warn the player
			float pulse = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 150f));
			_sprite.Modulate = new Color(1, 1 - pulse * 0.4f, 1 - pulse * 0.4f);

			_sprite.Play("attack");
			MoveAndSlide();
		}
		else
		{
			_isCharging      = false;
			_sprite.Modulate = Colors.White;
			Wander((float)delta);
		}
	}

	private void Wander(float delta)
	{
		_wanderTimer -= delta;
		if (_wanderTimer <= 0)
		{
			PickRandomDirection();
			_wanderTimer = WanderTime;
		}

		Velocity      = _wanderDirection * Speed * 0.4f;
		_sprite.FlipH = _wanderDirection.X < 0;
		_sprite.Play("idle");
		MoveAndSlide();
	}

	private void PickRandomDirection()
	{
		float angle      = (float)GD.RandRange(0, Mathf.Tau);
		_wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
	}

	public void TakeDamage(int damage, Vector2 knockbackDir)
	{
		if (_isDead) return;

		Health -= damage;
		UpdateLifeBar();
		FlashHit();

		_isKnockedBack  = true;
		_knockbackTimer = 0.2f;
		Velocity        = knockbackDir.Normalized() * KnockbackForce;

		if (Health <= 0)
			Explode(); // Explodes on death instead of just dying
	}

	private void Explode()
	{
		if (_isExploding) return;
		_isExploding     = true;
		Velocity         = Vector2.Zero;
		_sprite.Modulate = Colors.White;
		_sprite.Play("die");

		// Deal area damage to the player if in range
		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player != null)
		{
			float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (dist <= ExplosionRadius)
			{
				Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
				player.TakeDamage(ExplosionDamage, knockDir);
			}
		}

		DropLoot();
	}

	private void FlashHit()
	{
		_sprite.Modulate = new Color(1, 0.3f, 0.3f);
		GetTree().CreateTimer(0.15).Timeout += () =>
		{
			if (!_isExploding) _sprite.Modulate = Colors.White;
		};
	}

	private void UpdateLifeBar()
	{
		float ratio        = Mathf.Clamp((float)Health / _maxHealth, 0f, 1f);
		_lifeBarFull.Scale = new Vector2(ratio, 1f);
	}

	private void DropLoot()
	{
		var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");

		// Drop mushroom + herbs
		SpawnDrop(pickupScene, ItemType.Mushroom, (int)GD.RandRange(1, 3));

		if (GD.Randf() < 0.5f)
			SpawnDrop(pickupScene, ItemType.Herb, (int)GD.RandRange(1, 3));

		if (GD.Randf() < 0.15f)
			SpawnDrop(pickupScene, ItemType.Coal, 1);
	}

	private void SpawnDrop(PackedScene scene, ItemType type, int amount)
	{
		var pickup    = scene.Instantiate<ItemPickup>();
		pickup.Type   = type;
		pickup.Amount = amount;

		float angle  = (float)GD.RandRange(0, Mathf.Tau);
		float radius = (float)GD.RandRange(15, 35);
		pickup.Position = GlobalPosition + new Vector2(
			Mathf.Cos(angle) * radius,
			Mathf.Sin(angle) * radius
		);

		GetTree().CurrentScene.AddChild(pickup);
	}

	private void OnSightBodyEntered(Node2D body)
	{
		if (body.IsInGroup("player")) _target = body;
	}

	private void OnSightBodyExited(Node2D body)
	{
		if (body == _target) _target = null;
	}

	private void OnHurtBoxAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player player)
		{
			Vector2 knockDir = (GlobalPosition - player.GlobalPosition).Normalized();
			TakeDamage(player.AttackDamage, knockDir);
		}
	}

	private void OnAnimationFinished()
	{
		if (_isExploding)
		{
			_isDead = true;
			CallDeferred(MethodName.QueueFree);
		}
	}
}
