using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Specialized base class for all ShippingBox states.
/// Provides a reference to the ShippingBox node.
/// </summary>
public abstract partial class ShippingState : State
{
    protected ShippingBox _node;

    public override void _Ready()
    {
        _node = GetOwner<ShippingBox>();
    }
}
