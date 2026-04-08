using Godot;
using FSM;

/// <summary>
/// Base class for all enemies in the game.
/// Handles common properties like health, targeting, and taking damage.
/// </summary>
public abstract partial class Enemy : CharacterBody2D
{
    [Export] public int Health = 3;
    [Export] public float Speed = 50f;
    [Export] public float AttackRange = 64f;
    [Export] public float WanderTime = 2f;
    [Export] public int AttackDamage = 1;
    [Export] public float KnockbackForce = 150f;

    protected AnimatedSprite2D _sprite;
    protected Area2D _hurtBox;
    protected Area2D _hitBox;
    protected CollisionShape2D _hitBoxShape;
    protected Area2D _sight;
    protected Sprite2D _lifeBarFull;
    protected int _maxHealth;
    protected Node2D _target;
    protected FSM.StateMachine _stateMachine;

    public Node2D Target => _target;
    public AnimatedSprite2D Sprite => _sprite;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _hurtBox = GetNode<Area2D>("HurtBox");
        _hitBox = GetNodeOrNull<Area2D>("HitBox");
        if (_hitBox != null)
            _hitBoxShape = _hitBox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _sight = GetNode<Area2D>("Sight");
        _lifeBarFull = GetNodeOrNull<Sprite2D>("LifeBar/Full");

        _maxHealth = Health;
        _stateMachine = GetNode<FSM.StateMachine>("StateMachine");

        // Common signal connections
        _hurtBox.AreaEntered += OnHurtBoxAreaEntered;
        if (_hitBox != null)
            _hitBox.BodyEntered += OnHitBoxBodyEntered;
        
        _sight.BodyEntered += OnSightBodyEntered;
        _sight.BodyExited += OnSightBodyExited;

        InitializeEnemy();
    }

    /// <summary>
    /// For subclasses to add their own initialization logic.
    /// </summary>
    protected virtual void InitializeEnemy() { }

    public override void _PhysicsProcess(double delta)
    {
        _stateMachine?.PhysicsUpdate(delta);
    }

    public override void _Process(double delta)
    {
        _stateMachine?.Update(delta);
    }

    protected virtual void OnSightBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
            _target = body;
    }

    protected virtual void OnSightBodyExited(Node2D body)
    {
        if (body == _target)
            _target = null;
    }

    protected virtual void OnHurtBoxAreaEntered(Area2D area)
    {
        if (area.GetParent() is Player player)
        {
            Vector2 knockDir = (GlobalPosition - player.GlobalPosition).Normalized();
            TakeDamage(player.AttackDamage, knockDir);
        }
    }

    protected virtual void OnHitBoxBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
            player.TakeDamage(AttackDamage, knockDir);
        }
    }

    public virtual void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        Health -= damage;
        UpdateLifeBar();
        FlashHit();
        
        Velocity = knockbackDirection * KnockbackForce;

        if (Health <= 0)
        {
            QuestManager.Instance?.ReportKill(GetType().Name);
            _stateMachine.TransitionTo("Death");
        }
        else
            _stateMachine.TransitionTo("Hurt");
    }

    protected void UpdateLifeBar()
    {
        if (_lifeBarFull == null) return;
        float ratio = Mathf.Clamp((float)Health / _maxHealth, 0f, 1f);
        _lifeBarFull.Scale = new Vector2(ratio, 1f);
    }

    protected void FlashHit()
    {
        if (_sprite == null) return;
        _sprite.Modulate = new Color(1, 0.3f, 0.3f);
        GetTree().CreateTimer(0.15).Timeout += () => 
        {
            if (IsInstanceValid(_sprite))
                _sprite.Modulate = Colors.White;
        };
    }

    public virtual void PlayAnimation(string action)
    {
        if (_sprite == null) return;
        if (_sprite.Animation != action)
            _sprite.Play(action);
    }

    public virtual void DropLoot() { }
}
