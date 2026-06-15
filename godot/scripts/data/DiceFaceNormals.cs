using Godot;

namespace RedFiendsAndRice.Data;

// Face-up detection works by storing each face's normal in the die's LOCAL space.
// At runtime we transform world-up into local space and find the face whose normal
// is most aligned with it.
//
// Coordinate-system note: Godot is right-handed (Z+ points toward camera, so
// Vector3.Forward = (0, 0, -1)). Unity is left-handed (Vector3.forward = (0, 0, 1)).
// The constants below preserve the same axis labels as the Unity version, but if the
// dice mesh's face-to-axis mapping was authored in Unity, you may see the gold/back
// faces swap. Verify with the smoke test and re-label if needed.
public static class DiceFaceNormals
{
    public static readonly Vector3[] D6 =
    {
        Vector3.Forward, // gold
        Vector3.Left,    // shield
        Vector3.Up,      // sword
        Vector3.Down,    // sword
        Vector3.Right,   // shield
        Vector3.Back,    // gold
    };

    public static readonly Vector3[] D8 =
    {
        new Vector3( 1,  1,  1).Normalized(),
        new Vector3( 1,  1, -1).Normalized(),
        new Vector3( 1, -1,  1).Normalized(),
        new Vector3( 1, -1, -1).Normalized(),
        new Vector3(-1,  1,  1).Normalized(),
        new Vector3(-1,  1, -1).Normalized(),
        new Vector3(-1, -1,  1).Normalized(),
        new Vector3(-1, -1, -1).Normalized(),
    };
}
