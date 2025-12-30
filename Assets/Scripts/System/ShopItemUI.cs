using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace System
{
    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TextMeshProUGUI purchaseButtonText;
    
        private ShopItem item;
        private System.Action<ShopItem> onPurchaseCallback;
    
        public void Initialize(ShopItem shopItem, System.Action<ShopItem> purchaseCallback)
        {
            item = shopItem;
            onPurchaseCallback = purchaseCallback;
        
            nameText.text = item.displayName;
            costText.text = $"Cost: {item.cost}";
            purchaseButtonText.text = "Purchase";
        
            purchaseButton.onClick.AddListener(OnPurchaseClick);
        }
    
        private void OnPurchaseClick()
        {
            onPurchaseCallback?.Invoke(item);
            purchaseButton.interactable = false; // Disable after purchase
        }
    }
    
    public class QueuedItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
    
        private ShopItem item;
        private System.Action<ShopItem> onSelectCallback;
    
        public void Initialize(ShopItem shopItem, System.Action<ShopItem> selectCallback)
        {
            item = shopItem;
            onSelectCallback = selectCallback;
        
            nameText.text = item.displayName;
            selectButton.onClick.AddListener(OnSelectClick);
        }
    
        private void OnSelectClick()
        {
            onSelectCallback?.Invoke(item);
        }
    
        public void SetHighlight(bool highlighted)
        {
            backgroundImage.color = highlighted ? highlightColor : normalColor;
        }
    
        public ShopItem GetItem()
        {
            return item;
        }
    }
    
    public class DiceFaceUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI faceIndexText;
        [SerializeField] private TextMeshProUGUI upgradesText;
        [SerializeField] private Button faceButton;
    
        private int faceIndex;
        private System.Action<int> onClickCallback;
        private List<string> appliedUpgrades = new List<string>();
    
        public void Initialize(int index, System.Action<int> clickCallback)
        {
            faceIndex = index;
            onClickCallback = clickCallback;
        
            faceIndexText.text = $"Face {index + 1}";
            UpdateUpgradesDisplay();
        
            faceButton.onClick.AddListener(OnFaceClick);
        }
    
        private void OnFaceClick()
        {
            onClickCallback?.Invoke(faceIndex);
        }
    
        public void UpdateVisual(ShopItem appliedUpgrade)
        {
            appliedUpgrades.Add(appliedUpgrade.displayName);
            UpdateUpgradesDisplay();
        }
    
        private void UpdateUpgradesDisplay()
        {
            if (appliedUpgrades.Count == 0)
            {
                upgradesText.text = "No upgrades";
            }
            else
            {
                upgradesText.text = string.Join("\n", appliedUpgrades);
            }
        }
    }
}