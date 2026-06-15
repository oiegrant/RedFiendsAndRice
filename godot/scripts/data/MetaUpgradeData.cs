namespace RedFiendsAndRice.Data;

// Ported from Unity as a static class rather than an interface-with-static-fields
// (Unity's pattern compiles but is non-idiomatic). Behavior is identical.
public static class MetaUpgradeData
{
    public static int StartingAbilityDieCount = 1;
    public static int StartingMultiDiceCount = 4;
    public static float Crit7 = 1.5f;
    public static float Crit11 = 3f;
    public static int PlayerMaxHealth = 100;
    public static int PlayerMaxShield = 1000;
}
