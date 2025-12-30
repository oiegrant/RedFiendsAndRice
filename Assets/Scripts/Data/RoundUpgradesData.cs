namespace Data
{
    public struct ShopItem
    {
        internal int id;
        internal string displayName;
        internal ItemType type;
        internal int cost;
        internal int unlockAtEnemyCount;
        internal bool isUnlocked; // should be true unless it is an upgrade variant
        internal int upgradesTo;
    }
    
    internal enum ItemType
    {
        Ability,
        MultiDie
    }
}