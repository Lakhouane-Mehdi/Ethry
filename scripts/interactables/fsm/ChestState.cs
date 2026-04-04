using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized base class for all Chest states.
/// Provides a reference to the Chest node.
/// </summary>
public abstract partial class ChestState : State
{
    protected Chest _node;

    public override void _Ready()
    {
        _node = GetOwner<Chest>();
    }
}
