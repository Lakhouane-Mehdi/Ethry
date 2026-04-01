using Godot;

public partial class HUD : CanvasLayer
{
	private Node2D _heart1;
	private Node2D _heart2;
	private Node2D _heart3;
	private Player _player;

	public override void _Ready()
	{
		_heart1 = GetNode<Node2D>("heart1");
		_heart2 = GetNode<Node2D>("heart2");
		_heart3 = GetNode<Node2D>("heart3");
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}

	public override void _Process(double delta)
	{
		if (_player == null)
			return;

		UpdateHeart(_heart1, _player.Health, 0);
		UpdateHeart(_heart2, _player.Health, 1);
		UpdateHeart(_heart3, _player.Health, 2);
	}

	private void UpdateHeart(Node2D heart, int health, int index)
	{
		var full = heart.GetNode<Sprite2D>("health");
		var half = heart.GetNode<Sprite2D>("healthunder");
		var empty = heart.GetNode<Sprite2D>("healthzero");

		int hpForThisHeart = health - (index * 2);

		full.Visible = hpForThisHeart >= 2;
		half.Visible = hpForThisHeart == 1;
		empty.Visible = hpForThisHeart <= 0;
	}
}
