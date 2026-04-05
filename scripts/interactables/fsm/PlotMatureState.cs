using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a farm plot where the crop is fully grown and ready to harvest.
/// Player can harvest by interacting with it.
/// </summary>
public partial class PlotMatureState : PlotState
{
    public override void Enter()
    {
        _plot.UpdateVisuals();
        _plot.UpdatePrompt();
    }

    public override void Interact()
    {
        _plot.Harvest();
    }
}
