using Godot;

public partial class World : Node2D
{
	private int _currentLevelIndex;

	public override void _Ready()
	{
		_currentLevelIndex = 0;
	}

	public int CurrentLevelIndex => _currentLevelIndex;
}
