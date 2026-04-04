using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized base class for all NPC states.
/// Provides a reference to the NPC node.
/// </summary>
public abstract partial class NPCState : State
{
    protected NPC _npc;

    public override void _Ready()
    {
        _npc = GetOwner<NPC>();
    }
}
