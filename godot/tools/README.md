# Unity → Godot scene converter

## Files

- `unity_scene_to_godot.py` — the parser. Standard-library only. Run with:
  ```
  python3 godot/tools/unity_scene_to_godot.py
  ```
- `out/transform_manifest.json` — reference doc: every non-UI scene node with
  its full hierarchy path, Godot-coord position/rotation, scale, and component
  type IDs. Use this as the source of truth if anything in the generated scene
  looks off.
- `out/main_scene.tscn` — first-draft Godot scene. Open it in the Godot editor
  to start finishing the conversion.

## What the parser does

- Reads every `*.meta` file under `Assets/` to build a `guid → asset path` index.
- Parses the Unity scene YAML and walks the Transform/Children graph.
- Skips UI subtrees (anything attached via `RectTransform`, `Canvas`, or
  `CanvasRenderer`).
- Applies the Unity → Godot coordinate flip:
  - position: `(x, y, z) → (x, y, -z)`
  - rotation: Euler `(rx, ry, rz) → (rx, -ry, -rz)`
- Emits each `PrefabInstance` as a `Node3D` placeholder named
  `Prefab_<source-prefab-name>`, with a TODO comment containing the source GUID.
- Emits each `MeshFilter` placement as a `Node3D` placeholder with a TODO for
  the mesh GUID.

## What you finish in the editor

1. **Replace placeholders.** Each `Prefab_<name>` node and each "needs MeshInstance3D"
   comment in `main_scene.tscn` is a manual swap once the corresponding
   prefab/model is ported.
2. **Fix component types.** Camera, lights, and EventSystem all came over as
   plain `Node3D`. Re-type:
   - `CameraLighting/Main_Camera` → `Camera3D`
   - `CameraLighting/Directional_Light_(1)` → `DirectionalLight3D`
   - `EventSystem` → delete (Godot's input system doesn't need this)
3. **Verify coord-flip on rotated objects.** Most scene nodes in this project
   use identity rotations and aren't affected. The camera and dir light DO
   have non-identity rotations — eyeball them in the Godot 3D view and tweak
   if the orientation looks off.
4. **Static colliders.** Unity Quad/Plane primitives used as colliders
   (the `goldcolliders/Quad` and `rollareacollider/Plane` nodes) became plain
   `Node3D`. Wrap each in a `StaticBody3D` + add a `CollisionShape3D` child
   with a `WorldBoundaryShape3D` (or `BoxShape3D` for the gold area walls).

## When to re-run

Re-run the parser if you change the Unity scene. The script is idempotent and
overwrites `out/` each run, so your hand-edits to `main_scene.tscn` will be
lost. Best practice: rename `main_scene.tscn` to something else (e.g.,
`level_main.tscn`) once you start editing it, so a re-run can't clobber your work.
