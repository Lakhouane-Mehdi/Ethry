using Godot;
using FSM;
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
	private FSM.StateMachine _stateMachine;

	/// <summary>The ID of the currently equipped tool/weapon in the Weapon slot.</summary>
	public string EquippedToolId => Equipment.Instance?.GetSlotId(EquipSlot.Weapon);

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
		ZIndex = 1;
		ZIndex = 1;
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

		// Initialize State Machine
		_stateMachine = GetNode<FSM.StateMachine>("StateMachine");
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
		_stateMachine?.PhysicsUpdate(delta);
	}

	public override void _Process(double delta)
	{
		_stateMachine?.Update(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		_stateMachine?.HandleInput(@event);
	}

	public void SetLastDirection(Vector2 direction) => _lastDirection = direction;

	public void PlayAnimation(string action)
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

	public void UpdateSpriteFlip()
	{
		if (_lastDirection.X < 0)
			_sprite.FlipH = true;
		else if (_lastDirection.X > 0)
			_sprite.FlipH = false;
	}

	public void EnableHitBox()
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

	/// <summary>Checks if there is a resource or enemy in the attack range.</summary>
	public bool IsTargetInFront()
	{
		if (_hitboxShape == null) return false;

		var space = GetWorld2D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters2D();
		query.Shape = _hitboxShape.Shape;
		
		// Determine the future global position of the hitbox
		string dir = GetDirectionName();
		Vector2 offset = dir switch
		{
			"up" => new Vector2(-0.5f, -7.5f),
			"down" => new Vector2(0.5f, 7.5f),
			_ => _sprite.FlipH ? new Vector2(-7.5f, -0.5f) : new Vector2(7.5f, -0.5f)
		};
		
		query.Transform = new Transform2D(0, GlobalPosition + offset * Scale);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.CollisionMask = 1; // Default layer where enemies/resources usually live

		var results = space.IntersectShape(query);
		foreach (var res in results)
		{
			var collider = res["collider"].As<Node>();
			if (collider.GetParent() == this) continue; // Don't hit ourselves
			return true;
		}
		return false;
	}

	public void DisableHitBox()
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
			resource.TakeDamage(AttackDamage, this); // 'this' allows node to check tool requirement
	}

	public void DisableHurtBox() => _hurtBox.SetDeferred("monitoring", false);
	public void EnableHurtBox() => _hurtBox.SetDeferred("monitoring", true);

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
		// Don't take damage if already dead or in hitstate
		if (_stateMachine.CurrentState is PlayerDeathState || _stateMachine.CurrentState is PlayerHurtState)
			return;

		// Reduce incoming damage by player's current total defence
		int defence = Equipment.Instance?.GetTotalDefence() ?? 0;
		int effective = Mathf.Max(1, damage - defence);
		Health -= effective;

		Velocity = knockbackDirection * KnockbackForce;

		if (Health <= 0)
			_stateMachine.TransitionTo("Death");
		else
			_stateMachine.TransitionTo("Hurt");
	}

	/// <summary>Restores HP, capped at MaxHealth.</summary>
	public void Heal(int amount)
	{
		Health = Mathf.Min(Health + amount, MaxHealth);
	}

	/// <summary>Consumes a food item by string ID.
	/// Returns false if already at max HP or item not in inventory.</summary>
	public bool UseConsumable(string itemId)
	{
		var data = ItemDatabase.Instance?.Get(itemId);
		int heal = data?.HealAmount ?? 0;

		if (heal <= 0)           return false;
		if (Health >= MaxHealth) return false;
		if (!Inventory.Instance.RemoveItem(itemId)) return false;
		Heal(heal);
		return true;
	}

	public void Respawn()
	{
		// Stardew-style: lose some gold, wake up with half health
		int goldPenalty = Mathf.Min(PlayerData.Instance.Gold, PlayerData.Instance.Gold / 4 + 50);
		if (goldPenalty > 0)
			PlayerData.Instance.SpendGold(goldPenalty);

		Health = Mathf.Max(MaxHealth / 2, 1);
		EnableHurtBox();

		// Advance to next day
		DaySystem.Instance?.AdvanceDay();

		NotificationManager.Instance?.ShowWarning($"You passed out... -{goldPenalty}g");
	}

	private void OnAnimationFinished()
	{
		// We can still use this signal if we need to sync with animations,
		// but the state machine handles most timing now.
	}

	/// <summary>World-space direction the player is facing for attacks/projectiles.</summary>
	public Vector2 GetAttackDirection()
	{
		string dir = GetDirectionName();
		return dir switch
		{
			"up"   => Vector2.Up,
			"down" => Vector2.Down,
			_      => _sprite.FlipH ? Vector2.Left : Vector2.Right,
		};
	}

	/// <summary>Spawns an arrow projectile in the player's facing direction.</summary>
	public void FireArrow()
	{
		var scene = GD.Load<PackedScene>("res://scenes/entities/arrow.tscn");
		if (scene == null) return;

		var arrow = scene.Instantiate<Arrow>();
		arrow.Direction = GetAttackDirection();
		arrow.Damage    = AttackDamage;
		arrow.GlobalPosition = GlobalPosition + arrow.Direction * 10f;
		GetParent().AddChild(arrow);
		AudioManager.Instance?.PlaySfx("player_attack");
	}
}
