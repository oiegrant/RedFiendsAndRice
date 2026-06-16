namespace RedFiendsAndRice.Data;

public struct EnemyDataLoader
{
    public EnemyData[] data;
}

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
}
