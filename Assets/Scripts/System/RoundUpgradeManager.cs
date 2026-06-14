using System.Collections.Generic;
using Data;
using UnityEngine;

namespace System
{
    public class RoundUpgradeManager
    {
        //This system manages the upgrades purchased through the shop
        //The round manager will pass in #EnemiesDefeated to this system to :
            //keep track of what's purchased / what's available to purchase    
            //Generate a set of random loot for the current storefront round
            //
            
        private Dictionary<int, ShopItem> shopItemDict;
        private List<ShopItem> shopItemList;
        private List<int> playerOwnedUpgrades;
        
        public void initialize()
        {
            //load in item database from json
            //create dictionary
        }

        public List<ShopItem> getAvailableAbilityUpgrades(int currentEnemiesDefeated)
        {
            List<ShopItem> available = new List<ShopItem>();

            foreach (ShopItem item in shopItemList)
            {
                if (item.type == ItemType.Ability &&
                    !playerOwnedUpgrades.Contains(item.id) &&
                    currentEnemiesDefeated >= item.unlockAtEnemyCount &&
                    item.isUnlocked)
                {
                    available.Add(item);
                }
            }

            return available;
        }
        
        public List<ShopItem> getAvailableMultUpgrades(int currentEnemiesDefeated)
        {
            List<ShopItem> available = new List<ShopItem>();
            
            foreach (ShopItem item in shopItemList)
            {
                if (item.type == ItemType.MultiDie &&
                    !playerOwnedUpgrades.Contains(item.id) &&
                    currentEnemiesDefeated >= item.unlockAtEnemyCount &&
                    item.isUnlocked)
                {
                    available.Add(item);
                }
            }
            
            return available;
        }

        public List<ShopItem> getRandomAbilityUpgradeSet(int currentEnemiesDefeated)
        {
            List<ShopItem> finalList = new List<ShopItem>();
            List<ShopItem> availableAbilityUpgrades = getAvailableAbilityUpgrades(currentEnemiesDefeated);
            
            // Shuffle and take up to 3 items
            int count = Mathf.Min(3, availableAbilityUpgrades.Count);
            
            // Create a copy to shuffle without modifying original
            List<ShopItem> shuffled = new List<ShopItem>(availableAbilityUpgrades);
            
            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                ShopItem temp = shuffled[i];
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }
            
            // Take first 'count' items
            for (int i = 0; i < count; i++)
            {
                finalList.Add(shuffled[i]);
            }
            
            return finalList;
        }

        public List<ShopItem> getRandomMultiUpgradeSet(int currentEnemiesDefeated)
        {
            List<ShopItem> finalList = new List<ShopItem>();
            List<ShopItem> availableMultiUpgrades = getAvailableMultUpgrades(currentEnemiesDefeated);
            
            // Shuffle and take up to 3 items
            int count = Mathf.Min(3, availableMultiUpgrades.Count);
            
            // Create a copy to shuffle without modifying original
            List<ShopItem> shuffled = new List<ShopItem>(availableMultiUpgrades);
            
            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                ShopItem temp = shuffled[i];
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }
            
            // Take first 'count' items
            for (int i = 0; i < count; i++)
            {
                finalList.Add(shuffled[i]);
            }
            
            return finalList;
        }

        public bool tryPurchaseUpgrade(int id, int availableGold)
        {
            //checks price
            //if true upgrades the playerownedupgrades
            return false;
        }
        
        
    }
}