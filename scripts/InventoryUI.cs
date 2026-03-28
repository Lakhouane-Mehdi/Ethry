using Godot;

public partial class InventoryUI : CanvasLayer
{
	private Panel _panel;
	private VBoxContainer _itemList;
	private bool _isVisible;

	public override void _Ready()
	{
		_panel = GetNode<Panel>("Panel");
		_itemList = GetNode<VBoxContainer>("Panel/VBoxContainer/ItemList");
		_panel.Visible = false;
		Inventory.Instance.Changed += Refresh;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("toggle_inventory"))
			return;

		_isVisible = !_isVisible;
		_panel.Visible = _isVisible;

		if (_isVisible)
			Refresh();
	}

	private void Refresh()
	{
		foreach (Node child in _itemList.GetChildren())
			child.QueueFree();

		bool empty = true;
		foreach (var (type, count) in Inventory.Instance.Items)
		{
			var label = new Label();
			label.Text = $"{ItemRegistry.GetName(type)}: {count}";
			label.AddThemeColorOverride("font_color", Colors.White);
			_itemList.AddChild(label);
			empty = false;
		}

		if (empty)
		{
			var label = new Label();
			label.Text = "(empty)";
			label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
			_itemList.AddChild(label);
		}
	}
}
