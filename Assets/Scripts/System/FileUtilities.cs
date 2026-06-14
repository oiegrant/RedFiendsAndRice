using System.Collections.Generic;
using System.IO;
using Data;
using UnityEngine;

namespace System
{
    public class FileUtilities : MonoBehaviour
    {

        public static List<EnemyData> loadEnemyDataFile()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "enemydata.json");
    
            List<EnemyData> enemyData = new List<EnemyData>();
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                EnemyDataLoader enemyDataLoader = JsonUtility.FromJson<EnemyDataLoader>(jsonContent);
        
                if (enemyDataLoader.data != null && enemyDataLoader.data.Length > 0)
                {
                    enemyData.Clear();
                    enemyData.AddRange(enemyDataLoader.data);
                }
            }
            else
            {
                Debug.LogError($"JSON file not found: {filePath}");
            }
            return enemyData;
        }
    }
}