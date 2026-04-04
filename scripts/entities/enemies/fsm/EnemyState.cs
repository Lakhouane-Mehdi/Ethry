using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized base class for all enemy states.
/// Provides a reference to the Enemy node.
/// </summary>
public abstract partial class EnemyState : State
{
    protected Enemy _enemy;

    public override void _Ready()
    {
        _enemy = GetOwner<Enemy>();
    }
}
