using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Explosion state for the Bombshroom.
/// Deals area damage to the player and then queues free the enemy.
/// </summary>
public partial class BombshroomExplodeState : EnemyState
{
    private float _explosionRadius = 60f;
    private int _explosionDamage = 3;

    public override void Enter()
    {
        _enemy.Velocity = Vector2.Zero;
        _enemy.Sprite.Modulate = Colors.White;
        _enemy.PlayAnimation("die");

        // Deal area damage to the player if in range
        var player = _enemy.GetTree().GetFirstNodeInGroup("player") as Player;
        if (player != null)
        {
            float dist = _enemy.GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist <= _explosionRadius)
            {
                Vector2 knockDir = (player.GlobalPosition - _enemy.GlobalPosition).Normalized();
                player.TakeDamage(_explosionDamage, knockDir);
            }
        }

        _enemy.DropLoot();
    }

    public override void Update(double delta)
    {
        // Handled by signal in Bombshroom.cs, or we can use a timer here.
    }
}
