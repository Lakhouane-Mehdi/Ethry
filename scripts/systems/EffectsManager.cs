using Godot;

/// <summary>
/// Singleton manager for creating common visual effects (particles, flashes, etc.)
/// and handling their lifecycle.
/// </summary>
public partial class EffectsManager : Node
{
	public static EffectsManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>Spawns simple dust puff particles at a position.</summary>
	public void SpawnDust(Vector2 globalPos, Color? color = null)
	{
		var particles = CreateBasicParticles(8);
		particles.GlobalPosition = globalPos;
		particles.Amount         = 6;
		particles.Lifetime       = 0.4f;
		particles.OneShot        = true;
		particles.Explosiveness  = 0.8f;
		particles.Spread         = 180f;
		particles.Gravity        = new Vector2(0, -20); // drift up slightly
		particles.InitialVelocityMin = 10f;
		particles.InitialVelocityMax = 20f;
		particles.ScaleAmountMin     = 2f;
		particles.ScaleAmountMax     = 4f;
		particles.Color              = color ?? new Color(0.9f, 0.9f, 0.9f, 0.6f);
		
		AddChild(particles);
		particles.Emitting = true;
	}

	/// <summary>Spawns "impact" particles for harvesting/combat.</summary>
	public void SpawnImpact(Vector2 globalPos, Color color, int amount = 10)
	{
		var particles = CreateBasicParticles(amount);
		particles.GlobalPosition    = globalPos;
		particles.Lifetime          = 0.3f;
		particles.OneShot           = true;
		particles.Explosiveness     = 1.0f;
		particles.Spread            = 360f;
		particles.Gravity           = new Vector2(0, 150); // fall down
		particles.InitialVelocityMin = 40f;
		particles.InitialVelocityMax = 80f;
		particles.ScaleAmountMin     = 1.5f;
		particles.ScaleAmountMax     = 3.0f;
		particles.Color              = color;

		AddChild(particles);
		particles.Emitting = true;
	}

	private CpuParticles2D CreateBasicParticles(int amount)
	{
		var p = new CpuParticles2D();
		p.Amount = amount;
		p.DrawOrder = CpuParticles2D.DrawOrderEnum.Lifetime;
		
		// Auto-delete after finished
		p.Finished += () => p.QueueFree();
		
		return p;
	}
}
