using UnityEngine;

namespace Data
{
    public class AbilityDie : MonoBehaviour
    {
        internal byte diceId;
        internal Rigidbody rb;
        internal Renderer renderer;
        internal FaceData[] diceData;
        
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            renderer = GetComponent<Renderer>();
        }
    }
}