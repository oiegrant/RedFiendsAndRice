using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.System;

// Ported from Unity FaceUpCalculator. Finds which face of a die is pointing up by
// transforming world-up into the die's local space and dot-producting against each
// stored face normal.
public static class FaceUpCalculator
{
    public static int GetUpwardFace(Node3D die)
    {
        Vector3 localUp = die.GlobalBasis.Inverse() * Vector3.Up;

        int upFaceIndex = 0;
        float maxDot = -1f;

        for (int i = 0; i < DiceFaceNormals.D6.Length; i++)
        {
            float dot = localUp.Dot(DiceFaceNormals.D6[i].Normalized());
            if (dot > maxDot)
            {
                maxDot = dot;
                upFaceIndex = i;
            }
        }
        return upFaceIndex;
    }

    public static void PrintUpwardFace(Node3D die)
    {
        int faceIndex = GetUpwardFace(die);
        GD.Print($"Face {faceIndex + 1} is up");
    }
}
