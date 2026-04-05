using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a tilled farm plot.
/// Player can plant seeds or water the soil.
/// </summary>
public partial class PlotTilledState : PlotState
{
    public override void Enter()
    {
        _plot.RefreshAutoTile();
        _plot.UpdatePrompt();
    }

    public override void Interact()
    {
        if (!_plot.TryWater())
        {
            _plot.TryPlant();
        }
    }
}
