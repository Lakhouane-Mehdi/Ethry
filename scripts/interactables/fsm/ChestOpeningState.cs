using Godot;
using FSM;

namespace FSM;

/// <summary>
/// Intermediate state for the opening animation and loot generation.
/// </summary>
public partial class ChestOpeningState : ChestState
{
    public override void Enter()
    {
        _node.PlayAnimation("open");
        _node.HidePrompt();
        AudioManager.Instance?.PlaySfx("chest_open");
        
        if (!_node.InitialLootGenerated)
            _node.GenerateInitialLoot();
    }

    public override void Update(double delta)
    {
        // Simple polling for animation finish, or could be signal-based
        if (_node.IsAnimationFinished("open"))
        {
            GetParent<StateMachine>().TransitionTo("Open");
            _node.OpenStorageUI();
        }
    }
}
