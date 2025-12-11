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
}