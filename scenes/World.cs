using Godot;

public partial class World : Node2D
{
	private static readonly string[] LevelPaths = {
		"res://scenes/levels/level_1.tscn",
		"res://scenes/levels/level_2.tscn",
	};

	private int _currentLevelIndex;
	private Node _currentLevel;

	public override void _Ready()
	{
		_currentLevel = GetNode("levelroot");
		_currentLevelIndex = 0;
		ConnectExit();
	}

	private void ConnectExit()
	{
		var exit = _currentLevel.GetNodeOrNull<Area2D>("exit");
		if (exit != null)
			exit.BodyEntered += OnExitBodyEntered;
	}

	private void OnExitBodyEntered(Node2D body)
	{
		if (body.IsInGroup("player"))
			CallDeferred(MethodName.NextLevel);
	}

	public void LoadLevel(int levelIndex)
	{
		if (levelIndex < 0 || levelIndex >= LevelPaths.Length)
			return;

		_currentLevel?.QueueFree();

		_currentLevelIndex = levelIndex;
		var scene = GD.Load<PackedScene>(LevelPaths[levelIndex]);
		_currentLevel = scene.Instantiate();
		AddChild(_currentLevel);
		ConnectExit();
	}

	public void NextLevel()
	{
		LoadLevel(_currentLevelIndex + 1);
	}

	public int CurrentLevelIndex => _currentLevelIndex;
}
