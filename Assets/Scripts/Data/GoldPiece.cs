using UnityEngine;

namespace Data
{
    public class GoldPiece : MonoBehaviour
    {
        internal Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
    }
}