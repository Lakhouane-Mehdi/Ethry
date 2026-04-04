using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Base class for all Bed states.
/// </summary>
public abstract partial class BedState : State
{
    protected Bed _node;

    public override void _Ready()
    {
        _node = GetOwner<Bed>();
    }
}
