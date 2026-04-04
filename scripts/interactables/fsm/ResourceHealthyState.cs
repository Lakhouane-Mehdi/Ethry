using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a healthy resource node that can be damaged.
/// </summary>
public partial class ResourceHealthyState : ResourceState
{
    public override void Enter()
    {
        _node.UpdateVisuals();
    }

    public virtual void TakeDamage(int damage, Player attacker)
    {
        _node.PerformDamage(damage, attacker);
        
        if (_node.Health <= 0)
        {
            _node.ChopDown(); // This handles transition to Stump or Empty
        }
    }
}
