using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Bed state for when the player is nearby but hasn't interacted yet.
/// </summary>
public partial class BedIdleState : BedState
{
    public override void Enter()
    {
        _node.UpdatePromptVisibility();
        _node.HideConfirmation();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (_node.PlayerInRange && @event.IsActionPressed("interact"))
        {
            GetParent<StateMachine>().TransitionTo("Confirming");
            GetViewport().SetInputAsHandled();
        }
    }
}
