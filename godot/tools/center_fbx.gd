@tool
extends EditorScenePostImport

# Post-import processing for Unity-origin FBX assets.
#
# 1. CENTERING: zero every Node3D's transform in the imported scene (FBX
#    inner-node offset baked at export time from Blender world coords).
# 2. MATERIAL AUTO-APPLY: walks each MeshInstance3D's surfaces and finds
#    sibling PNGs by Unity's `<fbx>_<material>_<channel>.png` convention.
#
# Search direction is texture-first: we build an index of available
# materials by scanning the FBX's directory (and `textures/` subdir),
# then match those to each surface. This handles the common case where
# `surface_get_name(i)` returns the mesh-primitive name (e.g. "ashtray"),
# NOT the material name (e.g. "DefaultMaterial") — which is what the PNG
# files are actually keyed by.
#
# Note: roughness is NOT auto-assigned. Unity's MetallicSmoothness encodes
# smoothness (= 1 - roughness) in alpha; wiring it as-is to Godot's roughness
# inverts it. Set roughness manually per-material.

const TEX_ALBEDO := "AlbedoTransparency"
const TEX_NORMAL := "Normal"
const TEX_METALLIC := "MetallicSmoothness"
const CHANNELS := [TEX_ALBEDO, TEX_NORMAL, TEX_METALLIC]


func _post_import(scene: Node) -> Node:
	var root_name := String(scene.name)
	if root_name.is_empty():
		scene.name = "FbxRoot"
		root_name = "FbxRoot"
	print("[center_fbx] === ", root_name, " ===")
	_ensure_names(scene, "node")

	var zeroed: int = _flatten_inner_offsets(scene)
	print("[center_fbx] reset ", zeroed, " transform(s)")

	var source_file: String = get_source_file()
	var source_dir: String = source_file.get_base_dir()
	var fbx_basename: String = source_file.get_file().get_basename()
	var idx: Dictionary = _index_textures(source_dir, fbx_basename)
	print("[center_fbx] textures indexed: ", idx.keys())
	var applied: int = _apply_materials(scene, idx)
	print("[center_fbx] applied ", applied, " material(s)")

	return scene


func _ensure_names(node: Node, fallback_prefix: String) -> void:
	var i := 0
	for child in node.get_children():
		if String(child.name).is_empty():
			child.name = "%s_%d" % [fallback_prefix, i]
		_ensure_names(child, fallback_prefix)
		i += 1


func _flatten_inner_offsets(node: Node) -> int:
	var count := 0
	if node is Node3D:
		var n3 := node as Node3D
		if n3.transform != Transform3D.IDENTITY:
			n3.transform = Transform3D.IDENTITY
			count += 1
	for child in node.get_children():
		count += _flatten_inner_offsets(child)
	return count


# Returns Dictionary[material_name -> Dictionary[channel -> texture_path]].
# Scans source_dir and source_dir/textures/. Material name is inferred as
# the last underscore-separated segment of the filename stem before the
# channel suffix:
#   "ashtray_DefaultMaterial_AlbedoTransparency.png" -> "DefaultMaterial"
#   "d6_d6f1_AlbedoTransparency.png" -> "d6f1"
#
# When source_dir contains peer FBXs (e.g. models/accessories/ holds
# ashtray.fbx + cigarette.fbx + coffeemug.fbx), only PNGs prefixed with
# `<fbx_basename>_` are indexed from that flat dir — otherwise every
# accessory's textures would collide under the shared material name
# "DefaultMaterial". The textures/ subdir is treated as dedicated to this
# FBX (the pattern in models/d6/textures/) and never filtered.
func _index_textures(source_dir: String, fbx_basename: String) -> Dictionary:
	var idx: Dictionary = {}
	var require_prefix: bool = _has_peer_fbx(source_dir, fbx_basename)
	var prefix: String = "%s_" % fbx_basename

	# Flat source_dir: filter by prefix when peers exist.
	var dir: DirAccess = DirAccess.open(source_dir)
	if dir != null:
		dir.list_dir_begin()
		var file: String = dir.get_next()
		while file != "":
			if not dir.current_is_dir() and file.ends_with(".png"):
				if not require_prefix or file.begins_with(prefix):
					_index_file(idx, source_dir, file)
			file = dir.get_next()

	# textures/ subdir: trust everything in it (the d6 pattern).
	var tex_dir: String = source_dir.path_join("textures")
	var tdir: DirAccess = DirAccess.open(tex_dir)
	if tdir != null:
		tdir.list_dir_begin()
		var tfile: String = tdir.get_next()
		while tfile != "":
			if not tdir.current_is_dir() and tfile.ends_with(".png"):
				_index_file(idx, tex_dir, tfile)
			tfile = tdir.get_next()

	return idx


func _has_peer_fbx(source_dir: String, fbx_basename: String) -> bool:
	var dir: DirAccess = DirAccess.open(source_dir)
	if dir == null:
		return false
	dir.list_dir_begin()
	var file: String = dir.get_next()
	while file != "":
		if not dir.current_is_dir() and file.ends_with(".fbx"):
			var other: String = file.get_basename()
			if other != fbx_basename:
				return true
		file = dir.get_next()
	return false


func _index_file(idx: Dictionary, dir_path: String, filename: String) -> void:
	for ch in CHANNELS:
		var suffix := "_%s.png" % ch
		if not filename.ends_with(suffix):
			continue
		var stem: String = filename.substr(0, filename.length() - suffix.length())
		var parts := stem.split("_")
		if parts.size() < 1:
			return
		var mat_name: String = parts[parts.size() - 1]
		if mat_name.is_empty():
			return
		if not idx.has(mat_name):
			idx[mat_name] = {}
		idx[mat_name][ch] = dir_path.path_join(filename)
		return


func _apply_materials(node: Node, idx: Dictionary) -> int:
	var applied := 0
	if node is MeshInstance3D:
		applied += _apply_to_mesh_instance(node as MeshInstance3D, idx)
	for child in node.get_children():
		applied += _apply_materials(child, idx)
	return applied


func _apply_to_mesh_instance(mi: MeshInstance3D, idx: Dictionary) -> int:
	var mesh: Mesh = mi.mesh
	if mesh == null or idx.is_empty():
		return 0
	var applied := 0
	var mat_keys: Array = idx.keys()
	var surface_count := mesh.get_surface_count()
	for i in range(surface_count):
		var matched_key: String = _match_surface_to_material(mesh, i, idx, mat_keys, surface_count)
		if matched_key.is_empty():
			continue
		var mat: StandardMaterial3D = _build_material(idx[matched_key])
		if mat != null:
			mat.resource_name = matched_key
			mi.set_surface_override_material(i, mat)
			applied += 1
	return applied


# Resolution order:
#   1. surface_get_name(i) directly matches an indexed material name
#   2. existing material's resource_name matches an indexed material name
#   3. positional assignment when surface count == index count
#   4. single-material fallback when index has exactly one entry
func _match_surface_to_material(
		mesh: Mesh, i: int, idx: Dictionary, mat_keys: Array, surface_count: int
) -> String:
	var surface_name: String = mesh.surface_get_name(i)
	if not surface_name.is_empty() and idx.has(surface_name):
		return surface_name

	var existing: Material = mesh.surface_get_material(i)
	if existing != null:
		var rn: String = String(existing.resource_name)
		if not rn.is_empty() and idx.has(rn):
			return rn

	if mat_keys.size() == surface_count and i < mat_keys.size():
		return mat_keys[i]

	if mat_keys.size() == 1:
		return mat_keys[0]

	return ""


func _build_material(textures: Dictionary) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	var any_set := false

	if textures.has(TEX_ALBEDO):
		var t: Texture2D = _load_texture(textures[TEX_ALBEDO])
		if t != null:
			mat.albedo_texture = t
			any_set = true

	if textures.has(TEX_NORMAL):
		var t: Texture2D = _load_texture(textures[TEX_NORMAL])
		if t != null:
			mat.normal_enabled = true
			mat.normal_texture = t
			any_set = true

	if textures.has(TEX_METALLIC):
		var t: Texture2D = _load_texture(textures[TEX_METALLIC])
		if t != null:
			mat.metallic_texture = t
			mat.metallic_texture_channel = BaseMaterial3D.TEXTURE_CHANNEL_RED
			mat.metallic = 1.0
			any_set = true

	return mat if any_set else null


func _load_texture(path: String) -> Texture2D:
	if not ResourceLoader.exists(path):
		return null
	var res = load(path)
	return res as Texture2D
