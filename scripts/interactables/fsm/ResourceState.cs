using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized base class for all resource node states.
/// Provides a reference to the ResourceNode node.
/// </summary>
public abstract partial class ResourceState : State
{
    protected ResourceNode _node;

    public override void _Ready()
    {
        _node = GetOwner<ResourceNode>();
    }
}
