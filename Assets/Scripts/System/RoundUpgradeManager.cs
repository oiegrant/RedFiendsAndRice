using System.Collections.Generic;
using Data;

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
            //load in item database
            //create dictionary
        }

        public void getAvailableWeapons()
        {
            
        }

        public void getAvailableItems()
        {
            
        }

        public bool tryPurchaseUpgrade(int id, int availableGold)
        {
            //checks price
            //if true upgrades the playerownedupgrades
            return false;
        }
        
        
    }
}