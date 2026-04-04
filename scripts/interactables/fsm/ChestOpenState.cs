using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for an open chest. Handles opening the storage UI.
/// </summary>
public partial class ChestOpenState : ChestState
{
    public override void Enter()
    {
        _node.UpdatePrompt();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (_node.PlayerInRange && @event.IsActionPressed("interact"))
        {
            _node.OpenStorageUI();
            GetViewport().SetInputAsHandled();
        }
    }
}
