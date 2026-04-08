using Godot;
using FSM;

public partial class Skeleton : Enemy
{
    private Vector2 _lastDirection = Vector2.Down;

    protected override void InitializeEnemy()
    {
        _sprite.AnimationFinished += OnAnimationFinished;
    }

    public override void PlayAnimation(string action)
    {
        // Update last direction if moving
        if (Velocity != Vector2.Zero)
            _lastDirection = Velocity.Normalized();

        UpdateSpriteFlip();
        string dirName = GetDirectionName();

        // Convention: action_direction (down is often just action).
        // "die" and any action lacking a directional variant fall back to the bare name.
        string animName = action;
        if (dirName != "down")
        {
            string candidate = $"{action}_{dirName}";
            if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(candidate))
                animName = candidate;
        }

        if (_sprite.Animation != animName)
            _sprite.Play(animName);
    }

    private string GetDirectionName()
    {
        if (Mathf.Abs(_lastDirection.X) > Mathf.Abs(_lastDirection.Y))
            return "right";
        return _lastDirection.Y < 0 ? "up" : "down";
    }

    private void UpdateSpriteFlip()
    {
        if (_lastDirection.X < 0)
            _sprite.FlipH = true;
        else if (_lastDirection.X > 0)
            _sprite.FlipH = false;
    }

    public override void DropLoot()
    {
        var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");

        // Always drop 1-2 bones
        SpawnDrop(pickupScene, "Bone", (int)GD.RandRange(1, 3));

        // 40% chance to drop iron ore
        if (GD.Randf() < 0.4f)
            SpawnDrop(pickupScene, "IronOre", 1);

        // 20% chance to drop a crystal
        if (GD.Randf() < 0.2f)
            SpawnDrop(pickupScene, "Crystal", 1);
    }

    private void SpawnDrop(PackedScene scene, string id, int amount)
    {
        var pickup    = scene.Instantiate<ItemPickup>();
        pickup.ItemId = id;
        pickup.Amount = amount;

        float angle  = (float)GD.RandRange(0, Mathf.Tau);
        float radius = (float)GD.RandRange(15, 35);
        pickup.Position = GlobalPosition + new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );

        GetTree().CurrentScene.AddChild(pickup);
    }

    private void OnAnimationFinished()
    {
        if (_sprite.Animation == "die")
        {
            CallDeferred(MethodName.QueueFree);
            return;
        }

        if (_sprite.Animation.ToString().StartsWith("attack"))
        {
            if (_target is Player player &&
                GlobalPosition.DistanceTo(_target.GlobalPosition) <= AttackRange * 1.5f)
            {
                Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
                player.TakeDamage(AttackDamage, knockDir);
            }
            _stateMachine.TransitionTo("Chase");
        }
    }
}
