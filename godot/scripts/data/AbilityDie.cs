using Godot;

namespace RedFiendsAndRice.Data;

[GlobalClass]
public partial class AbilityDie : RigidBody3D
{
    internal byte DiceId;
    internal AbilityFaceData[] FaceData = null!;

    public override void _Ready()
    {
        FaceData = new AbilityFaceData[6];
        FaceData[0] = new AbilityFaceData { FaceIndex = 0, AbilityType = AbilityType.Gold };
        FaceData[1] = new AbilityFaceData { FaceIndex = 1, AbilityType = AbilityType.Shield };
        FaceData[2] = new AbilityFaceData { FaceIndex = 2, AbilityType = AbilityType.Sword };
        FaceData[3] = new AbilityFaceData { FaceIndex = 3, AbilityType = AbilityType.Sword };
        FaceData[4] = new AbilityFaceData { FaceIndex = 4, AbilityType = AbilityType.Shield };
        FaceData[5] = new AbilityFaceData { FaceIndex = 5, AbilityType = AbilityType.Gold };
    }

    public void SetFaceMaterial(int faceIndex, Material material)
    {
        FindMeshInstance()?.SetSurfaceOverrideMaterial(faceIndex, material);
    }

    public Material? GetFaceMaterial(int faceIndex)
    {
        var mi = FindMeshInstance();
        if (mi == null) return null;
        return mi.GetSurfaceOverrideMaterial(faceIndex) ?? mi.GetActiveMaterial(faceIndex);
    }

    private MeshInstance3D? FindMeshInstance()
    {
        var model = GetNodeOrNull<Node3D>("Model");
        if (model is MeshInstance3D direct) return direct;
        if (model == null) return null;
        foreach (var child in model.GetChildren())
            if (child is MeshInstance3D nested) return nested;
        return null;
    }
}
