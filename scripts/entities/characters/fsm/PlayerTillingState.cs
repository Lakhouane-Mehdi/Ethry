using Godot;
using System.Linq;

namespace FSM;

/// <summary>
/// State for when the player is tilling the ground with a Shovel.
/// </summary>
public partial class PlayerTillingState : PlayerState
{
    private float _timer;
    private PackedScene _plotScene;

    public override void _Ready()
    {
        base._Ready();
        _plotScene = GD.Load<PackedScene>("res://scenes/interactables/farm_plot.tscn");
    }

    public override void Enter()
    {
        _player.Velocity = Vector2.Zero;
        _player.PlayAnimation("shovel"); 

        _timer = _player.AttackCooldown;
        _player.UpdateSpriteFlip();

        TryTill();
    }

    public override void Update(double delta)
    {
        _timer -= (float)delta;
        if (_timer <= 0)
        {
            GetParent<StateMachine>().TransitionTo("Idle");
        }
    }

    private void TryTill()
    {
        // Get tilling target (1 tile in front of player)
        Vector2 direction = GetFacingDirection();
        Vector2 targetPos = _player.GlobalPosition + (direction * 48f); // Increased distance slightly for 4x scale
        
        // Find the terrain TileMapLayer
        var level = _player.GetParent();
        var terrain = level.GetNodeOrNull<TileMapLayer>("terrain");
        
        if (terrain == null) return;

        Vector2I mapPos = terrain.LocalToMap(terrain.ToLocal(targetPos));
        int sourceId = terrain.GetCellSourceId(mapPos);

        // Validation: Must be Grass (Source 1)
        if (sourceId == 1)
        {
            // Check if a plot already exists here
            if (IsPlotAt(targetPos))
            {
                NotificationManager.Instance?.ShowWarning("Soil already tilled here.");
                return;
            }

            SpawnPlot(terrain.MapToLocal(mapPos) * terrain.Scale + terrain.GlobalPosition - new Vector2(32, 32)); 
            // Wait, snapping is tricky with scales. Let's simplify:
            Vector2 snappedPos = terrain.ToGlobal(terrain.MapToLocal(mapPos));
            
            SpawnPlot(snappedPos);
            NotificationManager.Instance?.Show("Soil tilled!", new Color(0.75f, 0.55f, 0.28f));
        }
        else
        {
            NotificationManager.Instance?.ShowWarning("You can only till grass or dirt.");
        }
    }

    private Vector2 GetFacingDirection()
    {
        // Use the player's last direction for much more reliable 4-way tilling
        string dir = _player.Call("GetDirectionName").AsString();
        return dir switch
        {
            "up" => Vector2.Up,
            "down" => Vector2.Down,
            "right" => _player.GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH ? Vector2.Left : Vector2.Right,
            _ => _player.GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH ? Vector2.Left : Vector2.Right
        };
    }

    private bool IsPlotAt(Vector2 pos)
    {
        var interactables = _player.GetParent().GetNodeOrNull("interactables");
        if (interactables == null) return false;

        foreach (Node child in interactables.GetChildren())
        {
            if (child is FarmPlot plot && plot.GlobalPosition.DistanceTo(pos) < 8f)
                return true;
        }
        return false;
    }

    private void SpawnPlot(Vector2 pos)
    {
        var interactables = _player.GetParent().GetNodeOrNull("interactables");
        if (interactables == null) return;

        var plot = _plotScene.Instantiate<FarmPlot>();
        interactables.AddChild(plot);
        plot.GlobalPosition = pos;
        
        // Force Tilled state immediately
        plot.GetNode<StateMachine>("StateMachine").TransitionTo("Tilled");
    }
}
