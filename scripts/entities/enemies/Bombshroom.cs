using Godot;
using FSM;

public partial class Bombshroom : Enemy
{
    [Export] public float ChargeSpeed = 120f;
    [Export] public float DetonateRange = 32f;
    [Export] public int ExplosionDamage = 3;
    [Export] public float ExplosionRadius = 60f;

    protected override void InitializeEnemy()
    {
        _sprite.AnimationFinished += OnAnimationFinished;
    }

    public override void TakeDamage(int damage, Vector2 knockbackDirection)
    {
        Health -= damage;
        UpdateLifeBar();
        FlashHit();
        
        Velocity = knockbackDirection * KnockbackForce;

        if (Health <= 0)
            _stateMachine.TransitionTo("Attack"); // Attack is the explosion
        else
            _stateMachine.TransitionTo("Hurt");
    }

    public override void DropLoot()
    {
        var pickupScene = GD.Load<PackedScene>("res://scenes/items/item_pickup.tscn");

        // Drop mushroom + herbs
        SpawnDrop(pickupScene, "Mushroom", (int)GD.RandRange(1, 3));

        if (GD.Randf() < 0.5f)
            SpawnDrop(pickupScene, "Herb", (int)GD.RandRange(1, 3));

        if (GD.Randf() < 0.15f)
            SpawnDrop(pickupScene, "Coal", 1);
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
        }
    }
}
