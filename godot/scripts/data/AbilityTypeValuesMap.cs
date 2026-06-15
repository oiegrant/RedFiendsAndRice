using System.Collections.Generic;

namespace RedFiendsAndRice.Data;

public static class AbilityTypeValuesMap
{
    public static readonly Dictionary<AbilityType, int> Map = new()
    {
        { AbilityType.Sword, 1 },
        { AbilityType.Gold, 1 },
        { AbilityType.Shield, 1 },
    };
}
