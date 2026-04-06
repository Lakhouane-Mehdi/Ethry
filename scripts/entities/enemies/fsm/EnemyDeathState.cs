using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for when the enemy is dying.
/// </summary>
public partial class EnemyDeathState : EnemyState
{
    public override void Enter()
    {
        _enemy.Velocity = Vector2.Zero;
        _enemy.PlayAnimation("die");
        _enemy.DropLoot();
        AudioManager.Instance?.PlaySfx("enemy_death");
        
        // Disable collision to avoid hitting a corpse. Deferred because we may
        // be inside a physics query flush when death is triggered.
        _enemy.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        _enemy.SetDeferred(CollisionObject2D.PropertyName.CollisionMask,  0);
    }

    public override void Update(double delta)
    {
        // Handled by animation finished signal in individual enemy scripts for now,
        // or we could add a timer here.
    }
}
