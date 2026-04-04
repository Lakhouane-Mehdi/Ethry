using Godot;

public partial class InventorySlot : Control
{
	private TextureRect _icon;
	private Label       _count;
	private Panel       _bg;

	public override void _Ready()
	{
		_bg    = GetNodeOrNull<Panel>("BG");
		_icon  = GetNodeOrNull<TextureRect>("Icon");
		_count = GetNodeOrNull<Label>("Count");
	}

	public void UpdateSlot(string itemId, int amount)
	{
		if (amount <= 0)
		{
			if (_icon != null) _icon.Visible = false;
			if (_count != null) _count.Visible = false;
			return;
		}

		var data = ItemDatabase.Instance?.Get(itemId);
		if (data != null && _icon != null)
		{
			_icon.Texture = data.Icon;
			_icon.Visible = true;
		}

		if (_count != null)
		{
			_count.Text = amount > 1 ? amount.ToString() : "";
			_count.Visible = amount > 1;
		}
	}
}
