using Godot;

namespace FSM;

/// <summary>
/// State for when the player is dead and waiting for respawn.
/// </summary>
public partial class PlayerDeathState : PlayerState
{
	private float _respawnTimer;

	public override void Enter()
	{
		_player.Velocity = Vector2.Zero;
		_player.PlayAnimation("dying");
		_player.DisableHurtBox();
		AudioManager.Instance?.PlaySfx("player_death");
		_player.DisableHitBox();
		_respawnTimer = 2.0f; // Let the animation play before respawning
	}

	public override void Update(double delta)
	{
		_respawnTimer -= (float)delta;
		if (_respawnTimer <= 0)
		{
			_player.Respawn();
			GetParent<StateMachine>().TransitionTo("Idle");
		}
	}
}

