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
        }

    }
    
    
    
}