using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a tree that has been chopped down to a stump.
/// Collisions are usually disabled in this state.
/// </summary>
public partial class ResourceStumpState : ResourceState
{
    public override void Enter()
    {
        _node.IsStump = true;
        _node.HasFruit = false;
        _node.UpdateVisuals();
        _node.UpdatePrompt();
    }
}
