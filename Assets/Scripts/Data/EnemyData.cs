using System;
using UnityEngine;

namespace Data
{
    public enum AbilityType
    {
        TakeGold,
        AddShield,
        HealSelf,
        TakeMultiDice
    }

    public struct SpecialAbility
    {
        public AbilityType type;
        public int magnitude;
    }

    public struct EnemyData
    {
        internal String enemyName;
        internal int health;
        internal int shield;
        internal int startingPhysicalDamage;
        internal int startingMagicDamage;
        internal SpecialAbility specialType;
        internal float specialChance;
    }






}
