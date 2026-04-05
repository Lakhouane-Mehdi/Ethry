using Godot;

namespace FSM;

/// <summary>
/// State for when the player is not moving.
/// </summary>
public partial class PlayerIdleState : PlayerState
{
	public override void Enter()
	{
		_player?.PlayAnimation("idle");
		_player.Velocity = Vector2.Zero;
	}

	public override void PhysicsUpdate(double delta)
	{
		if (Input.IsActionJustPressed("attack"))
		{
			string equipped = Equipment.Instance?.GetSlotId(EquipSlot.Weapon);
			bool isShovel = equipped != null && equipped.ToLower().Contains("shovel");
			
			if (isShovel)
				GetParent<StateMachine>().TransitionTo("Tilling");
			else
				GetParent<StateMachine>().TransitionTo("Attack");
			return;
		}

		Vector2 input = Input.GetVector("left", "right", "up", "down");
		if (input != Vector2.Zero)
		{
			GetParent<StateMachine>().TransitionTo("Move");
			return;
		}

		_player.Velocity = _player.Velocity.MoveToward(Vector2.Zero, _player.Friction * (float)delta);
		_player.MoveAndSlide();
	}
}

