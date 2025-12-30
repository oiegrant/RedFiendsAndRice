using System;

namespace Data
{

    [System.Serializable]
    public struct EnemyDataLoader
    {
        public EnemyData[] data;
    }
    
    [System.Serializable]
    public struct EnemyData
    {
        public string enemyName;
        public int maxHealth;
        public int currentHealth;
        public int maxShield;
        public int currentShield;
        public int startingPhysicalDamage;
        public int startingMagicDamage;
        public bool isBaseDamageIncrements;
        public int basePhysicalDamageIncrement;
        public int baseMagicDamageIncrement;
        // public int[] customPhysicalDamageIncrements;
        // public int[] customMagicDamageIncrements;
        // public SpecialAbility specialType;
        // public float specialChance;
    }
    
}