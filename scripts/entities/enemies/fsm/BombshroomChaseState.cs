using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized chase state for the Bombshroom.
/// Charges at the player with high speed and pulses red as a warning.
/// </summary>
public partial class BombshroomChaseState : EnemyState
{
    private float _chargeSpeed = 120f;
    private float _detonateRange = 32f;

    public override void PhysicsUpdate(double delta)
    {
        if (_enemy.Target == null)
        {
            _enemy.Sprite.Modulate = Colors.White;
            GetParent<StateMachine>().TransitionTo("Idle");
            return;
        }

        float distance = _enemy.GlobalPosition.DistanceTo(_enemy.Target.GlobalPosition);

        if (distance <= _detonateRange)
        {
            GetParent<StateMachine>().TransitionTo("Attack");
            return;
        }

        Vector2 direction = (_enemy.Target.GlobalPosition - _enemy.GlobalPosition).Normalized();
        _enemy.Velocity = direction * _chargeSpeed;
        _enemy.Sprite.FlipH = direction.X < 0;

        // Flash red while charging
        float pulse = Mathf.Abs(Mathf.Sin((float)Time.GetTicksMsec() / 150f));
        _enemy.Sprite.Modulate = new Color(1, 1 - pulse * 0.4f, 1 - pulse * 0.4f);

        _enemy.PlayAnimation("attack");
        _enemy.MoveAndSlide();
    }

    public override void Exit()
    {
        _enemy.Sprite.Modulate = Colors.White;
    }
}
