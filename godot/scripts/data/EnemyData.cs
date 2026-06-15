namespace RedFiendsAndRice.Data;

public struct EnemyDataLoader
{
    public EnemyData[] Data;
}

public struct EnemyData
{
    public string EnemyName;
    public int MaxHealth;
    public int CurrentHealth;
    public int MaxShield;
    public int CurrentShield;
    public int StartingPhysicalDamage;
    public int StartingMagicDamage;
    public bool IsBaseDamageIncrements;
    public int BasePhysicalDamageIncrement;
    public int BaseMagicDamageIncrement;
}
