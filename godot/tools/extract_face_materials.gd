@tool
extends EditorScript

# One-shot tool: loads each dice FBX, walks its MeshInstance3D surfaces, and
# saves the auto-applied override materials (built by center_fbx.gd at import
# time) as standalone .tres files under materials/<die_kind>/.
#
# Why: the import-time materials live only inside the imported .scn cache.
# After this script runs, each face material is a stable, path-addressable
# resource that game code can load with GD.Load<Material>("res://materials/...").
#
# How to run:
#   1. Open this file in the editor (FileSystem → tools/extract_face_materials.gd)
#   2. With the script focused, press F6 (File → Run) OR right-click in
#      ScriptEditor → "Run"
#
# Re-running overwrites the .tres files. Safe to run any time you reimport
# the source FBXs.

const TARGETS := {
	"res://models/d6/d6v2.fbx": "res://materials/d6",
	"res://models/d6ability/d6ability.fbx": "res://materials/d6ability",
	"res://models/gold/gold.fbx": "res://materials/gold",
}


func _run() -> void:
	for fbx_path in TARGETS:
		_extract_from(fbx_path, TARGETS[fbx_path])


func _extract_from(fbx_path: String, out_dir: String) -> void:
	print("[extract_materials] === ", fbx_path, " ===")
	var packed: PackedScene = load(fbx_path)
	if packed == null:
		push_error("Could not load %s" % fbx_path)
		return

	# Ensure the output dir exists.
	var globalized: String = ProjectSettings.globalize_path(out_dir)
	var err := DirAccess.make_dir_recursive_absolute(globalized)
	if err != OK and err != ERR_ALREADY_EXISTS:
		push_error("Could not create %s (err %d)" % [out_dir, err])
		return

	var scene: Node = packed.instantiate()
	var saved: int = _walk_and_save(scene, out_dir)
	scene.queue_free()
	print("[extract_materials] saved ", saved, " material(s) to ", out_dir)


func _walk_and_save(node: Node, out_dir: String) -> int:
	var count := 0
	if node is MeshInstance3D:
		count += _save_overrides(node as MeshInstance3D, out_dir)
	for child in node.get_children():
		count += _walk_and_save(child, out_dir)
	return count


func _save_overrides(mi: MeshInstance3D, out_dir: String) -> int:
	var mesh: Mesh = mi.mesh
	if mesh == null:
		return 0
	var count := 0
	for i in range(mesh.get_surface_count()):
		var mat: Material = mi.get_surface_override_material(i)
		if mat == null:
			mat = mesh.surface_get_material(i)
		if mat == null:
			continue
		var mat_name: String = String(mat.resource_name)
		if mat_name.is_empty():
			mat_name = "surface_%d" % i
		# Deep-duplicate so the saved resource is independent of the FBX scene.
		# Textures stay shared (they're loaded from their own .png paths).
		var copy: Material = mat.duplicate(true) as Material
		copy.resource_name = mat_name
		var path: String = "%s/%s.tres" % [out_dir, mat_name]
		var save_err := ResourceSaver.save(copy, path)
		if save_err == OK:
			print("  saved ", path)
			count += 1
		else:
			push_error("Failed to save %s (err %d)" % [path, save_err])
	return count
