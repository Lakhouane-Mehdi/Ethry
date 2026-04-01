using Godot;

/// <summary>
/// Tracks in-game time: hour of day (6→24), day (1–28), season, and year.
/// Time ticks in real-time; SecondsPerDay controls pacing.
/// Default: 8 real minutes = 1 in-game day.
/// </summary>
public partial class DaySystem : Node
{
	public static DaySystem Instance { get; private set; }

	[Export] public float SecondsPerDay = 480f;   // 8 real minutes = 1 day
	[Export] public float StartHour     = 6f;      // Day begins at 6:00 AM
	[Export] public float NightHour     = 22f;     // Auto-advance at 10:00 PM

	// ── Public state ───────────────────────────────────────────────────────
	public int   Day    { get; private set; } = 1;
	public int   Year   { get; private set; } = 1;
	public int   SeasonIndex { get; private set; } = 0;   // 0=Spring … 3=Winter
	public float Hour   { get; private set; } = 6f;
	public bool  IsNight => Hour >= NightHour || Hour < StartHour;

	public static readonly string[] SeasonNames  = { "Spring", "Summer", "Autumn", "Winter" };

	public string SeasonName => SeasonNames[SeasonIndex];
	public string TimeString
	{
		get
		{
			int h = (int)Hour;
			int m = (int)((Hour - h) * 60);
			string suffix = h < 12 ? "AM" : "PM";
			int h12 = h == 0 ? 12 : h > 12 ? h - 12 : h;
			return $"{h12}:{m:D2} {suffix}";
		}
	}
	public string DayString => $"Day {Day}  ·  {SeasonName}  ·  Year {Year}";

	// ── Signals ────────────────────────────────────────────────────────────
	[Signal] public delegate void DayAdvancedEventHandler(int day, int seasonIndex, int year);
	[Signal] public delegate void TimeChangedEventHandler(float hour);

	// ── Internal ───────────────────────────────────────────────────────────
	private float _secondsPerHour;
	private bool  _advancing;  // guard against double-advance

	// ── Lifecycle ──────────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance        = this;
		_secondsPerHour = SecondsPerDay / (NightHour - StartHour);
	}

	public override void _Process(double delta)
	{
		if (GetTree().Paused || _advancing) return;

		Hour += (float)delta / _secondsPerHour;
		EmitSignal(SignalName.TimeChanged, Hour);

		if (Hour >= NightHour)
			AdvanceDay();
	}

	// ── Public API ─────────────────────────────────────────────────────────
	/// <summary>Restores day/season/year from a save file.</summary>
	public void LoadState(int day, int seasonIndex, int year)
	{
		Day         = day;
		SeasonIndex = Mathf.Clamp(seasonIndex, 0, 3);
		Year        = year;
		Hour        = StartHour;
	}

	/// <summary>Call when player sleeps to jump to next morning.</summary>
	public void AdvanceDay()
	{
		if (_advancing) return;
		_advancing = true;

		Hour = StartHour;
		Day++;
		if (Day > 28)
		{
			Day          = 1;
			SeasonIndex  = (SeasonIndex + 1) % 4;
			if (SeasonIndex == 0) Year++;
		}

		EmitSignal(SignalName.DayAdvanced, Day, SeasonIndex, Year);
		NotificationManager.Instance?.Show(
			$"☀  {SeasonName} — Day {Day}", new Color(0.95f, 0.85f, 0.45f));

		_advancing = false;
	}
}
