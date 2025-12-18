using System.Linq;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

namespace System
{
    public class UIManager : MonoBehaviour
    {
        public GameObject goldCounterScreen;
        public static TextMeshProUGUI goldCounterText;

        public GameObject enemyHealthPanel;
        public static TextMeshProUGUI enemyHealthCounter;
        public static TextMeshProUGUI enemyShieldCounter;
        public static Slider enemyHealthSlider;
        public static Slider enemyShieldSlider;
        
        public GameObject playerHealthPanel;
        public static TextMeshProUGUI playerHealthCounter;
        public static TextMeshProUGUI playerShieldCounter;
        public static Slider playerHealthSlider;
        public static Slider playerShieldSlider;
        
        
        
        
        public static UIManager uiManager;
        
        public static UIManager getUIManagerInstance()
        {
            if (!uiManager)
            {
                uiManager = FindFirstObjectByType<UIManager>();
            }
            return uiManager;
        }

        public void Awake()
        {
            goldCounterText = goldCounterScreen.GetComponentsInChildren<TextMeshProUGUI>()[0];
            enemyHealthCounter = enemyHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("health"));
            enemyShieldCounter = enemyHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("shield"));
            enemyHealthSlider = enemyHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("healthbar"));
            enemyShieldSlider = enemyHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("shieldbar"));
            playerHealthCounter = playerHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("health"));
            playerShieldCounter = playerHealthPanel.GetComponentsInChildren<TextMeshProUGUI>().ToList().Find(x => x.name.Contains("shield"));
            playerHealthSlider = playerHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("healthbar"));
            playerShieldSlider = playerHealthPanel.GetComponentsInChildren<Slider>().ToList().Find(x => x.name.Contains("shieldbar"));
        }

        public static void updateGoldCounter(float newValue)
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

        public static void updateEnemyHealthValues(int newValue, int totalHealth)
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

        public static void updateEnemyShieldValues(int newValue, int totalShield)
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

        public static void updatePlayerHealthValues(int newValue, int totalHealth)
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

        public static void updatePlayerShieldValues(int newValue, int totalShield)
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

		//public static void updateSingleEnemyAttackUI(EnemyAbilityType abilityType, int magnitude) {
	
	}
}