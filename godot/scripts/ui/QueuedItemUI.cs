using System;
using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.UI;

// PORT NOTE: Unity used `Image backgroundImage; backgroundImage.color = ...` to
// recolor a panel. Godot's nearest is `Control.Modulate` (multiplicative tint
// over whatever the existing theme draws). If you want a flat color swap
// instead, parent this under a `PanelContainer` with a `StyleBoxFlat` theme
// override and mutate the `BgColor` on the style box.
[GlobalClass]
public partial class QueuedItemUI : Control
{
    [Export] private Label NameText = null!;
    [Export] private Button SelectButton = null!;
    [Export] private Control Background = null!;
    [Export] public Color NormalColor = Colors.White;
    [Export] public Color HighlightColor = Colors.Yellow;

    private ShopItem _item;
    private Action<ShopItem>? _onSelectCallback;

    public void Initialize(ShopItem shopItem, Action<ShopItem> selectCallback)
    {
        _item = shopItem;
        _onSelectCallback = selectCallback;

        NameText.Text = _item.DisplayName;
        SelectButton.Pressed += OnSelectClick;
    }

    private void OnSelectClick()
    {
        _onSelectCallback?.Invoke(_item);
    }

    public void SetHighlight(bool highlighted)
    {
        Background.Modulate = highlighted ? HighlightColor : NormalColor;
    }

    public ShopItem GetItem() => _item;
}
