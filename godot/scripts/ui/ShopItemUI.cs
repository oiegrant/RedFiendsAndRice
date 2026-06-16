using System;
using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.UI;

// PORT NOTE — bundle of three small UI scripts from Unity's ShopItemUI.cs.
// In Godot each [GlobalClass] should ideally live in its own file so the
// editor's "Add Child Node" picker can find them. Split into:
//   ShopItemUI.cs   (this file — purchase button row)
//   QueuedItemUI.cs (highlighted, selectable queued upgrade)
//   DiceFaceUI.cs   (per-die-face slot in the upgrade screen)
//
// Common Unity→Godot migrations applied here:
//   [SerializeField] private Foo x;   →   [Export] private Foo X;
//   Button.onClick.AddListener(F)     →   Button.Pressed += F;
//   button.interactable = false       →   button.Disabled = true;
//   System.Action<T>                  →   same — kept as is.
//   TextMeshProUGUI.text              →   Label.Text
[GlobalClass]
public partial class ShopItemUI : Control
{
    [Export] private Label NameText = null!;
    [Export] private Label CostText = null!;
    [Export] private Button PurchaseButton = null!;
    [Export] private Label PurchaseButtonText = null!;

    private ShopItem _item;
    private Action<ShopItem>? _onPurchaseCallback;

    public void Initialize(ShopItem shopItem, Action<ShopItem> purchaseCallback)
    {
        _item = shopItem;
        _onPurchaseCallback = purchaseCallback;

        NameText.Text = _item.DisplayName;
        CostText.Text = $"Cost: {_item.Cost}";
        PurchaseButtonText.Text = "Purchase";

        // PORT NOTE: Godot signals don't auto-disconnect on QueueFree of *this*
        // node — they do auto-disconnect when *the emitter* (the button) is
        // freed. Since the button is our child, this is safe. If you ever
        // re-Initialize() on a recycled instance, remember to `-=` first.
        PurchaseButton.Pressed += OnPurchaseClick;
    }

    private void OnPurchaseClick()
    {
        _onPurchaseCallback?.Invoke(_item);
        PurchaseButton.Disabled = true;
    }
}
