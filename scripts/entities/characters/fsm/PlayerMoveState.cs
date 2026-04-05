using Godot;

namespace FSM;

/// <summary>
/// State for when the player is moving.
/// </summary>
public partial class PlayerMoveState : PlayerState
{
	private float _dustTimer;
	private float _footstepTimer;

	public override void Enter()
	{
		_player?.PlayAnimation("run");
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
		if (input == Vector2.Zero)
		{
			GetParent<StateMachine>().TransitionTo("Idle");
			return;
		}

		bool isRunning = Input.IsActionPressed("running");
		float targetSpeed = isRunning ? _player.Speed * _player.RunSpeedMultiplier : _player.Speed;
		Vector2 targetVelocity = input * targetSpeed;

		_player.Velocity = _player.Velocity.MoveToward(targetVelocity, _player.Acceleration * (float)delta);
		_player.SetLastDirection(input);
		_player.PlayAnimation("run");
		_player.UpdateSpriteFlip();
		_player.MoveAndSlide();

		// Footstep sounds
		_footstepTimer -= (float)delta;
		if (_footstepTimer <= 0)
		{
			AudioManager.Instance?.PlaySfx("player_footstep", 0.05f);
			_footstepTimer = isRunning ? 0.22f : 0.32f;
		}

		// Dust logic
		if (_player.Velocity.Length() > _player.Speed)
		{
			_dustTimer -= (float)delta;
			if (_dustTimer <= 0)
			{
				if (GodotObject.IsInstanceValid(EffectsManager.Instance)) EffectsManager.Instance.SpawnDust(_player.GlobalPosition + new Vector2(0, 8));
				_dustTimer = 0.15f;
			}
		}
	}
}

