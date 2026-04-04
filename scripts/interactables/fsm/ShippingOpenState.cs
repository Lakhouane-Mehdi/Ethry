using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for the open shipping box. Handles UI navigation and selling logic.
/// </summary>
public partial class ShippingOpenState : ShippingState
{
    public override void Enter()
    {
        _node.OpenBox();
    }

    public override void Exit()
    {
        _node.CloseBox();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("interact"))
        {
            _node.GetNode<StateMachine>("StateMachine").TransitionTo("Idle");
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("ui_up"))
        {
            _node.Scroll( -1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_down"))
        {
            _node.Scroll(1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
            _node.ShipSelected();
            GetViewport().SetInputAsHandled();
        }
    }
}
