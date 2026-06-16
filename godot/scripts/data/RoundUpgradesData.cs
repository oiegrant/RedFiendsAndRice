namespace RedFiendsAndRice.Data;

public struct ShopItem
{
    public int Id;
    public string DisplayName;
    public ItemType Type;
    public int Cost;
    public int UnlockAtEnemyCount;
    public bool IsUnlocked; // should be true unless it is an upgrade variant
    public int UpgradesTo;
}

public enum ItemType
{
    Ability,
    MultiDie,
}
