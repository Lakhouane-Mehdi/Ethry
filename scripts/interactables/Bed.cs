using Godot;
using FSM;

/// <summary>
/// Bed interactable — walk near and interact to sleep.
/// Refactored to use a StateMachine for the interaction flow.
/// </summary>
public partial class Bed : Node2D
{
    private bool _playerInRange;
    private FSM.StateMachine _stateMachine;

    // Floating UI components
    private Node2D _anchor;
    private Control _uiRoot;
    private Label _promptLabel;
    private Label _confirmLabel;

    public bool PlayerInRange => _playerInRange;

    public override void _Ready()
    {
        BuildPrompt();
        _stateMachine = GetNode<FSM.StateMachine>("StateMachine");

        var area = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = 24f } };
        area.AddChild(shape);
        AddChild(area);

        area.BodyEntered += OnEnter;
        area.BodyExited += OnExit;
    }

    public void UpdatePromptVisibility()
    {
        if (_promptLabel != null)
            _promptLabel.Visible = _playerInRange;
    }

    public void ShowConfirmation()
    {
        if (_promptLabel != null) _promptLabel.Visible = false;
        if (_confirmLabel != null) _confirmLabel.Visible = true;
    }

    public void HideConfirmation()
    {
        if (_confirmLabel != null) _confirmLabel.Visible = false;
        UpdatePromptVisibility();
    }

    public override void _Process(double delta)
    {
        if (_uiRoot != null)
            _uiRoot.GlobalPosition = GlobalPosition + new Vector2(-70, -44);
        
        _stateMachine?.Update(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        _stateMachine?.HandleInput(@event);
    }

    public void PerformSleepSequence()
    {
        // Disable player input during sleep
        var player = GetTree().GetFirstNodeInGroup("player") as Player;

        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        overlay.ZIndex = 200;

        var fadeLayer = new CanvasLayer { Layer = 99 };
        fadeLayer.AddChild(overlay);
        GetTree().Root.AddChild(fadeLayer);

        var tween = GetTree().CreateTween();
        
        // Fade out
        tween.TweenProperty(overlay, "color:a", 1.0f, 0.6f);
        
        // World update
        tween.TweenCallback(Callable.From(() =>
        {
            DaySystem.Instance?.AdvanceDay();
            if (player != null) player.Heal(player.MaxHealth);
        })).SetDelay(0.3f);
        
        // Notify
        tween.TweenCallback(Callable.From(() =>
        {
            NotificationManager.Instance?.Show("Good morning!", new Color(0.95f, 0.85f, 0.45f));
        })).SetDelay(0.4f);
        
        // Fade in
        tween.TweenProperty(overlay, "color:a", 0.0f, 0.6f);
        
        // Transition back to Idle and cleanup
        tween.TweenCallback(Callable.From(() => {
            _stateMachine.TransitionTo("Idle");
            fadeLayer.QueueFree();
        }));
    }

    private void OnEnter(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;
        _stateMachine?.CurrentState?.Enter(); // Trigger state update
    }

    private void OnExit(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = false;
        
        if (_stateMachine != null && _stateMachine.CurrentState is not BedIdleState)
            _stateMachine.TransitionTo("Idle");
        else
            UpdatePromptVisibility();
    }

    private void BuildPrompt()
    {
        _anchor = new Node2D { TopLevel = true };
        AddChild(_anchor);

        _uiRoot = new Control();
        _anchor.AddChild(_uiRoot);

        _promptLabel = MakeLabel("[E]  Sleep", Colors.White, 12);
        _promptLabel.Visible = false;
        _uiRoot.AddChild(_promptLabel);

        _confirmLabel = MakeLabel("[E] Confirm  •  [Esc] Cancel", new Color(0.9f, 0.82f, 0.45f), 11);
        _confirmLabel.Position = new Vector2(0, 16);
        _confirmLabel.Visible = false;
        _uiRoot.AddChild(_confirmLabel);
    }

    private static Label MakeLabel(string text, Color color, int size)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        lbl.AddThemeFontSizeOverride("font_size", size);
        lbl.AddThemeConstantOverride("shadow_offset_x", 1);
        lbl.AddThemeConstantOverride("shadow_offset_y", 1);
        return lbl;
    }
}
