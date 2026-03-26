using Godot;

public partial class Slime : CharacterBody2D
{
	[Export] public int Health = 3;
	[Export] public float Speed = 30f;
	[Export] public float AttackRange = 64f;
	[Export] public float WanderTime = 2f;
	[Export] public int AttackDamage = 1;
	[Export] public float KnockbackForce = 150f;

	private AnimatedSprite2D _sprite;
	private Area2D _hurtBox;
	private Area2D _hitBox;
	private CollisionShape2D _hitBoxShape;
	private Area2D _sight;
	private Sprite2D _lifeBarFull;
	private int _maxHealth;
	private float _wanderTimer;
	private Vector2 _wanderDirection;
	private bool _isDead;
	private bool _isAttacking;
	private bool _isKnockedBack;
	private float _knockbackTimer;
	private Node2D _target;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_hurtBox = GetNode<Area2D>("HurtBox");
		_hitBox = GetNode<Area2D>("HitBox");
		_hitBoxShape = _hitBox.GetNode<CollisionShape2D>("CollisionShape2D");
		_sight = GetNode<Area2D>("Sight");
		_hurtBox.AreaEntered += OnHurtBoxAreaEntered;
		_hitBox.BodyEntered += OnHitBoxBodyEntered;
		_hitBoxShape.Disabled = true;
		_sight.BodyEntered += OnSightBodyEntered;
		_sight.BodyExited += OnSightBodyExited;
		_sprite.AnimationFinished += OnAnimationFinished;
		_lifeBarFull = GetNode<Sprite2D>("LifeBar/Full");
		_maxHealth = Health;
		_wanderTimer = WanderTime;
		PickRandomDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
			return;



		if (_isKnockedBack)
		{
			_knockbackTimer -= (float)delta;
			MoveAndSlide();
			if (_knockbackTimer <= 0)
			{
				_isKnockedBack = false;
				Velocity = Vector2.Zero;
			}
			return;
		}

		if (_isAttacking)
			return;

		if (_target != null)
		{
			float distance = GlobalPosition.DistanceTo(_target.GlobalPosition);

			if (distance <= AttackRange)
			{
				_isAttacking = true;
				Velocity = Vector2.Zero;
				_sprite.Stop();
				_sprite.Play("attack");
			}
			else
			{
				Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
				Velocity = direction * Speed;
				_sprite.FlipH = direction.X < 0;
				_sprite.Play("idle");
				MoveAndSlide();
			}
		}
		else
		{
			Wander((float)delta);
		}
	}

	private void OnSightBodyEntered(Node2D body)
	{
		if (body.IsInGroup("player"))
			_target = body;
	}

	private void OnSightBodyExited(Node2D body)
	{
		if (body == _target)
			_target = null;
	}

	private void Wander(float delta)
	{
		_wanderTimer -= delta;
		if (_wanderTimer <= 0)
		{
			PickRandomDirection();
			_wanderTimer = WanderTime;
		}

		Velocity = _wanderDirection * Speed * 0.5f;
		_sprite.FlipH = _wanderDirection.X < 0;
		_sprite.Play("idle");
		MoveAndSlide();
	}

	private void PickRandomDirection()
	{
		float angle = (float)GD.RandRange(0, Mathf.Tau);
		_wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
	}

	public void TakeDamage(int damage, Vector2 knockbackDirection)
	{
		if (_isDead)
			return;

		Health -= damage;
		UpdateLifeBar();
		FlashHit();

		_isKnockedBack = true;
		_isAttacking = false;
		_knockbackTimer = 0.2f;
		Velocity = knockbackDirection.Normalized() * KnockbackForce;

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
		float ratio = Mathf.Clamp((float)Health / _maxHealth, 0f, 1f);
		_lifeBarFull.Scale = new Vector2(ratio, 1f);
	}

	private void DisableHitBox()
	{
		_hitBoxShape.SetDeferred("disabled", true);
	}

	private void Die()
	{
		_isDead = true;
		Velocity = Vector2.Zero;
		DisableHitBox();
		_hurtBox.SetDeferred("monitoring", false);
		_hurtBox.SetDeferred("monitorable", false);
		_sprite.Play("die");
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
			if (_target != null && _target is Player player)
			{
				if (GlobalPosition.DistanceTo(_target.GlobalPosition) <= AttackRange * 1.5f)
				{
					Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
					player.TakeDamage(AttackDamage, knockDir);
				}
			}
			_isAttacking = false;
		}
	}
}
