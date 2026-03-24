using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 100f;
	[Export] public float AttackCooldown = 0.4f;
	[Export] public int AttackDamage = 1;

	private AnimatedSprite2D _sprite;
	private Area2D _hitbox;
	private CollisionShape2D _hitboxShape;
	private Vector2 _lastDirection = Vector2.Down;
	private bool _isAttacking;
	private float _attackTimer;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.AnimationFinished += OnAnimationFinished;

		_hitbox = GetNode<Area2D>("HitBox");
		_hitboxShape = _hitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		_hitbox.BodyEntered += OnHitBoxBodyEntered;
		_hitboxShape.Disabled = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_attackTimer > 0)
			_attackTimer -= (float)delta;

		if (_isAttacking)
			return;

		Vector2 input = Input.GetVector("left", "right", "up", "down");

		if (Input.IsActionJustPressed("attack") && _attackTimer <= 0)
		{
			Attack();
			return;
		}

		Velocity = input * Speed;
		MoveAndSlide();

		if (input != Vector2.Zero)
		{
			_lastDirection = input;
			PlayAnimation("run");
		}
		else
		{
			PlayAnimation("idle");
		}

		UpdateSpriteFlip();
	}

	private void Attack()
	{
		_isAttacking = true;
		_attackTimer = AttackCooldown;
		Velocity = Vector2.Zero;
		PlayAnimation("attack");
		UpdateSpriteFlip();
		EnableHitBox();
	}

	private void PlayAnimation(string action)
	{
		string direction = GetDirectionName();
		string animName = $"{action}_{direction}";
		if (_sprite.Animation != animName)
			_sprite.Play(animName);
	}

	private string GetDirectionName()
	{
		// No left animations — use right + flip
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

	private void EnableHitBox()
	{
		string dir = GetDirectionName();
		Vector2 offset = dir switch
		{
			"up" => new Vector2(-0.5f, -7.5f),
			"down" => new Vector2(0.5f, 7.5f),
			_ => _sprite.FlipH ? new Vector2(-7.5f, -0.5f) : new Vector2(7.5f, -0.5f)
		};
		_hitbox.Position = offset;
		_hitboxShape.Disabled = false;
	}

	private void DisableHitBox()
	{
		_hitboxShape.Disabled = true;
	}

	private void OnHitBoxBodyEntered(Node2D body)
	{
		if (body == this)
			return;
		GD.Print($"Hit: {body.Name} for {AttackDamage} damage");
	}

	private void OnAnimationFinished()
	{
		if (_isAttacking)
		{
			_isAttacking = false;
			DisableHitBox();
		}
	}
}
