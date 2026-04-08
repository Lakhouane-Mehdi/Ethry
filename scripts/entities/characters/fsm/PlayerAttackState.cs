using Godot;

namespace FSM;

/// <summary>
/// State for when the player is performing an attack or tool action.
/// </summary>
public partial class PlayerAttackState : PlayerState
{
	private float _timer;
	private bool  _isBow;
	private bool  _arrowFired;

	public override void Enter()
	{
		_player.Velocity = Vector2.Zero;

		// Select animation based on tool
		string prefix = GetToolPrefix(_player.EquippedToolId);
		_player.PlayAnimation(prefix);
		_player.UpdateSpriteFlip();

		_isBow      = prefix == "bow";
		_arrowFired = false;
		_timer      = _player.AttackCooldown;

		if (_isBow)
		{
			// Ranged: no melee hitbox. Arrow spawns partway through the draw.
		}
		else
		{
			_player.EnableHitBox();
			AudioManager.Instance?.PlaySfx("player_attack");
		}
	}

	public override void Exit()
	{
		_player.DisableHitBox();
	}

	public override void Update(double delta)
	{
		_timer -= (float)delta;

		// Fire the arrow near the end of the draw animation
		if (_isBow && !_arrowFired && _timer <= _player.AttackCooldown * 0.4f)
		{
			_player.FireArrow();
			_arrowFired = true;
		}

		if (_timer <= 0)
		{
			GetParent<StateMachine>().TransitionTo("Idle");
		}
	}

	private static string GetToolPrefix(string itemId)
	{
		if (string.IsNullOrEmpty(itemId)) return "attack";
		string idLower = itemId.ToLower();
		if (idLower.Contains("pickaxe")) return "pickaxe";
		if (idLower.Contains("axe"))     return "axe";
		if (idLower.Contains("shovel"))  return "shovel";
		if (idLower.Contains("bow"))     return "bow";
		return "attack";
	}
}

