using Data;
using UnityEngine;

namespace System
{
    public class FaceUpCalculator
    {
        
    public static int GetUpwardFace(GameObject go)
    {
        
        // Transform world up into die's local space
        Vector3 localUp = go.transform.InverseTransformDirection(Vector3.up);
        // Debug.DrawRay(go.transform.position, localUp * 2f, Color.yellow, 0.1f);
        
        // Find which face normal is most aligned with up
        int upFaceIndex = 0;
        float maxDot = -1f;
        
        for (int i = 0; i < DiceFaceNormals.D6.Length; i++)
        {
            float dot = Vector3.Dot(localUp, DiceFaceNormals.D6[i].normalized);
            if (dot > maxDot)
            {
                maxDot = dot;
                upFaceIndex = i;
            }
        }
        return upFaceIndex;
    }
    
    public static void PrintUpwardFace(GameObject go)
    {
        int faceIndex = GetUpwardFace(go);
        
        if (faceIndex == -1)
        {
            Debug.Log("Error determining upward face");
            return;
        }
        
        Debug.Log($"Face {faceIndex+1} is up");
        
    }
    

    }
}