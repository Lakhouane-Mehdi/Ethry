using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Bed state for the sleeping transition.
/// Handles the fade-to-black and day advancement.
/// </summary>
public partial class BedSleepingState : BedState
{
    public override void Enter()
    {
        _node.HideConfirmation();
        _node.PerformSleepSequence();
    }
}
