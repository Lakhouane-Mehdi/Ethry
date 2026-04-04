using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a closed chest. Handles interaction to open it.
/// </summary>
public partial class ChestClosedState : ChestState
{
    public override void Enter()
    {
        _node.UpdatePrompt();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (_node.PlayerInRange && @event.IsActionPressed("interact"))
        {
            _node.GetNode<StateMachine>("StateMachine").TransitionTo("Opening");
            GetViewport().SetInputAsHandled();
        }
    }
}
