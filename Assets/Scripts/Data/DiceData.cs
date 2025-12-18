using System;
using System.Collections.Generic;

namespace Data
{
    public struct FaceData
    {
        public int faceIndex;
        public int baseValue;
        public int modifierValue;
    }

    public enum AbilityType
    {
        Sword,
        Gold,
        Shield
    }
    
    public struct AbilityFaceData
    {
        public int faceIndex;
        public AbilityType abilityType;
    }

    public struct DiceSet
    {
        public List<AbilityDie> abilityDice;
        public List<MultiDie> multiDice;
    }
    

    public enum EnemySpecialAbilityType
    {
        TakeGold,
        AddShield,
        HealSelf,
        TakeMultiDice
    }

    public struct SpecialAbility
    {
        public EnemySpecialAbilityType type;
        public int magnitude;
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

	public enum EnemyAbilityType {
	Melee,
	Magic
	}
    

}