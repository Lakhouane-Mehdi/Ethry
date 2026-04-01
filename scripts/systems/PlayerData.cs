using Godot;

/// <summary>
/// Persistent player economy: gold currency.
/// All earn/spend operations go through this singleton.
/// </summary>
public partial class PlayerData : Node
{
	public static PlayerData Instance { get; private set; }

	public int Gold { get; private set; } = 0;

	[Signal] public delegate void GoldChangedEventHandler(int newTotal);

	public override void _Ready() => Instance = this;

	// ── API ────────────────────────────────────────────────────────────────
	public void AddGold(int amount)
	{
		if (amount <= 0) return;
		Gold += amount;
		EmitSignal(SignalName.GoldChanged, Gold);
		NotificationManager.Instance?.Show(
			$"+ {amount}g", new Color(1f, 0.88f, 0.28f));
	}

	/// <summary>Returns false (and charges nothing) if not enough gold.</summary>
	public bool SpendGold(int amount)
	{
		if (amount <= 0) return true;
		if (Gold < amount)
		{
			NotificationManager.Instance?.ShowWarning("Not enough gold!");
			return false;
		}
		Gold -= amount;
		EmitSignal(SignalName.GoldChanged, Gold);
		return true;
	}
}
