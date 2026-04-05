using Godot;

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance { get; private set; }

	private ColorRect _overlay;
	private string _nextScenePath;
	private Vector2 _spawnPosition;
	private float _fadeTimer;
	private float _holdTimer;
	private enum State { Idle, FadingIn, Holding, ChangingScene, FadingOut }
	private State _state = State.Idle;

	private const float FadeDuration = 0.5f;
	private const float HoldDuration = 0.5f;

	public override void _Ready()
	{
		Instance = this;
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		_overlay = new ColorRect();
		_overlay.Color = new Color(0, 0, 0, 0);
		_overlay.AnchorRight = 1;
		_overlay.AnchorBottom = 1;
		_overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_overlay);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		switch (_state)
		{
			case State.FadingIn:
				_fadeTimer += dt;
				float alphaIn = Mathf.Clamp(_fadeTimer / FadeDuration, 0, 1);
				_overlay.Color = new Color(0, 0, 0, alphaIn);
				if (_fadeTimer >= FadeDuration)
				{
					_overlay.Color = new Color(0, 0, 0, 1);
					_holdTimer = 0;
					_state = State.Holding;
				}
				break;

			case State.Holding:
				_holdTimer += dt;
				if (_holdTimer >= HoldDuration)
				{
					_state = State.ChangingScene;
				}
				break;

			case State.ChangingScene:
				GetTree().Paused = false;
				GetTree().ChangeSceneToFile(_nextScenePath);
				_fadeTimer = 0;
				_state = State.FadingOut;
				break;

			case State.FadingOut:
				if (_spawnPosition != Vector2.Inf)
				{
					var player = GetTree().GetFirstNodeInGroup("player");
					if (player is Node2D playerNode)
					{
						playerNode.GlobalPosition = _spawnPosition;
						_spawnPosition = Vector2.Inf;
					}
				}
				_fadeTimer += dt;
				float alphaOut = Mathf.Clamp(1.0f - _fadeTimer / FadeDuration, 0, 1);
				_overlay.Color = new Color(0, 0, 0, alphaOut);
				if (_fadeTimer >= FadeDuration)
				{
					_overlay.Color = new Color(0, 0, 0, 0);
					_state = State.Idle;
				}
				break;
		}
	}

	public void TransitionTo(string scenePath, Vector2 spawnPosition)
	{
		if (_state != State.Idle) return;

		_nextScenePath = scenePath;
		_spawnPosition = spawnPosition;
		_fadeTimer = 0;
		_state = State.FadingIn;
		GetTree().Paused = true;
		AudioManager.Instance?.PlaySfx("door_open");
	}
}
