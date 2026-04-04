using Godot;

/// <summary>
/// Skeleton enemy — tougher than slime, faster, hits harder.
/// Supports directional animations (up, down, right with FlipH for left).
/// Drops bones and iron ore on death.
/// </summary>
public partial class Skeleton : CharacterBody2D
{
	[Export] public int   Health         = 6;
	[Export] public float Speed          = 70f;
	[Export] public float AttackRange    = 48f;
	[Export] public float WanderTime     = 2.5f;
	[Export] public int   AttackDamage   = 2;
	[Export] public float KnockbackForce = 180f;

	private AnimatedSprite2D _sprite;
	private Area2D           _hurtBox;
	private Area2D           _hitBox;
	private CollisionShape2D _hitBoxShape;
	private Area2D           _sight;
	private Sprite2D         _lifeBarFull;
	private int              _maxHealth;
	private float            _wanderTimer;
	private Vector2          _wanderDirection;
	private bool             _isDead;
	private bool             _isAttacking;
	private bool             _isKnockedBack;
	private float            _knockbackTimer;
	private Node2D           _target;
	private Vector2          _lastDirection = Vector2.Down;

	public override void _Ready()
	{
		_sprite       = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_hurtBox      = GetNode<Area2D>("HurtBox");
		_hitBox       = GetNode<Area2D>("HitBox");
		_hitBoxShape  = _hitBox.GetNode<CollisionShape2D>("CollisionShape2D");
		_sight        = GetNode<Area2D>("Sight");

		_hurtBox.AreaEntered      += OnHurtBoxAreaEntered;
		_hitBox.BodyEntered       += OnHitBoxBodyEntered;
		_hitBoxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
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
		if (_isDead) return;

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

		if (_isAttacking) return;

		if (_target != null)
		{
			float dist = GlobalPosition.DistanceTo(_target.GlobalPosition);
			Vector2 dir = (_target.GlobalPosition - GlobalPosition).Normalized();
			_lastDirection = dir;

			if (dist <= AttackRange)
			{
				StartAttack();
			}
			else
			{
				Velocity = dir * Speed;
				PlayAnimation("idle");
				MoveAndSlide();
			}
		}
		else
		{
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

		Velocity = _wanderDirection * Speed * 0.4f;
		if (Velocity != Vector2.Zero)
			_lastDirection = _wanderDirection;
		
		PlayAnimation("idle");
		MoveAndSlide();
	}

	private void PickRandomDirection()
	{
		float angle      = (float)GD.RandRange(0, Mathf.Tau);
		_wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
	}

	private void PlayAnimation(string action)
	{
		UpdateSpriteFlip();
		string dirName = GetDirectionName();
		
		// Map 'down' to base 'idle' or 'attack' if specific down anims don't follow naming convention
		// Based on .tscn: attack_down, attack_right, attack_up, idle, idle_right, idle_up
		string animName = action;
		if (dirName != "down")
		{
			animName = $"{action}_{dirName}";
		}
		
		if (_sprite.Animation != animName)
			_sprite.Play(animName);
	}

	private string GetDirectionName()
	{
		if (Mathf.Abs(_lastDirection.X) > Mathf.Abs(_lastDirection.Y))
			return "right";
		return _lastDirection.Y < 0 ? "up" : "down";
	}

	private void UpdateSpriteFlip()
	{
		if (_lastDirection.X < 0)
			_sprite.FlipH = true;
		else if (_lastDirection.X > 0)
			_sprite.FlipH = false;
	}

	private void StartAttack()
	{
		_isAttacking = true;
		Velocity     = Vector2.Zero;
		_sprite.Stop();

		// Play directional attack
		UpdateSpriteFlip();
		string dir = GetDirectionName();
		string anim = $"attack_{dir}";
		_sprite.Play(anim);
	}

	public void TakeDamage(int damage, Vector2 knockbackDir)
	{
		if (_isDead) return;

		Health -= damage;
		UpdateLifeBar();
		FlashHit();

		_isKnockedBack  = true;
		_isAttacking    = false;
		_knockbackTimer = 0.2f;
		Velocity        = knockbackDir.Normalized() * KnockbackForce;

		if (Health <= 0)
			Die();
	}

	private void FlashHit()
	{
		_sprite.Modulate = new Color(1, 0.3f, 0.3f);
		GetTree().CreateTimer(0.15).Timeout += () => _sprite.Modulate = Colors.White;
	}

	private void UpdateLifeBar()
	{
		float ratio          = Mathf.Clamp((float)Health / _maxHealth, 0f, 1f);
		_lifeBarFull.Scale   = new Vector2(ratio, 1f);
	}

	private void Die()
	{
		_isDead  = true;
		Velocity = Vector2.Zero;
		_hitBoxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_hurtBox.SetDeferred("monitoring",  false);
		_hurtBox.SetDeferred("monitorable", false);
		_sprite.Play("die");
		DropLoot();
	}

	private void DropLoot()
	{
		var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");

		// Always drop 1-2 bones
		SpawnDrop(pickupScene, ItemType.Bone, (int)GD.RandRange(1, 3));

		// 40% chance to drop iron ore
		if (GD.Randf() < 0.4f)
			SpawnDrop(pickupScene, ItemType.IronOre, 1);

		// 20% chance to drop a crystal
		if (GD.Randf() < 0.2f)
			SpawnDrop(pickupScene, ItemType.Crystal, 1);
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

	private void OnHitBoxBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
			player.TakeDamage(AttackDamage, knockDir);
		}
	}

	private void OnAnimationFinished()
	{
		if (_isDead)
		{
			CallDeferred(MethodName.QueueFree);
			return;
		}

		if (_isAttacking)
		{
			if (_target is Player player &&
			    GlobalPosition.DistanceTo(_target.GlobalPosition) <= AttackRange * 1.5f)
			{
				Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
				player.TakeDamage(AttackDamage, knockDir);
			}
			_isAttacking = false;
		}
	}
}
