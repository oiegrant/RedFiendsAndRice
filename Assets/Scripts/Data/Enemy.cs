using System;

namespace Data
{

    public struct EnemyDataLoader
    {
        internal EnemyData[] enemyData;
    }
    
    public struct EnemyData
    {
        internal String enemyName;
        internal int maxHealth;
        internal int currentHealth;
        internal int maxShield;
        internal int currentShield;
        internal int startingPhysicalDamage;
        internal int startingMagicDamage;
        internal bool isBaseDamageIncrements;
        internal int basePhysicalDamageIncrement;
        internal int baseMagicDamageIncrement;
        internal int[] customPhysicalDamageIncrements;
        internal int[] customMagicDamageIncrements;
        internal SpecialAbility specialType;
        internal float specialChance;
    }
    
}