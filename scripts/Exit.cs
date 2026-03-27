using Godot;

public partial class Exit : Area2D
{
	[Export] public string NextLevelPath;

	public override void _Ready()
	{
		Monitoring = true;
		CollisionLayer = 0;
		CollisionMask = 1;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player") || string.IsNullOrEmpty(NextLevelPath))
			return;

		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, NextLevelPath);
	}
}
