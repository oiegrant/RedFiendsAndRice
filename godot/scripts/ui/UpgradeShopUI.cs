using System.Collections.Generic;
using Godot;
using RedFiendsAndRice.Data;
using RedFiendsAndRice.System;

namespace RedFiendsAndRice.UI;

// PORT NOTE — heavy editor-driven UI script.
//
// Unity pattern    →    Godot equivalent (used here)
//   [Header(...)]          (no equivalent — drop, or group via inspector category)
//   [SerializeField] Foo   [Export] Foo
//   GameObject prefab      PackedScene scene
//   Instantiate(p, parent) var inst = scene.Instantiate<T>(); parent.AddChild(inst);
//   foreach Transform c    foreach (Node c in container.GetChildren())
//     in container
//   Destroy(gameObject)    QueueFree()
//   gameObject.SetActive   node.Visible = bool (or ProcessMode for pausing)
//   GetComponent<T>        the instance is already typed — no GetComponent needed
//                          (cast or use generic `Instantiate<T>()`)
[GlobalClass]
public partial class UpgradeShopUI : Control
{
    [Export] private Control ShopPanel = null!;
    [Export] private Node AbilityUpgradeContainer = null!;
    [Export] private Node MultiDieUpgradeContainer = null!;
    [Export] private Node UpgradeQueueContainer = null!;
    [Export] private Node DiceFacesContainer = null!;

    [Export] private PackedScene ShopItemScene = null!;
    [Export] private PackedScene QueuedItemScene = null!;
    [Export] private PackedScene DiceFaceScene = null!;

    // PORT NOTE: Unity exposed `RoundUpgradeManager` as a `[SerializeField]`
    // because Unity could serialize plain-C# classes via SerializeReference.
    // Godot's `[Export]` only handles `Resource` / `Node` / primitives. So
    // either:
    //   (a) keep this as a plain field set by code (current approach), or
    //   (b) turn RoundUpgradeManager into a `Resource` subclass for inspector
    //       drag-and-drop.
    public RoundUpgradeManager ShopManager = null!;

    private List<ShopItem> _currentAbilityUpgrades = new();
    private List<ShopItem> _currentMultiUpgrades = new();
    private Queue<ShopItem> _purchaseQueue = new();
    private ShopItem _selectedQueuedItem;
    private bool _isItemSelected;

    private readonly List<Node> _queuedItemButtons = new();
    private readonly List<Node> _diceFaceButtons = new();

    public void ShowShop(int currentEnemiesDefeated)
    {
        _currentAbilityUpgrades = ShopManager.GetRandomAbilityUpgradeSet(currentEnemiesDefeated);
        _currentMultiUpgrades = ShopManager.GetRandomMultiUpgradeSet(currentEnemiesDefeated);

        PopulateAbilityUpgrades();
        PopulateMultiUpgrades();
        PopulateDiceFaces();

        ShopPanel.Visible = true;
    }

    private void PopulateAbilityUpgrades()
        => PopulateItemContainer(AbilityUpgradeContainer, _currentAbilityUpgrades);

    private void PopulateMultiUpgrades()
        => PopulateItemContainer(MultiDieUpgradeContainer, _currentMultiUpgrades);

    private void PopulateItemContainer(Node container, List<ShopItem> items)
    {
        ClearChildren(container);
        foreach (var item in items)
        {
            var itemUI = ShopItemScene.Instantiate<ShopItemUI>();
            container.AddChild(itemUI);
            itemUI.Initialize(item, OnPurchaseClicked);
        }
    }

    private void PopulateDiceFaces()
    {
        ClearChildren(DiceFacesContainer);
        _diceFaceButtons.Clear();

        // PORT NOTE: hardcoded to 6 dice faces (matches D6). Pull from
        // DiceFaceNormals.D6.Length if you ever support other shapes.
        for (int i = 0; i < 6; i++)
        {
            var faceUI = DiceFaceScene.Instantiate<DiceFaceUI>();
            DiceFacesContainer.AddChild(faceUI);
            faceUI.Initialize(i, OnDiceFaceClicked);
            _diceFaceButtons.Add(faceUI);
        }
    }

    private void OnPurchaseClicked(ShopItem item)
    {
        _purchaseQueue.Enqueue(item);

        var queuedUI = QueuedItemScene.Instantiate<QueuedItemUI>();
        UpgradeQueueContainer.AddChild(queuedUI);
        queuedUI.Initialize(item, OnQueuedItemClicked);
        _queuedItemButtons.Add(queuedUI);

        // TODO deduct currency, disable purchase button, etc.
    }

    private void OnQueuedItemClicked(ShopItem item)
    {
        _selectedQueuedItem = item;
        _isItemSelected = true;
        UpdateQueuedItemVisuals();
    }

    private void OnDiceFaceClicked(int faceIndex)
    {
        if (!_isItemSelected)
        {
            GD.Print("Select an upgrade from the queue first!");
            return;
        }

        ApplyUpgradeToFace(_selectedQueuedItem, faceIndex);
        RemoveItemFromQueue(_selectedQueuedItem);

        _isItemSelected = false;
        _selectedQueuedItem = default;

        UpdateQueuedItemVisuals();
    }

    private void ApplyUpgradeToFace(ShopItem upgrade, int faceIndex)
    {
        // TODO real game logic — push upgrade onto the face's modifier list.
        GD.Print($"Applied {upgrade.DisplayName} to dice face {faceIndex}");

        if (_diceFaceButtons[faceIndex] is DiceFaceUI face)
        {
            face.UpdateVisual(upgrade);
        }
    }

    private void RemoveItemFromQueue(ShopItem item)
    {
        var tempList = new List<ShopItem>(_purchaseQueue);
        tempList.Remove(item);
        _purchaseQueue = new Queue<ShopItem>(tempList);

        for (int i = _queuedItemButtons.Count - 1; i >= 0; i--)
        {
            if (_queuedItemButtons[i] is QueuedItemUI queuedUI &&
                queuedUI.GetItem().Id == item.Id)
            {
                _queuedItemButtons[i].QueueFree();
                _queuedItemButtons.RemoveAt(i);
                break;
            }
        }
    }

    private void UpdateQueuedItemVisuals()
    {
        foreach (var node in _queuedItemButtons)
        {
            if (node is not QueuedItemUI queuedUI) continue;
            bool isSelected = _isItemSelected && queuedUI.GetItem().Id == _selectedQueuedItem.Id;
            queuedUI.SetHighlight(isSelected);
        }
    }

    // PORT NOTE: Unity's `foreach (Transform child in parent)` iterated children
    // by transform. In Godot we walk `Node.GetChildren()` and QueueFree each one.
    // Doing it in reverse avoids any reordering surprise mid-iteration.
    private static void ClearChildren(Node container)
    {
        var children = container.GetChildren();
        for (int i = children.Count - 1; i >= 0; i--)
        {
            children[i].QueueFree();
        }
    }
}
