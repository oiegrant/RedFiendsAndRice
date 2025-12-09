using System;
using UnityEngine;

namespace Data
{

    //DataLayout
    //FaceData: 1,2,3,4,5,6
    //FaceNormals: Vector3.forward,Vector3.left,Vector3.up,Vector3.down,Vector3.right,Vector3.back
    
    public class MultiDie : MonoBehaviour
    {
        internal byte diceId;
        internal Rigidbody rb;
        internal Renderer renderer;
        internal FaceData[] diceData;
        internal Vector3[] faceNormals;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            renderer = GetComponent<Renderer>();
            Debug.unityLogger.logEnabled = true;
            faceNormals = new Vector3[]
            {
                Vector3.forward,
                Vector3.left,
                Vector3.up,
                Vector3.down,
                Vector3.right,
                Vector3.back
            };
        }
        
        
        
        public enum CubeFace { Top, Bottom, Front, Back, Left, Right }
        //3, 4, 1, 6, 2, 5
    
        public CubeFace GetUpwardFace() //Method doesnt work for 6+ faces
        {
            // Transform world "up" into the cube's local space
            Vector3 localUp = transform.InverseTransformDirection(Vector3.up);
        
            // Find which local axis is most aligned with world up
            float maxDot = Mathf.Abs(localUp.x);
            int axis = 0; // 0=X, 1=Y, 2=Z
        
            if (Mathf.Abs(localUp.y) > maxDot)
            {
                maxDot = Mathf.Abs(localUp.y);
                axis = 1;
            }
        
            if (Mathf.Abs(localUp.z) > maxDot)
            {
                axis = 2;
            }
        
            // Determine positive or negative direction
            float value = axis == 0 ? localUp.x : (axis == 1 ? localUp.y : localUp.z);
            bool positive = value > 0;
        
            Debug.Log("axis:" + axis + ", positive: " + positive );
            
            // Map to face (adjust based on your cube's setup)
            if (axis == 1) return positive ? CubeFace.Top : CubeFace.Bottom;
            if (axis == 2) return positive ? CubeFace.Front : CubeFace.Back;
            CubeFace ret = positive ? CubeFace.Right : CubeFace.Left;
            return ret;
        }

        private void Update()
        {
            Debug.Log("");
            GetUpwardFace();
        }
    }
    
    
    
}