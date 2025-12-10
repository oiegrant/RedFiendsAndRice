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
        internal FaceData[] faceData;
        internal Vector3[] faceNormals;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            renderer = GetComponent<Renderer>();
            Debug.unityLogger.logEnabled = true;
            faceData = new FaceData[6];
            for (int i = 0; i < 6; i++)
            {
                faceData[i] = new FaceData
                {
                    faceIndex = i,
                    baseValue = i + 1,  // 1, 2, 3, 4, 5, 6
                    modifierValue = 0
                };
            }
            
        }

    }
    
    
    
}