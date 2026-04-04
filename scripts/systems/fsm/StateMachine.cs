using Godot;

namespace FSM;

/// <summary>
/// Manages the state transitions for a Finite State Machine (FSM).
/// </summary>
public partial class StateMachine : Node
{
	[Export] public State InitialState;
	
	private State _currentState;
	public State CurrentState => _currentState;

	public override async void _Ready()
	{
		// Wait for the parent to finish its _Ready setup
		await ToSignal(GetParent(), Node.SignalName.Ready);

		if (_currentState != null) return;

		if (InitialState != null)
		{
			_currentState = InitialState;
			_currentState.Enter();
		}
		else
		{
			// Fallback: Try to find a child named "Idle"
			var idle = GetNodeOrNull<State>("Idle");
			if (idle != null)
			{
				_currentState = idle;
				_currentState.Enter();
				// Optional: GD.Print($"StateMachine on {GetParent().Name}: InitialState was empty, using 'Idle' fallback.");
			}
			else
			{
				// Secondary fallback: just use the first child that is a State.
				foreach (var child in GetChildren())
				{
					if (child is State s)
					{
						_currentState = s;
						_currentState.Enter();
						// Optional: GD.Print($"StateMachine on {GetParent().Name}: InitialState empty, using first child '{s.Name}' fallback.");
						return;
					}
				}

				GD.PrintErr($"StateMachine on {GetParent().Name}: InitialState is not set and no valid states found! This will prevent logic.");
			}
		}
	}

	public void Update(double delta)
	{
		_currentState?.Update(delta);
	}

	public void PhysicsUpdate(double delta)
	{
		_currentState?.PhysicsUpdate(delta);
	}

	public void HandleInput(InputEvent @event)
	{
		_currentState?.HandleInput(@event);
	}

	public void TransitionTo(string targetStateName)
	{
		if (!HasNode(targetStateName))
		{
			GD.PrintErr($"StateMachine: State '{targetStateName}' not found on {GetParent().Name}.");
			return;
		}

		var targetState = GetNode<State>(targetStateName);
		if (targetState == null)
		{
			GD.PrintErr($"StateMachine: State '{targetStateName}' is not a valid State node.");
			return;
		}

		_currentState?.Exit();
		_currentState = targetState;
		_currentState.Enter();
	}
}

