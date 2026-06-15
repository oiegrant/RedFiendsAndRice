using System.Collections.Generic;

namespace RedFiendsAndRice.Data;

public struct FaceData
{
    public int FaceIndex;
    public int BaseValue;
    public int ModifierValue;
}

public enum AbilityType
{
    Sword,
    Gold,
    Shield,
}

public struct AbilityFaceData
{
    public int FaceIndex;
    public AbilityType AbilityType;
}

public enum EnemySpecialAbilityType
{
    TakeGold,
    AddShield,
    HealSelf,
    TakeMultiDice,
}

public struct SpecialAbility
{
    public EnemySpecialAbilityType Type;
    public int Magnitude;
}

public enum EnemyAbilityType
{
    Melee,
    Magic,
}

public struct DiceSet
{
    public List<AbilityDie> AbilityDice;
    public List<MultiDie> MultiDice;
}
