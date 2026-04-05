using Godot;

namespace FSM;

/// <summary>
/// State for when the player is performing an attack or tool action.
/// </summary>
public partial class PlayerAttackState : PlayerState
{
	private float _timer;

	public override void Enter()
	{
		_player.Velocity = Vector2.Zero;
		
		// Select animation based on tool
		string prefix = ItemRegistry.GetToolPrefix(_player.EquippedToolId);
		_player.PlayAnimation(prefix);

		_timer = _player.AttackCooldown;
		_player.UpdateSpriteFlip();
		_player.EnableHitBox();
		AudioManager.Instance?.PlaySfx("player_attack");
	}

	public override void Exit()
	{
		_player.DisableHitBox();
	}

	public override void Update(double delta)
	{
		_timer -= (float)delta;
		if (_timer <= 0)
		{
			GetParent<StateMachine>().TransitionTo("Idle");
		}
	}
}

