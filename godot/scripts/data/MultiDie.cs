using Godot;

namespace RedFiendsAndRice.Data;

// Face layout (matches DiceFaceNormals.D6):
//   faceIndex: 0=forward, 1=left, 2=up, 3=down, 4=right, 5=back
//   baseValue: 1..6 (i + 1)
[GlobalClass]
public partial class MultiDie : RigidBody3D
{
	internal byte DiceId;
	internal FaceData[] FaceData = null!;

	public override void _Ready()
	{
		FaceData = new FaceData[6];
		for (int i = 0; i < 6; i++)
		{
			FaceData[i] = new FaceData
			{
				FaceIndex = i,
				BaseValue = i + 1,
				ModifierValue = 0,
			};
		}
	}
}
