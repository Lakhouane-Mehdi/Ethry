using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for an NPC when the player is browsing their shop.
/// Handles closing the shop and returning to idle.
/// </summary>
public partial class NPCShopState : NPCState
{
	private ShopUI _shop;

	public override void _Ready()
	{
		base._Ready();
		_shop = _npc.GetNodeOrNull<ShopUI>("ShopUI");
	}

	public override void Enter()
	{
		_shop?.Open();
	}

	public override void Update(double delta)
	{
		// Many shops handle their own closing via a 'Close' button, 
		// but we can check if the player walked away.
		if (!_npc.PlayerInRange)
		{
			_shop?.Close();
			GetParent<StateMachine>().TransitionTo("Idle");
		}
	}

	public override void Exit()
	{
		_shop?.Close();
	}
}
