using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized base class for all FarmPlot states.
/// Provides a reference to the FarmPlot node.
/// </summary>
public abstract partial class PlotState : State
{
    protected FarmPlot _plot;

    public override void _Ready()
    {
        _plot = GetOwner<FarmPlot>();
    }
}
