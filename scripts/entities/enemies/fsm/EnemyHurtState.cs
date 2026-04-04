using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for when the enemy takes damage.
/// </summary>
public partial class EnemyHurtState : EnemyState
{
    private float _timer;

    public override void Enter()
    {
        _timer = 0.2f; // Hurt animation duration
    }

    public override void PhysicsUpdate(double delta)
    {
        _timer -= (float)delta;
        _enemy.MoveAndSlide();

        if (_timer <= 0)
        {
            if (_enemy.Target != null)
                GetParent<StateMachine>().TransitionTo("Chase");
            else
                GetParent<StateMachine>().TransitionTo("Idle");
        }
    }
}
