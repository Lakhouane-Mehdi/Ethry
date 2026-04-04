using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI for transferring items between the player's inventory and a storage container (chest).
/// </summary>
public partial class StorageUI : CanvasLayer
{
	public static StorageUI Instance { get; private set; }

	private Control   _root;
	private GridContainer _playerGrid;
	private GridContainer _chestGrid;
	private Label     _chestLabel;
	private Inventory _chestInventory;
	private Node2D    _activeSource;
	private const float CloseDistance = 100f;

	private PackedScene _slotScene;

	public override void _Ready()
	{
		Instance = this;
		Visible  = false;

		// We assume the scene has these nodes
		_root       = GetNode<Control>("Root");
		_playerGrid = _root.GetNode<GridContainer>("Panels/PlayerPanel/VBox/Margin/PlayerGrid");
		_chestGrid  = _root.GetNode<GridContainer>("Panels/ChestPanel/VBox/Margin/ChestGrid");
		_chestLabel = _root.GetNode<Label>("Panels/ChestPanel/VBox/RibbonBox/ChestLabel");
		
		_slotScene  = GD.Load<PackedScene>("res://scenes/ui/inventory_slot.tscn");
	}

	public void Open(Inventory chestInv, string chestName = "Storage", Node2D source = null)
	{
		_chestInventory = chestInv;
		_activeSource   = source;
		_chestLabel.Text = chestName;
		Visible = true;
		GetTree().Paused = true;
		Refresh();
	}

	public override void _Process(double delta)
	{
		if (!Visible || _activeSource == null) return;

		var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (player == null) return;

		float dist = player.GlobalPosition.DistanceTo(_activeSource.GlobalPosition);
		if (dist > CloseDistance)
		{
			Close();
		}
	}

	public void Close()
	{
		if (Visible && _activeSource != null)
		{
			_activeSource.Call("CloseStorage");
		}

		Visible = false;
		GetTree().Paused = false;
		_chestInventory = null;
		_activeSource   = null;
	}

	public void Refresh()
	{
		if (_chestInventory == null) return;

		ClearGrid(_playerGrid);
		ClearGrid(_chestGrid);

		PopulateGrid(_playerGrid, Inventory.Instance, true);
		PopulateGrid(_chestGrid,  _chestInventory, false);
	}

	private void ClearGrid(GridContainer grid)
	{
		foreach (Node child in grid.GetChildren())
			child.QueueFree();
	}

	private void PopulateGrid(GridContainer grid, Inventory inv, bool isToChest)
	{
		foreach (var (id, count) in inv.Items)
		{
			var slotNode = _slotScene.Instantiate<Control>();
			grid.AddChild(slotNode);
			
			// Setup slot (this depends on how InventorySlot is structured)
			// Assuming there's a script with a 'UpdateSlot' method
			slotNode.Call("UpdateSlot", id, count);
			
			// Handle clicks for transfer
			slotNode.GuiInput += (ev) =>
			{
				if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
				{
					if (isToChest)
						inv.TransferTo(_chestInventory, id, 1);
					else
						inv.TransferTo(Inventory.Instance, id, 1);
					
					Refresh();
				}
			};
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Visible && (@event.IsActionPressed("toggle_inventory") || @event.IsActionPressed("ui_cancel")))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}
}
