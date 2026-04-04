using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for when the enemy is chasing the player.
/// </summary>
public partial class EnemyChaseState : EnemyState
{
    public override void PhysicsUpdate(double delta)
    {
        if (_enemy.Target == null)
        {
            GetParent<StateMachine>().TransitionTo("Idle");
            return;
        }

        float distance = _enemy.GlobalPosition.DistanceTo(_enemy.Target.GlobalPosition);

        if (distance <= _enemy.AttackRange)
        {
            GetParent<StateMachine>().TransitionTo("Attack");
            return;
        }

        Vector2 direction = (_enemy.Target.GlobalPosition - _enemy.GlobalPosition).Normalized();
        _enemy.Velocity = direction * _enemy.Speed;
        _enemy.Sprite.FlipH = direction.X < 0;
        _enemy.PlayAnimation("idle");
        _enemy.MoveAndSlide();
    }
}
