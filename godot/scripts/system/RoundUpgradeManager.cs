using System.Collections.Generic;
using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.System;

// PORT NOTE: Unity version was a plain class (not MonoBehaviour). Kept that here.
// PORT NOTE: `UnityEngine.Random.Range(int, int)` → `GD.RandRange(int, int)`.
//   Godot's RandRange is inclusive on both ends for integers, vs Unity's exclusive
//   upper bound — adjust if you depend on the half-open semantics.
// PORT NOTE: `UnityEngine.Mathf.Min` → `Godot.Mathf.Min` (same name, different namespace).
public class RoundUpgradeManager
{
    // This system manages the upgrades purchased through the shop.
    // RoundManager passes in #EnemiesDefeated so we can:
    //   - keep track of what's purchased / what's available to purchase
    //   - generate a set of random loot for the current storefront round
    private Dictionary<int, ShopItem> _shopItemDict = new();
    private List<ShopItem> _shopItemList = new();
    private List<int> _playerOwnedUpgrades = new();

    public void Initialize()
    {
        // TODO load item database from JSON (mirror FileUtilities pattern) and
        // populate `_shopItemDict` and `_shopItemList`.
    }

    public List<ShopItem> GetAvailableAbilityUpgrades(int currentEnemiesDefeated)
    {
        var available = new List<ShopItem>();
        foreach (var item in _shopItemList)
        {
            if (item.Type == ItemType.Ability &&
                !_playerOwnedUpgrades.Contains(item.Id) &&
                currentEnemiesDefeated >= item.UnlockAtEnemyCount &&
                item.IsUnlocked)
            {
                available.Add(item);
            }
        }
        return available;
    }

    public List<ShopItem> GetAvailableMultiUpgrades(int currentEnemiesDefeated)
    {
        var available = new List<ShopItem>();
        foreach (var item in _shopItemList)
        {
            if (item.Type == ItemType.MultiDie &&
                !_playerOwnedUpgrades.Contains(item.Id) &&
                currentEnemiesDefeated >= item.UnlockAtEnemyCount &&
                item.IsUnlocked)
            {
                available.Add(item);
            }
        }
        return available;
    }

    public List<ShopItem> GetRandomAbilityUpgradeSet(int currentEnemiesDefeated)
        => PickShuffledSubset(GetAvailableAbilityUpgrades(currentEnemiesDefeated), 3);

    public List<ShopItem> GetRandomMultiUpgradeSet(int currentEnemiesDefeated)
        => PickShuffledSubset(GetAvailableMultiUpgrades(currentEnemiesDefeated), 3);

    // Unity version inlined Fisher-Yates twice; consolidated here.
    private static List<ShopItem> PickShuffledSubset(List<ShopItem> source, int maxCount)
    {
        var shuffled = new List<ShopItem>(source);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = (int)GD.RandRange(0, i); // inclusive on both ends
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
        }
        int count = Mathf.Min(maxCount, shuffled.Count);
        return shuffled.GetRange(0, count);
    }

    public bool TryPurchaseUpgrade(int id, int availableGold)
    {
        // TODO check price; if affordable, add `id` to `_playerOwnedUpgrades`
        // and return true.
        return false;
    }
}
