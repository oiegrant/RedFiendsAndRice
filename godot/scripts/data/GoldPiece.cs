using Godot;

namespace RedFiendsAndRice.Data;

[GlobalClass]
public partial class GoldPiece : RigidBody3D
{
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
