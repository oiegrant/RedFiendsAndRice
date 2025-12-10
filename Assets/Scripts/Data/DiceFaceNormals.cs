using UnityEngine;

namespace Data
{
    public class DiceFaceNormals
    {
        public static readonly Vector3[] D6 = new Vector3[]
        {
            Vector3.forward,
            Vector3.left,
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.back
        };
    
        // You can add other dice types too
        public static readonly Vector3[] D8 = new Vector3[]
        {
            new Vector3(1, 1, 1).normalized,
            new Vector3(1, 1, -1).normalized,
            new Vector3(1, -1, 1).normalized,
            new Vector3(1, -1, -1).normalized,
            new Vector3(-1, 1, 1).normalized,
            new Vector3(-1, 1, -1).normalized,
            new Vector3(-1, -1, 1).normalized,
            new Vector3(-1, -1, -1).normalized
        };
    }
}