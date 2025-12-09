namespace Data
{
    public struct FaceData
    {
        public int faceIndex;
        public int baseValue;
        public int modifierValue;
    }

    public struct DiceSet
    {
        public AbilityDie[] abilityDice;
        public MultiDie[] multiDice;
    }
}