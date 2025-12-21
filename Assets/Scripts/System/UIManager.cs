using System.Linq;
using Data;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

namespace System
{
    public class UIManager : MonoBehaviour
    {
        public GameObject goldCounterScreen;
        private TextMeshProUGUI goldCounterText;

        public GameObject enemyHealthPanel;
        private TextMeshProUGUI enemyHealthCounter;
        private TextMeshProUGUI enemyShieldCounter;
        private Slider enemyHealthSlider;
        private Slider enemyShieldSlider;
        
        //Player Health/Shield
        public GameObject playerHealthPanel;
        private TextMeshProUGUI playerHealthCounter;
        private TextMeshProUGUI playerShieldCounter;
        private Slider playerHealthSlider;
        private Slider playerShieldSlider;
        
        //Enemy Attack
        public GameObject enemyAttackPanel;
        private TextMeshProUGUI singleDamageAmount;
        internal Image singleDamageTypeImage;
        private TextMeshProUGUI doubleOneDamageAmount;
        private Image doubleOneDamageTypeImage;
        private TextMeshProUGUI doubleTwoDamageAmount;
        private Image doubleTwoDamageTypeImage;

        //EnemyAbilityImages
        public  Image physicalSymbol;
        public Image magicSymbol;
        
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<UIManager>();
                return _instance;
            }
        }

        public void Awake()
        {
            _instance = this;
            
            goldCounterText = goldCounterScreen.GetComponentsInChildren<TextMeshProUGUI>()[0];
            //Health/Shield bars
            enemyHealthCounter = enemyHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("health"));
            enemyShieldCounter = enemyHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("shield"));
            enemyHealthSlider = enemyHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("healthbar"));
            enemyShieldSlider = enemyHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("shieldbar"));
            playerHealthCounter = playerHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("health"));
            playerShieldCounter = playerHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("shield"));
            playerHealthSlider = playerHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("healthbar"));
            playerShieldSlider = playerHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("shieldbar"));
            
            //EnemyAttack
            singleDamageAmount = enemyAttackPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("singleDamageAmount"));
            singleDamageTypeImage = enemyAttackPanel.GetComponentsInChildren<Image>().ToList().Find(x => x.name.Contains("singleType"));
            doubleOneDamageAmount = enemyAttackPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("doubleOneDamageAmount"));
            doubleOneDamageTypeImage = enemyAttackPanel.GetComponentsInChildren<Image>().ToList().Find(x => x.name.Contains("doubleOneType"));
            doubleTwoDamageAmount = enemyAttackPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("doubleTwoDamageAmount"));
            doubleTwoDamageTypeImage = enemyAttackPanel.GetComponentsInChildren<Image>().ToList().Find(x => x.name.Contains("doubleTwoType"));

            clearEnemyAttackPanel();
        }

        public void updateGoldCounter(float newValue)
        {
            if (goldCounterText)
            {
                int intVal = (int)newValue;
                goldCounterText.text = intVal.ToString(); 
            }
            else
            {
                Debug.Log("gold counter text not initialized properly disabled");
            }
        }

        public void updateEnemyHealthValues(int newValue, int totalHealth)
        {
            if (enemyHealthCounter != null)
            {
                enemyHealthCounter.text = newValue.ToString();
            }
            else
            {
                Debug.Log("enemy health counter text not initialized properly");
            }
            
            if (enemyHealthSlider != null)
            {
                enemyHealthSlider.maxValue = totalHealth;
                enemyHealthSlider.value = newValue;
            }
            else
            {
                Debug.Log("enemy health slider not initialized properly");
            }
        }

        public void updateEnemyShieldValues(int newValue, int totalShield)
        {
            if (enemyShieldCounter != null)
            {
                enemyShieldCounter.text = newValue.ToString();
            }
            else
            {
                Debug.Log("enemy shield counter text not initialized properly");
            }
            
            if (enemyShieldSlider != null)
            {
                enemyShieldSlider.maxValue = totalShield;
                enemyShieldSlider.value = newValue;
            }
            else
            {
                Debug.Log("enemy shield slider not initialized properly");
            }
        }

        public void updatePlayerHealthValues(int newValue, int totalHealth)
        {
            if (playerHealthCounter != null)
            {
                playerHealthCounter.text = newValue.ToString();
            }
            else
            {
                Debug.Log("player health counter text not initialized properly");
            }
            
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = totalHealth;
                playerHealthSlider.value = newValue;
            }
            else
            {
                Debug.Log("player health slider not initialized properly");
            }
        }

        public void updatePlayerShieldValues(int newValue, int totalShield)
        {
            if (playerShieldCounter != null)
            {
                playerShieldCounter.text = newValue.ToString();
            }
            else
            {
                Debug.Log("player shield counter text not initialized properly");
            }
            
            if (playerShieldSlider != null)
            {
                playerShieldSlider.maxValue = totalShield;
                playerShieldSlider.value = newValue;
            }
            else
            {
                Debug.Log("player shield slider not initialized properly");
            }
        }

        public void updateSingleEnemyAttackUI(EnemyAbilityType abilityType, int magnitude)
        {
            switch (abilityType)
            {
                case EnemyAbilityType.Melee:
                    singleDamageAmount.text = $"{magnitude}";
                    singleDamageTypeImage.sprite = physicalSymbol.sprite;
                    break;
                case EnemyAbilityType.Magic:
                    singleDamageAmount.text = $"{magnitude}";
                    singleDamageTypeImage.sprite = magicSymbol.sprite;
                    break;
            }
            singleDamageAmount.enabled = true;
            singleDamageTypeImage.enabled = true;
        }
        
        public void updateDoubleEnemyAttackUI(int physicalDamageMagnitude, int magicDamageMagnitude)
        {
            doubleOneDamageAmount.text = $"{physicalDamageMagnitude}";
            doubleOneDamageTypeImage.sprite = physicalSymbol.sprite;
            
            doubleTwoDamageAmount.text = $"{magicDamageMagnitude}";
            doubleTwoDamageTypeImage.sprite = magicSymbol.sprite;
            
            doubleOneDamageAmount.enabled = true; 
            doubleOneDamageTypeImage.enabled = true; 
            
            doubleTwoDamageAmount.enabled = true; 
            doubleTwoDamageTypeImage.enabled = true; 
        }

        public void clearEnemyAttackPanel()
        {
            singleDamageAmount.enabled = false;
            singleDamageTypeImage.enabled = false;
            doubleOneDamageAmount.enabled = false; 
            doubleOneDamageTypeImage.enabled = false; 
            doubleTwoDamageAmount.enabled = false; 
            doubleTwoDamageTypeImage.enabled = false; 
        }
	
	}
}