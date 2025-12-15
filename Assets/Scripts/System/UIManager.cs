using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;

namespace System
{
    public class UIManager : MonoBehaviour
    {
        public GameObject goldCounterScreen;
        public static TextMeshProUGUI goldCounterText;
        
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
    }
}