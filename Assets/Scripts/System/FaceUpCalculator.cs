using UnityEngine;

namespace System
{
    public class FaceUpCalculator
    {
         // Define your face normals in the inspector or in code
    public Vector3[] faceNormals;
    
    // Optional: map face indices to actual die numbers
    public int[] faceNumbers;
    
    void Start()
    {
        // Example: Setup for a d6 (cube)
        // faceNormals = new Vector3[]
        // {
        //     Vector3.up,      // Face 0: Top (number 1)
        //     Vector3.down,    // Face 1: Bottom (number 6)
        //     Vector3.forward, // Face 2: Front (number 2)
        //     Vector3.back,    // Face 3: Back (number 5)
        //     Vector3.right,   // Face 4: Right (number 3)
        //     Vector3.left     // Face 5: Left (number 4)
        // };
        // faceNumbers = new int[] { 1, 6, 2, 5, 3, 4 };
    }
    
    public int GetUpwardFace(GameObject go)
    {
        if (faceNormals == null || faceNormals.Length == 0)
        {
            Debug.LogError("Face normals not defined!");
            return -1;
        }
        
        // Transform world up into die's local space
        Vector3 localUp = go.transform.InverseTransformDirection(Vector3.up);
        
        // Find which face normal is most aligned with up
        int upFaceIndex = 0;
        float maxDot = -1f;
        
        for (int i = 0; i < faceNormals.Length; i++)
        {
            float dot = Vector3.Dot(localUp, faceNormals[i].normalized);
            if (dot > maxDot)
            {
                maxDot = dot;
                upFaceIndex = i;
            }
        }
        
        return upFaceIndex;
    }
    
    public void PrintUpwardFace(GameObject go)
    {
        int faceIndex = GetUpwardFace(go);
        
        if (faceIndex == -1)
        {
            Debug.Log("Error determining upward face");
            return;
        }
        
        // If you have face numbers mapped, print the actual die number
        if (faceNumbers != null && faceIndex < faceNumbers.Length)
        {
            Debug.Log($"Face {faceIndex} is up (Die shows: {faceNumbers[faceIndex]})");
        }
        else
        {
            Debug.Log($"Face {faceIndex} is up");
        }
    }
    

    }
}