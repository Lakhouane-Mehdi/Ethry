using Godot;

namespace FSM;


/// <summary>
/// A specialized base class for all Player-specific states.
/// </summary>
public abstract partial class PlayerState : State
{
	protected Player _player;

	public override void _Ready()
	{
		_player = GetOwner<Player>();
	}
}

