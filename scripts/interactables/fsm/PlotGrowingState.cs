using Godot;
using FSM;

namespace FSM;

/// <summary>
/// State for a farm plot with a growing crop.
/// Advances growth every day and resets watering.
/// </summary>
public partial class PlotGrowingState : PlotState
{
	public override void Enter()
	{
		_plot.UpdateVisuals();
		_plot.UpdatePrompt();
		if (DaySystem.Instance != null)
			DaySystem.Instance.DayAdvanced += OnDayAdvanced;
	}

	public void Interact()
	{
		// Even when growing, we might want to water if it's dry
		if (!_plot.TryWater())
		{
			_plot.GrowInfo();
		}
	}

	private void OnDayAdvanced(int day, int season, int year)
	{
		if (_plot.CurrentCrop == null) return;

		// Grow each day
		_plot.GrowthDay++;
		_plot.IsWatered = false; // reset watering each morning
		
		if (_plot.GrowthDay >= _plot.CurrentCrop.GrowthDays)
		{
			GetParent<StateMachine>().TransitionTo("Mature");
		}
		else
		{
			_plot.UpdateVisuals();
			_plot.UpdatePrompt();
		}
	}

	public override void Exit()
	{
		if (DaySystem.Instance != null)
			DaySystem.Instance.DayAdvanced -= OnDayAdvanced;
	}
}
