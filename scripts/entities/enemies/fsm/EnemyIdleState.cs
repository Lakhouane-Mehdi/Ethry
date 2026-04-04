using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Passive idle/wander state for enemies.
/// </summary>
public partial class EnemyIdleState : EnemyState
{
    private float _wanderTimer;
    private Vector2 _wanderDirection;

    public override void Enter()
    {
        _wanderTimer = 0f;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (_enemy.Target != null)
        {
            GetParent<StateMachine>().TransitionTo("Chase");
            return;
        }

        _wanderTimer -= (float)delta;
        if (_wanderTimer <= 0)
        {
            PickRandomDirection();
            _wanderTimer = _enemy.WanderTime;
        }

        _enemy.Velocity = _wanderDirection * _enemy.Speed * 0.4f;
        _enemy.Sprite.FlipH = _wanderDirection.X < 0;
        _enemy.PlayAnimation("idle");
        _enemy.MoveAndSlide();
    }

    private void PickRandomDirection()
    {
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        _wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
