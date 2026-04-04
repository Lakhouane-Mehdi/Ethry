using Godot;
using FSM;

public partial class Slime : Enemy
{
    protected override void InitializeEnemy()
    {
        _sprite.AnimationFinished += OnAnimationFinished;
    }

    public override void DropLoot()
    {
        var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");

        // Always drop 1-2 fiber (slime residue)
        SpawnDrop(pickupScene, ItemType.Fiber, (int)GD.RandRange(1, 3));

        // 40% chance to drop herb
        if (GD.Randf() < 0.4f)
            SpawnDrop(pickupScene, ItemType.Herb, 1);

        // 15% chance to drop a berry
        if (GD.Randf() < 0.15f)
            SpawnDrop(pickupScene, ItemType.Berry, 1);
    }

    private void SpawnDrop(PackedScene scene, ItemType type, int amount)
    {
        var pickup    = scene.Instantiate<ItemPickup>();
        pickup.Type   = type;
        pickup.Amount = amount;

        float angle  = (float)GD.RandRange(0, Mathf.Tau);
        float radius = (float)GD.RandRange(12, 30);
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

        if (_sprite.Animation == "attack")
        {
            // Slime attack logic: check if target is still in range at end of anim
            if (_target is Player player)
            {
                if (GlobalPosition.DistanceTo(_target.GlobalPosition) <= AttackRange * 1.5f)
                {
                    Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
                    player.TakeDamage(AttackDamage, knockDir);
                }
            }
            _stateMachine.TransitionTo("Chase");
        }
    }
}
