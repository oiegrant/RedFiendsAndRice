# Finishing `scenes/main_scene.tscn` in the Godot editor

The port has produced a draft scene at `tools/out/main_scene.tscn`. This
document is the checklist for finishing it in the editor. After this is done,
you have a fully populated 3D scene ready for behavior work.

## 0. One-time prep

1. **Right-click the `models/` folder** in the FileSystem dock → Reimport.
   This re-runs the FBX centering script on all 19 models. Verify in the
   Output panel that each prints `[center_fbx] reset N inner transform(s)`.
2. **Copy the draft scene to its real location:**
   - In FileSystem: drag `tools/out/main_scene.tscn` to `scenes/`
   - Rename to `main_scene.tscn` if not already
   - This protects your hand-edits from being overwritten if the parser is re-run.
3. **Set it as the main scene:** Project → Project Settings → Application → Run →
   Main Scene → browse to `res://scenes/main_scene.tscn`.

## 1. Retype Camera and Light

In `scenes/main_scene.tscn` → Scene tree:

- `CameraLighting/Main_Camera`: Right-click → "Change Type..." → `Camera3D`. The position/rotation_degrees values will be preserved.
- `CameraLighting/Directional_Light_(1)`: Change Type → `DirectionalLight3D`.
- `EventSystem` (root level): Delete. Godot's input system has no equivalent.

## 2. Swap prefab placeholders for real instances

For each `Prefab_*` node in the scene tree:

### Group A — ported `.tscn` wrappers (9 nodes)
These have a corresponding file in `prefabs/`:

| Placeholder | Replace with instance of |
|---|---|
| Prefab_ashtray | `prefabs/ashtray.tscn` |
| Prefab_button | `prefabs/button.tscn` |
| Prefab_cigarette | `prefabs/cigarette.tscn` |
| Prefab_coffeemug | `prefabs/coffeemug.tscn` |
| Prefab_gasnozzle | `prefabs/gasnozzle.tscn` |
| Prefab_goldscreen | `prefabs/goldscreen.tscn` |
| Prefab_memorycard | `prefabs/memorycard.tscn` |
| Prefab_pressuregauge | `prefabs/pressuregauge.tscn` |
| Prefab_scrip (×2) | `prefabs/scrip.tscn` |

**Workflow per node:**
1. Note the placeholder's `position` (in the Inspector).
2. Drag the corresponding `.tscn` file from FileSystem onto the parent node
   (`statics` for most of these).
3. Set the newly instanced node's position to the value you noted.
4. Delete the original `Prefab_*` placeholder.
5. (Faster alternative: select the placeholder, copy its transform, paste it
   onto the instanced one.)

### Group B — direct FBX instances (7 nodes, no `.tscn` wrapper exists)
These reference FBX-as-prefab, which means just drop the FBX in:

| Placeholder | Replace with instance of |
|---|---|
| Prefab_buttoncase | `models/machine/buttoncase.fbx` |
| Prefab_healthbar (×2) | `models/screens/healthbar.fbx` |
| Prefab_infoscreen (×2) | `models/screens/infoscreen.fbx` |
| Prefab_machinebase | `models/machine/machinebase.fbx` |
| Prefab_rollarea | `models/rollarea/rollarea.fbx` |
| Prefab_tabletop | `models/table/tabletop.fbx` |
| Prefab_ticketdispenser | `models/machine/ticketdispenser.fbx` |

Same drag-and-drop workflow. The FBX instances act as PackedScene instances —
they'll render the imported mesh at the position you give them.

## 3. Wrap collider Quads/Planes in StaticBody3D

The `statics/goldcolliders/` and `statics/rollareacollider/` groups contain 8
Quad/Plane placeholder nodes that were Unity colliders. Currently they're
plain Node3D — no physics. To make dice and gold collide with them:

For each node (e.g., `Quad`, `Plane (1)`, etc.):

1. Right-click → Add Child Node → `StaticBody3D`. Name it the same with `_Body` suffix or just re-parent.
2. Add child → `CollisionShape3D`.
3. On the CollisionShape3D: in Inspector → Shape → New `WorldBoundaryShape3D` (for the rollarea floor — an infinite plane) or `BoxShape3D` (for the goldcollider walls).
4. Set the shape's size / normal to match the Unity Plane/Quad orientation.

You don't need MeshInstance3D for these unless you want them visible — Unity's
were invisible colliders too.

## 4. Test

1. Save the scene.
2. Hit Play (F5). The scene should load — empty of behavior, but visually
   showing the table with props.
3. Open the smoke test (`scenes/dice_smoke_test.tscn`) for a quick separate
   sanity check that the centered FBXs render correctly.

## What's not in the scene yet (intentional)

- No `LevelManager` / `RoundManager` running — those scripts have node
  placeholders under `Managers/` but no behaviors execute. You'll write fresh
  behaviors against the populated scene.
- No UI — the parser skipped UI subtrees per the agreed scope.
- The runtime-spawned dice/gold (`d6.tscn`, `d6ability.tscn`, `gold.tscn`)
  aren't placed in the scene — they're instanced at runtime by whatever
  spawning logic you write.

## Reference handles for behavior code

When you start writing behavior, these are the node paths your scripts will
want to grab:

```csharp
var spawns       = GetNode<Node3D>("SpawnPositions");
var goldSpawn    = GetNode<Node3D>("SpawnPositions/gold");
var abilitySpawn = GetNode<Node3D>("SpawnPositions/a1"); // a1..a5
var multiSpawn   = GetNode<Node3D>("SpawnPositions/m1"); // m1..m10, m3 too

var levelMgr    = GetNode<Node3D>("Managers/LevelManager");
var sumUp       = GetNode<Node3D>("Managers/LevelManager/sumuplocation");
var enemyStrike = GetNode<Node3D>("enemyStrikeLocation");
```

These names mirror what `LevelManager.cs` and `RoundManager.cs` referenced
from the Unity inspector, so any logic you port verbatim should find them.
