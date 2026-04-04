using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Default state for the Shipping Box. Handles proximity prompt.
/// </summary>
public partial class ShippingIdleState : ShippingState
{
    public override void Enter()
    {
        _node.UpdatePromptVisibility();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("interact"))
        {
            GetParent<StateMachine>().TransitionTo("Idle");
            GetViewport().SetInputAsHandled();
            return;
        }
    }
}
