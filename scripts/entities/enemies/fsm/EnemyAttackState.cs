using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Basic melee attack state for enemies.
/// </summary>
public partial class EnemyAttackState : EnemyState
{
    public override void Enter()
    {
        _enemy.Velocity = Vector2.Zero;
        _enemy.PlayAnimation("attack");
    }

    public override void Update(double delta)
    {
        // Handled by animation finished signal in individual enemy scripts for now,
        // but we can transition back after some time too.
    }
}
