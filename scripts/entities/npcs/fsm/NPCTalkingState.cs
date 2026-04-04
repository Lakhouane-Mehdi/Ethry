using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for an NPC that is currently in a dialogue session with the player.
/// Handles advancing lines and transitioning to shop or idle.
/// </summary>
public partial class NPCTalkingState : NPCState
{
    public override void Enter()
    {
        _npc.OpenDialogueUI();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            _npc.AdvanceDialogue();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void Exit()
    {
        _npc.HideDialogueUI();
    }
}
