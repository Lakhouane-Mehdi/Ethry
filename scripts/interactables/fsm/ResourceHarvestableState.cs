using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a resource node that has fruit ready for harvest.
/// Can also be damaged (which drops the fruit and primary drops).
/// </summary>
public partial class ResourceHarvestableState : ResourceHealthyState
{
    public override void Enter()
    {
        _node.HasFruit = true;
        _node.UpdateVisuals();
        _node.UpdatePrompt();
    }

    public override void HandleInput(InputEvent @event)
    {
        if (_node.PlayerNear && @event.IsActionPressed("interact"))
        {
            _node.HarvestFruit();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void TakeDamage(int damage, Player attacker)
    {
        base.TakeDamage(damage, attacker);
    }
}
