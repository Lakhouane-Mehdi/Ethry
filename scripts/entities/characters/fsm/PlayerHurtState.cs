using Godot;

namespace FSM;

/// <summary>
/// State for when the player is knocked back after taking damage.
/// </summary>
public partial class PlayerHurtState : PlayerState
{
	private float _timer;

	public override void Enter()
	{
		_player.Modulate = new Color(1, 0.3f, 0.3f);
		_timer = 0.15f; // Knockback duration
		AudioManager.Instance?.PlaySfx("player_hurt");
	}

	public override void Exit()
	{
		_player.Modulate = Colors.White;
		_player.Velocity = Vector2.Zero;
	}

	public override void PhysicsUpdate(double delta)
	{
		_timer -= (float)delta;
		_player.MoveAndSlide();

		if (_timer <= 0)
		{
			GetParent<StateMachine>().TransitionTo("Idle");
		}
	}
}

