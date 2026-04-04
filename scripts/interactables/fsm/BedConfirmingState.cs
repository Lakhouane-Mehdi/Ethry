using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Bed state for when the player has interacted and is confirming sleep.
/// </summary>
public partial class BedConfirmingState : BedState
{
    public override void Enter()
    {
        _node.ShowConfirmation();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            GetParent<StateMachine>().TransitionTo("Sleeping");
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            GetParent<StateMachine>().TransitionTo("Idle");
            GetViewport().SetInputAsHandled();
        }
    }
}
