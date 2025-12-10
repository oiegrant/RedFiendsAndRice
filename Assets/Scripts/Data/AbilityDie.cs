using UnityEngine;

namespace Data
{
    public class AbilityDie : MonoBehaviour
    {
        internal byte diceId;
        internal Rigidbody rb;
        internal Renderer renderer;
        internal AbilityFaceData[] faceData;
        
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            renderer = GetComponent<Renderer>();
            
            faceData = new AbilityFaceData[6];
            faceData[0] = new AbilityFaceData { faceIndex = 0, abilityType = AbilityType.Coin };
            faceData[1] = new AbilityFaceData { faceIndex = 1, abilityType = AbilityType.Shield };
            faceData[2] = new AbilityFaceData { faceIndex = 2, abilityType = AbilityType.Sword };
            faceData[3] = new AbilityFaceData { faceIndex = 3, abilityType = AbilityType.Sword };
            faceData[4] = new AbilityFaceData { faceIndex = 4, abilityType = AbilityType.Shield };
            faceData[5] = new AbilityFaceData { faceIndex = 5, abilityType = AbilityType.Coin };

        }

        public void initializeFaceData()
        {
            faceData = new AbilityFaceData[6];
            faceData[0] = new AbilityFaceData { faceIndex = 0, abilityType = AbilityType.Coin };
            faceData[1] = new AbilityFaceData { faceIndex = 1, abilityType = AbilityType.Shield };
            faceData[2] = new AbilityFaceData { faceIndex = 2, abilityType = AbilityType.Sword };
            faceData[3] = new AbilityFaceData { faceIndex = 3, abilityType = AbilityType.Sword };
            faceData[4] = new AbilityFaceData { faceIndex = 4, abilityType = AbilityType.Shield };
            faceData[5] = new AbilityFaceData { faceIndex = 5, abilityType = AbilityType.Coin };
        }
    }
}