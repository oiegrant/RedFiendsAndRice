using System.Collections.Generic;
using Data;
using UnityEngine;

namespace System
{
    public class UpgradeShopUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform abilityUpgradeContainer;
        [SerializeField] private Transform multiDieUpgradeContainer;
        [SerializeField] private Transform upgradeQueueContainer;
        [SerializeField] private Transform diceFacesContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private GameObject queuedItemPrefab;
        [SerializeField] private GameObject diceFacePrefab;
        
        [Header("References")]
        [SerializeField] private RoundUpgradeManager shopManager; // Your shop logic class
        
        private List<ShopItem> currentAbilityUpgrades;
        private List<ShopItem> currentMultiUpgrades;
        private Queue<ShopItem> purchaseQueue = new Queue<ShopItem>();
        private ShopItem selectedQueuedItem;
        private bool isItemSelected = false;
        
        private List<GameObject> queuedItemButtons = new List<GameObject>();
        private List<GameObject> diceFaceButtons = new List<GameObject>();
        
        public void ShowShop(int currentEnemiesDefeated)
        {
            // Get random upgrades
            currentAbilityUpgrades = shopManager.getRandomAbilityUpgradeSet(currentEnemiesDefeated);
            currentMultiUpgrades = shopManager.getRandomMultiUpgradeSet(currentEnemiesDefeated);
            
            // Populate UI
            PopulateAbilityUpgrades();
            PopulateMultiUpgrades();
            PopulateDiceFaces();
            
            shopPanel.SetActive(true);
        }
        
        private void PopulateAbilityUpgrades()
        {
            // Clear existing
            foreach (Transform child in abilityUpgradeContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create UI for each ability upgrade
            foreach (ShopItem item in currentAbilityUpgrades)
            {
                GameObject itemUI = Instantiate(shopItemPrefab, abilityUpgradeContainer);
                ShopItemUI itemScript = itemUI.GetComponent<ShopItemUI>();
                itemScript.Initialize(item, OnPurchaseClicked);
            }
        }
        
        private void PopulateMultiUpgrades()
        {
            // Clear existing
            foreach (Transform child in multiDieUpgradeContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create UI for each multi die upgrade
            foreach (ShopItem item in currentMultiUpgrades)
            {
                GameObject itemUI = Instantiate(shopItemPrefab, multiDieUpgradeContainer);
                ShopItemUI itemScript = itemUI.GetComponent<ShopItemUI>();
                itemScript.Initialize(item, OnPurchaseClicked);
            }
        }
        
        private void PopulateDiceFaces()
        {
            // Clear existing
            foreach (Transform child in diceFacesContainer)
            {
                Destroy(child.gameObject);
            }
            diceFaceButtons.Clear();
            
            // Assuming you have 6 dice faces - adjust as needed
            for (int i = 0; i < 6; i++)
            {
                GameObject faceUI = Instantiate(diceFacePrefab, diceFacesContainer);
                DiceFaceUI faceScript = faceUI.GetComponent<DiceFaceUI>();
                faceScript.Initialize(i, OnDiceFaceClicked);
                diceFaceButtons.Add(faceUI);
            }
        }
        
        private void OnPurchaseClicked(ShopItem item)
        {
            // Add to queue
            purchaseQueue.Enqueue(item);
            
            // Create queued item UI
            GameObject queuedUI = Instantiate(queuedItemPrefab, upgradeQueueContainer);
            QueuedItemUI queuedScript = queuedUI.GetComponent<QueuedItemUI>();
            queuedScript.Initialize(item, OnQueuedItemClicked);
            queuedItemButtons.Add(queuedUI);
            
            // TODO: Deduct currency, disable purchase button, etc.
        }
        
        private void OnQueuedItemClicked(ShopItem item)
        {
            selectedQueuedItem = item;
            isItemSelected = true;
            
            // Visual feedback - highlight selected item
            UpdateQueuedItemVisuals();
        }
        
        private void OnDiceFaceClicked(int faceIndex)
        {
            if (!isItemSelected)
            {
                Debug.Log("Select an upgrade from the queue first!");
                return;
            }
            
            // Apply the upgrade to the selected face
            ApplyUpgradeToFace(selectedQueuedItem, faceIndex);
            
            // Remove from queue
            RemoveItemFromQueue(selectedQueuedItem);
            
            // Clear selection
            isItemSelected = false;
            selectedQueuedItem = default;
            
            UpdateQueuedItemVisuals();
        }
        
        private void ApplyUpgradeToFace(ShopItem upgrade, int faceIndex)
        {
            // TODO: Your game logic to apply the upgrade
            Debug.Log($"Applied {upgrade.displayName} to dice face {faceIndex}");
            
            // Update dice face visual
            diceFaceButtons[faceIndex].GetComponent<DiceFaceUI>().UpdateVisual(upgrade);
        }
        
        private void RemoveItemFromQueue(ShopItem item)
        {
            // Remove from queue (requires converting to list temporarily)
            var tempList = new List<ShopItem>(purchaseQueue);
            tempList.Remove(item);
            purchaseQueue = new Queue<ShopItem>(tempList);
            
            // Remove corresponding UI element
            for (int i = queuedItemButtons.Count - 1; i >= 0; i--)
            {
                QueuedItemUI queuedUI = queuedItemButtons[i].GetComponent<QueuedItemUI>();
                if (queuedUI.GetItem().id == item.id)
                {
                    Destroy(queuedItemButtons[i]);
                    queuedItemButtons.RemoveAt(i);
                    break;
                }
            }
        }
        
        private void UpdateQueuedItemVisuals()
        {
            foreach (GameObject queuedButton in queuedItemButtons)
            {
                QueuedItemUI queuedUI = queuedButton.GetComponent<QueuedItemUI>();
                bool isSelected = isItemSelected && queuedUI.GetItem().id == selectedQueuedItem.id;
                queuedUI.SetHighlight(isSelected);
            }
        }
}
}