using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for an NPC that is idle and waiting for the player to interact.
/// Displays the [E] prompt when player is in range.
/// </summary>
public partial class NPCIdleState : NPCState
{
    public override void Enter()
    {
        _npc.UpdatePromptVisibility();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (_npc.PlayerInRange && @event.IsActionPressed("interact"))
        {
            _npc.StartDialogue();
            GetViewport().SetInputAsHandled();
        }
    }
}
