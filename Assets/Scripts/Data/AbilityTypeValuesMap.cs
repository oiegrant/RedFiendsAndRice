using System.Collections.Generic;

namespace Data
{
    public class AbilityTypeValuesMap
    {
        public static readonly Dictionary<AbilityType, int> abilityTypeValueMap = new Dictionary<AbilityType, int>
        {
            { AbilityType.Sword, 1 },
            { AbilityType.Gold, 1 },
            { AbilityType.Shield, 1 }
        };
    }
}