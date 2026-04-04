using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a resource node that has been harvested and is waiting for new fruit.
/// Still acts like a healthy node (can take damage).
/// </summary>
public partial class ResourceRegrowingState : ResourceHealthyState
{
    public override void Enter()
    {
        _node.HasFruit = false;
        _node.UpdateVisuals();
        _node.UpdatePrompt();
        
        if (DaySystem.Instance != null)
            DaySystem.Instance.DayAdvanced += OnDayAdvanced;
    }

    public override void Exit()
    {
        if (DaySystem.Instance != null)
            DaySystem.Instance.DayAdvanced -= OnDayAdvanced;
    }

    public override void HandleInput(InputEvent @event)
    {
        if (_node.PlayerNear && @event.IsActionPressed("interact"))
        {
            if (_node.Tree is { IsFruitTree: true } && !_node.IsInFruitSeason())
            {
                string seasonList = _node.GetFruitSeasonNames();
                NotificationManager.Instance?.ShowInfo($"{_node.Tree.DisplayName} bears fruit in {seasonList}");
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnDayAdvanced(int day, int season, int year)
    {
        if (_node.IsStump || _node.Tree == null || !_node.Tree.IsFruitTree) return;

        _node.DaysSinceHarvest++;

        if (!_node.HasFruit && _node.IsInFruitSeason() && _node.DaysSinceHarvest >= _node.Tree.FruitRegrowDays)
        {
            _node.HasFruit = true;
            GetParent<StateMachine>().TransitionTo("Harvestable");
        }
        else
        {
            _node.UpdatePrompt();
        }
    }
}
