using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a farm plot that is empty (bare ground).
/// The player can till this soil with a shovel.
/// </summary>
public partial class PlotEmptyState : PlotState
{
    public override void Enter()
    {
        _plot.UpdateVisuals();
        _plot.UpdatePrompt();
    }

    public void Interact()
    {
        _plot.TryTill();
    }
}
