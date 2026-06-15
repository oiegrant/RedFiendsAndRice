using Godot;

namespace RedFiendsAndRice.System;

// Smoke test for the Godot port: drops a cube and prints the up-facing index
// once it settles. Validates that (1) the .NET toolchain compiles + runs,
// (2) 3D rigidbody physics work, and (3) FaceUpCalculator's transform math
// matches Unity's behavior. Scene is built procedurally so the .tscn stays
// trivial.
public partial class DiceSmokeTest : Node3D
{
	private const float RestLinearThreshold = 0.05f;
	private const float RestAngularThreshold = 0.05f;
	private const float RestHoldSeconds = 0.3f;

	private RigidBody3D _die = null!;
	private Label _label = null!;
	private float _stillTime;
	private bool _reported;

	public override void _Ready()
	{
		BuildCamera();
		BuildLight();
		BuildGround();
		_die = BuildDie();
		_label = BuildLabel();
		DropDie();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_accept"))
		{
			DropDie();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_die.LinearVelocity.Length() < RestLinearThreshold &&
			_die.AngularVelocity.Length() < RestAngularThreshold)
		{
			_stillTime += (float)delta;
			if (_stillTime >= RestHoldSeconds && !_reported)
			{
				int face = FaceUpCalculator.GetUpwardFace(_die);
				GD.Print($"Settled. Face {face + 1} is up.");
				_label.Text = $"Face up: {face + 1}\n(Space to re-roll)";
				_reported = true;
			}
		}
		else
		{
			_stillTime = 0f;
			_reported = false;
		}
	}

	private void DropDie()
	{
		_die.LinearVelocity = Vector3.Zero;
		_die.AngularVelocity = new Vector3(
			(float)GD.RandRange(-8.0, 8.0),
			(float)GD.RandRange(-8.0, 8.0),
			(float)GD.RandRange(-8.0, 8.0));
		_die.GlobalPosition = new Vector3(0, 4f, 0);
		_die.GlobalRotation = new Vector3(
			(float)GD.RandRange(0.0, Mathf.Tau),
			(float)GD.RandRange(0.0, Mathf.Tau),
			(float)GD.RandRange(0.0, Mathf.Tau));
		_stillTime = 0f;
		_reported = false;
		_label.Text = "Rolling...";
	}

	private void BuildCamera()
	{
		var cam = new Camera3D
		{
			Position = new Vector3(0, 3, 6),
		};
		cam.LookAt(Vector3.Zero, Vector3.Up);
		AddChild(cam);
	}

	private void BuildLight()
	{
		var light = new DirectionalLight3D();
		light.Rotation = new Vector3(Mathf.DegToRad(-50), Mathf.DegToRad(40), 0);
		AddChild(light);
	}

	private void BuildGround()
	{
		var ground = new StaticBody3D();
		var groundShape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(20, 1, 20) },
		};
		groundShape.Position = new Vector3(0, -0.5f, 0);
		var groundMesh = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(20, 1, 20) },
		};
		groundMesh.Position = new Vector3(0, -0.5f, 0);
		ground.AddChild(groundShape);
		ground.AddChild(groundMesh);
		AddChild(ground);
	}

	private RigidBody3D BuildDie()
	{
		var die = new RigidBody3D();
		var shape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = Vector3.One },
		};
		die.AddChild(shape);

		// Six face quads instead of a single BoxMesh. A BoxMesh has only one
		// surface, so the 6 d6 face materials need 6 separate MeshInstance3Ds.
		// Order + orientation matches DiceFaceNormals.D6 so face index here
		// agrees with face index everywhere else in the codebase.
		var faces = new (Vector3 Normal, Vector3 Rotation, string MatName)[]
		{
			(Vector3.Forward, new Vector3(0, Mathf.Pi, 0),        "d6f1"),
			(Vector3.Left,    new Vector3(0, -Mathf.Pi / 2, 0),   "d6f2"),
			(Vector3.Up,      new Vector3(-Mathf.Pi / 2, 0, 0),   "d6f3"),
			(Vector3.Down,    new Vector3(Mathf.Pi / 2, 0, 0),    "d6f4"),
			(Vector3.Right,   new Vector3(0, Mathf.Pi / 2, 0),    "d6f5"),
			(Vector3.Back,    Vector3.Zero,                       "d6f6"),
		};

		int loaded = 0;
		foreach (var face in faces)
		{
			var quad = new MeshInstance3D
			{
				Mesh = new QuadMesh { Size = Vector2.One },
				Position = face.Normal * 0.5f,
				Rotation = face.Rotation,
			};
			var mat = GD.Load<Material>($"res://materials/d6/{face.MatName}.tres");
			if (mat != null)
			{
				quad.SetSurfaceOverrideMaterial(0, mat);
				loaded++;
			}
			die.AddChild(quad);
		}

		if (loaded == 0)
		{
			GD.PushWarning("DiceSmokeTest: no d6 face materials found at " +
				"res://materials/d6/. Run tools/extract_face_materials.gd " +
				"(F6 in editor) to generate them.");
		}
		else if (loaded < 6)
		{
			GD.PushWarning($"DiceSmokeTest: only {loaded}/6 d6 face materials loaded.");
		}

		AddChild(die);
		return die;
	}

	private Label BuildLabel()
	{
		var label = new Label
		{
			Position = new Vector2(16, 16),
			Text = "Rolling...",
		};
		label.AddThemeFontSizeOverride("font_size", 24);
		AddChild(label);
		return label;
	}
}
