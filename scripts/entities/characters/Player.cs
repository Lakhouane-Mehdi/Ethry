using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 100f;
	[Export] public float RunSpeedMultiplier = 1.8f;
	[Export] public float Acceleration = 800f;
	[Export] public float Friction = 1000f;
	[Export] public float AttackCooldown = 0.4f;
	public int AttackDamage => Equipment.Instance?.GetAttackDamage() ?? 1;
	[Export] public int MaxHealth = 6;
	[Export] public int Health = 6;
	[Export] public float KnockbackForce = 120f;

	private AnimatedSprite2D _sprite;
	private Area2D _hitbox;
	private Area2D _hurtBox;
	private CollisionShape2D _hitboxShape;
	private Camera2D _camera;
	private Vector2 _lastDirection = Vector2.Down;
	private bool _isAttacking;
	private bool _isDead;
	private bool _isKnockedBack;
	private float _knockbackTimer;
	private float _attackTimer;

	/// <summary>Briefly shakes the camera for impact/juice.</summary>
	public void ShakeCamera(float duration = 0.15f, float intensity = 3.0f)
	{
		if (!IsInstanceValid(_camera)) return;

		var tween = CreateTween();
		int shakes = 5;
		float step = duration / shakes;

		for (int i = 0; i < shakes; i++)
		{
			var offset = new Vector2(
				(float)GD.RandRange(-intensity, intensity),
				(float)GD.RandRange(-intensity, intensity)
			);
			tween.TweenProperty(_camera, "offset", offset, step).SetTrans(Tween.TransitionType.Sine);
		}

		tween.TweenProperty(_camera, "offset", Vector2.Zero, step);
	}

	public override void _Ready()
	{
		AddToGroup("player");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.AnimationFinished += OnAnimationFinished;

		_hitbox = GetNode<Area2D>("HitBox");
		_hitboxShape = _hitbox.GetNode<CollisionShape2D>("CollisionShape2D");
		_hitbox.BodyEntered += OnHitBoxBodyEntered;
		_hitboxShape.Disabled = true;

		_hurtBox = GetNode<Area2D>("HurtBox");
		_hurtBox.BodyEntered += OnHurtBoxBodyEntered;

		_camera = GetNode<Camera2D>("Camera2D");
		SetCameraLimits();
	}

	private void SetCameraLimits()
	{
		var tileMap = GetParent().GetNodeOrNull<TileMapLayer>("terrain");
		if (tileMap == null)
			return;

		Rect2I usedRect = tileMap.GetUsedRect();
		int tileSize = tileMap.TileSet.TileSize.X;
		Vector2 scale = tileMap.Scale;

		float cellSize = tileSize * scale.X;

		_camera.LimitLeft = (int)(usedRect.Position.X * cellSize);
		_camera.LimitTop = (int)(usedRect.Position.Y * cellSize);
		_camera.LimitRight = (int)((usedRect.Position.X + usedRect.Size.X) * cellSize);
		_camera.LimitBottom = (int)((usedRect.Position.Y + usedRect.Size.Y) * cellSize);
		_camera.LimitSmoothed = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_attackTimer > 0)
			_attackTimer -= (float)delta;

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
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 input = Input.GetVector("left", "right", "up", "down");
		bool isRunning = Input.IsActionPressed("running");

		if (Input.IsActionJustPressed("attack") && _attackTimer <= 0)
		{
			Attack();
			return;
		}

		float dt = (float)delta;

		if (input != Vector2.Zero)
		{
			float targetSpeed = isRunning ? Speed * RunSpeedMultiplier : Speed;
			Vector2 targetVelocity = input * targetSpeed;
			Velocity = Velocity.MoveToward(targetVelocity, Acceleration * dt);
			_lastDirection = input;
			PlayAnimation("run");
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Friction * dt);
			PlayAnimation("idle");
		}

		MoveAndSlide();

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

		// Safety timeout: force reset attacking state in case animation signal is missed
		GetTree().CreateTimer(0.6).Timeout += () =>
		{
			if (_isAttacking)
			{
				_isAttacking = false;
				DisableHitBox();
			}
		};
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
		_hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
	}

	private void DisableHitBox()
	{
		_hitboxShape.SetDeferred("disabled", true);
	}

	private void OnHitBoxBodyEntered(Node2D body)
	{
		if (body == this) return;

		Vector2 knockDir = GlobalPosition.DirectionTo(body.GlobalPosition);

		if (body is Slime slime)
			slime.TakeDamage(AttackDamage, knockDir);
		else if (body is Skeleton skeleton)
			skeleton.TakeDamage(AttackDamage, knockDir);
		else if (body is Bombshroom bombshroom)
			bombshroom.TakeDamage(AttackDamage, knockDir);
		else if (body is ResourceNode resource)
			resource.TakeDamage(AttackDamage);
	}

	private void OnHurtBoxBodyEntered(Node2D body)
	{
		Vector2 knockDir;
		if (body is Slime slime)
		{
			knockDir = (GlobalPosition - slime.GlobalPosition).Normalized();
			TakeDamage(slime.AttackDamage, knockDir);
		}
		else if (body is Skeleton skeleton)
		{
			knockDir = (GlobalPosition - skeleton.GlobalPosition).Normalized();
			TakeDamage(skeleton.AttackDamage, knockDir);
		}
		else if (body is Bombshroom bombshroom)
		{
			knockDir = (GlobalPosition - bombshroom.GlobalPosition).Normalized();
			TakeDamage(bombshroom.ExplosionDamage, knockDir);
		}
	}

	public void TakeDamage(int damage, Vector2 knockbackDirection)
	{
		if (_isDead || _isKnockedBack)
			return;

		// Reduce incoming damage by player's current total defence
		int defence = Equipment.Instance?.GetTotalDefence() ?? 0;
		int effective = Mathf.Max(1, damage - defence);
		Health -= effective;

		_sprite.Modulate = new Color(1, 0.3f, 0.3f);
		GetTree().CreateTimer(0.15).Timeout += () => _sprite.Modulate = Colors.White;

		_isKnockedBack = true;
		_knockbackTimer = 0.15f;
		Velocity = knockbackDirection * KnockbackForce;

		if (Health <= 0)
			Die();
	}

	/// <summary>Restores HP, capped at MaxHealth.</summary>
	public void Heal(int amount)
	{
		Health = Mathf.Min(Health + amount, MaxHealth);
	}

	/// <summary>Consumes a food item and heals the player.
	/// Returns false if already at max HP or item not in inventory.</summary>
	public bool UseConsumable(ItemType type) => UseConsumable(type.ToString());

	/// <summary>Consumes a food item by string ID.</summary>
	public bool UseConsumable(string itemId)
	{
		var data = ItemDatabase.Instance?.Get(itemId);
		int heal = data?.HealAmount ?? 0;
		if (heal <= 0 && System.Enum.TryParse<ItemType>(itemId, out var t))
			heal = ItemRegistry.GetHealAmount(t);

		if (heal <= 0)           return false;
		if (Health >= MaxHealth) return false;
		if (!Inventory.Instance.RemoveItem(itemId)) return false;
		Heal(heal);
		return true;
	}

	private void Die()
	{
		_isDead = true;
		Velocity = Vector2.Zero;
		_hurtBox.SetDeferred("monitoring", false);
		_hurtBox.SetDeferred("monitorable", false);
		_hitboxShape.SetDeferred("disabled", true);
		_sprite.Play("dying");
		SetPhysicsProcess(false);
	}

	private void Respawn()
	{
		// Stardew-style: lose some gold, wake up with half health
		int goldPenalty = Mathf.Min(PlayerData.Instance.Gold, PlayerData.Instance.Gold / 4 + 50);
		if (goldPenalty > 0)
			PlayerData.Instance.SpendGold(goldPenalty);

		Health = Mathf.Max(MaxHealth / 2, 1);
		_isDead = false;
		_isKnockedBack = false;
		_isAttacking = false;
		_sprite.Modulate = Colors.White;
		SetPhysicsProcess(true);
		_hurtBox.SetDeferred("monitoring", true);
		_hurtBox.SetDeferred("monitorable", true);

		// Advance to next day
		DaySystem.Instance?.AdvanceDay();

		NotificationManager.Instance?.ShowWarning($"You passed out... -{goldPenalty}g");
	}

	private void OnAnimationFinished()
	{
		if (_isDead)
		{
			Respawn();
			return;
		}

		if (_isAttacking)
		{
			_isAttacking = false;
			DisableHitBox();
		}
	}
}
