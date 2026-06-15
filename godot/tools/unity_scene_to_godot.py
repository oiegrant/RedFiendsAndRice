#!/usr/bin/env python3
"""
Unity scene → Godot scene draft converter.

Reads:
  - Assets/ (for all *.meta files, to build a GUID → asset path index)
  - Assets/Scenes/SampleScene.unity

Writes:
  - godot/tools/out/transform_manifest.json   (reference doc, all 3D nodes)
  - godot/tools/out/main_scene.tscn           (first-draft Godot scene)

Skips: UI subtrees (anything reachable via a RectTransform, Canvas, or CanvasRenderer).
Coord flip: Unity is left-handed (Z+ forward); Godot is right-handed (Z+ back).
  position: (x, y, z) -> (x, y, -z)
  rotation: euler (rx, ry, rz) -> (rx, -ry, -rz)

Prefab instances become Node3D placeholders named "Prefab_<prefab-name>". You
swap in actual `instance=ExtResource(...)` references in the Godot editor once
each prefab has been ported to a .tscn.
"""

import json
import math
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

UNITY_ROOT = Path(__file__).resolve().parents[2] / "Assets"
GODOT_ROOT = Path(__file__).resolve().parents[1]
SCENE_FILE = UNITY_ROOT / "Scenes" / "SampleScene.unity"
OUT_DIR = Path(__file__).resolve().parent / "out"

# Unity component type IDs we recognize.
TYPE_GAMEOBJECT = 1
TYPE_TRANSFORM = 4
TYPE_CAMERA = 20
TYPE_MESH_RENDERER = 23
TYPE_MESH_FILTER = 33
TYPE_MESH_COLLIDER = 64
TYPE_BOX_COLLIDER = 65
TYPE_LIGHT = 108
TYPE_MONO_BEHAVIOUR = 114
TYPE_CANVAS_RENDERER = 222
TYPE_CANVAS = 223
TYPE_RECT_TRANSFORM = 224
TYPE_PREFAB_INSTANCE = 1001

UI_TYPES = {TYPE_RECT_TRANSFORM, TYPE_CANVAS, TYPE_CANVAS_RENDERER}


# ---------- GUID index ----------------------------------------------------

def build_guid_index() -> dict[str, Path]:
    """Walk Assets/, read every *.meta, return {guid: asset_path}."""
    index = {}
    for meta in UNITY_ROOT.rglob("*.meta"):
        try:
            with meta.open() as f:
                for line in f:
                    m = re.match(r"^guid:\s*([a-f0-9]+)", line)
                    if m:
                        index[m.group(1)] = meta.with_suffix("")
                        break
        except (OSError, UnicodeDecodeError):
            continue
    return index


# ---------- Scene YAML parser --------------------------------------------

@dataclass
class Block:
    type_id: int
    file_id: int
    body: str
    # parsed fields filled in lazily:
    fields: dict = field(default_factory=dict)


def parse_scene(path: Path) -> dict[int, Block]:
    text = path.read_text()
    # Split on "--- !u!TYPE_ID &FILE_ID" markers.
    parts = re.split(r"^--- !u!(\d+) &(\d+)(?:\s+stripped)?\s*$",
                     text, flags=re.MULTILINE)
    blocks: dict[int, Block] = {}
    i = 1
    while i < len(parts):
        type_id = int(parts[i])
        file_id = int(parts[i + 1])
        body = parts[i + 2]
        blocks[file_id] = Block(type_id=type_id, file_id=file_id, body=body)
        i += 3
    for b in blocks.values():
        b.fields = parse_fields(b.body)
    return blocks


def parse_fields(body: str) -> dict:
    """Lightweight parser tuned for the Unity fields we care about.
    Returns a dict mapping top-level key -> raw string value (right of the colon)
    or list of dicts for sequences.
    """
    out = {}
    lines = body.split("\n")
    # First non-empty line is the type tag, e.g. "GameObject:"
    idx = 0
    while idx < len(lines) and not lines[idx].strip():
        idx += 1
    if idx < len(lines) and lines[idx].rstrip().endswith(":"):
        out["_type_name"] = lines[idx].rstrip()[:-1].strip()
        idx += 1
    # Walk top-level fields (2-space indent)
    while idx < len(lines):
        line = lines[idx]
        if not line.strip():
            idx += 1
            continue
        # Top-level fields are indented exactly 2 spaces, "key: value"
        m = re.match(r"^  (\w+):\s*(.*)$", line)
        if m:
            key = m.group(1)
            val = m.group(2)
            if val:
                out[key] = val.strip()
                idx += 1
            else:
                # Multi-line value: sequence or nested map
                seq, consumed = read_sequence(lines, idx + 1)
                if seq is not None:
                    out[key] = seq
                    idx += 1 + consumed
                else:
                    nested, consumed = read_nested_map(lines, idx + 1)
                    out[key] = nested
                    idx += 1 + consumed
        else:
            idx += 1
    return out


def read_sequence(lines, start_idx):
    """Read a sequence of `  - item` entries. Returns (list, consumed) or (None, 0)."""
    if start_idx >= len(lines) or not re.match(r"^  - ", lines[start_idx]):
        return None, 0
    items = []
    idx = start_idx
    while idx < len(lines) and lines[idx].startswith("  - "):
        item_body = lines[idx][4:]
        sub_lines = [item_body]
        idx += 1
        # Continuation lines for this item: indented 4 spaces, not "  - "
        while idx < len(lines) and lines[idx].startswith("    ") \
                and not lines[idx].startswith("  - "):
            sub_lines.append(lines[idx][4:])
            idx += 1
        items.append("\n".join(sub_lines).strip())
    return items, idx - start_idx


def read_nested_map(lines, start_idx):
    """Read a nested map at 4-space indent. Returns (dict, consumed)."""
    out = {}
    idx = start_idx
    while idx < len(lines) and lines[idx].startswith("    ") \
            and not lines[idx].startswith("  - "):
        m = re.match(r"^    (\w+):\s*(.*)$", lines[idx])
        if m:
            out[m.group(1)] = m.group(2).strip()
        idx += 1
    return out, idx - start_idx


# ---------- Helpers to read scalar refs ----------------------------------

REF_RE = re.compile(
    r"\{fileID:\s*(-?\d+)(?:,\s*guid:\s*([a-f0-9]+))?(?:,\s*type:\s*(\d+))?\}"
)
VEC3_RE = re.compile(r"\{x:\s*([-\d.eE+]+),\s*y:\s*([-\d.eE+]+),\s*z:\s*([-\d.eE+]+)\}")
QUAT_RE = re.compile(
    r"\{x:\s*([-\d.eE+]+),\s*y:\s*([-\d.eE+]+),"
    r"\s*z:\s*([-\d.eE+]+),\s*w:\s*([-\d.eE+]+)\}"
)


def parse_ref(s):
    if not s:
        return None
    m = REF_RE.search(s)
    if not m:
        return None
    return {"fileID": int(m.group(1)), "guid": m.group(2), "type": m.group(3)}


def parse_vec3(s, default=(0.0, 0.0, 0.0)):
    if not s:
        return default
    m = VEC3_RE.search(s)
    if not m:
        return default
    return float(m.group(1)), float(m.group(2)), float(m.group(3))


def parse_quat(s, default=(0.0, 0.0, 0.0, 1.0)):
    if not s:
        return default
    m = QUAT_RE.search(s)
    if not m:
        return default
    return (float(m.group(1)), float(m.group(2)),
            float(m.group(3)), float(m.group(4)))


# ---------- Scene model ---------------------------------------------------

@dataclass
class SceneNode:
    file_id: int
    name: str
    pos: tuple
    rot_quat: tuple   # (qx, qy, qz, qw) in Unity coords
    scale: tuple
    children: list = field(default_factory=list)
    components: list = field(default_factory=list)
    # Tags that affect output:
    is_ui: bool = False
    is_active: bool = True
    # Component-derived hints:
    mesh_ref: dict | None = None      # MeshFilter.m_Mesh
    is_prefab_instance: bool = False
    prefab_source_guid: str | None = None


def build_scene_graph(blocks, guid_index):
    """Returns (roots, by_id, prefab_overrides)."""
    # Index: GameObject id -> components (list of file_ids)
    # Build Transform & RectTransform mapping
    transforms = {fid: b for fid, b in blocks.items()
                  if b.type_id in (TYPE_TRANSFORM, TYPE_RECT_TRANSFORM)}
    gameobjects = {fid: b for fid, b in blocks.items()
                   if b.type_id == TYPE_GAMEOBJECT}

    nodes_by_id: dict[int, SceneNode] = {}
    transform_to_node: dict[int, SceneNode] = {}

    for tfid, tb in transforms.items():
        go_ref = parse_ref(tb.fields.get("m_GameObject", ""))
        if not go_ref or go_ref["fileID"] not in gameobjects:
            continue
        gb = gameobjects[go_ref["fileID"]]
        name = gb.fields.get("m_Name", "").strip().strip('"')
        is_active = gb.fields.get("m_IsActive", "1") == "1"

        pos = parse_vec3(tb.fields.get("m_LocalPosition"))
        rot = parse_quat(tb.fields.get("m_LocalRotation"))
        scl = parse_vec3(tb.fields.get("m_LocalScale"), default=(1.0, 1.0, 1.0))

        node = SceneNode(
            file_id=go_ref["fileID"],
            name=name or f"GameObject_{go_ref['fileID']}",
            pos=pos, rot_quat=rot, scale=scl,
            is_ui=(tb.type_id == TYPE_RECT_TRANSFORM),
            is_active=is_active,
        )
        # Component types attached to this GO
        comp_field = gb.fields.get("m_Component", [])
        if isinstance(comp_field, list):
            for entry in comp_field:
                ref = parse_ref(entry)
                if not ref:
                    continue
                cb = blocks.get(ref["fileID"])
                if not cb:
                    continue
                node.components.append(cb.type_id)
                if cb.type_id == TYPE_MESH_FILTER:
                    node.mesh_ref = parse_ref(cb.fields.get("m_Mesh", ""))
                if cb.type_id == TYPE_CANVAS:
                    node.is_ui = True

        nodes_by_id[node.file_id] = node
        transform_to_node[tfid] = node

    # Wire up parent → children using Transform.m_Children & m_Father.
    roots: list[SceneNode] = []
    for tfid, tb in transforms.items():
        node = transform_to_node.get(tfid)
        if not node:
            continue
        father = parse_ref(tb.fields.get("m_Father", ""))
        if not father or father["fileID"] == 0:
            roots.append(node)
        else:
            parent_node = transform_to_node.get(father["fileID"])
            if parent_node:
                parent_node.children.append(node)

    # PrefabInstance handling: create a placeholder node per PrefabInstance.
    for fid, b in blocks.items():
        if b.type_id != TYPE_PREFAB_INSTANCE:
            continue
        src = parse_ref(b.fields.get("m_SourcePrefab", ""))
        mods = b.fields.get("m_Modification", {})
        if not isinstance(mods, dict):
            mods = {}
        parent_ref = parse_ref(mods.get("m_TransformParent", ""))
        # Pull position overrides if present (read m_Modifications sequence).
        # For first draft we use parent's position if no overrides parsed.
        pos = (0.0, 0.0, 0.0)
        rot = (0.0, 0.0, 0.0, 1.0)
        scl = (1.0, 1.0, 1.0)
        name = "PrefabInstance"
        if src and src["guid"] and src["guid"] in guid_index:
            name = f"Prefab_{guid_index[src['guid']].stem}"
        node = SceneNode(
            file_id=-fid,  # avoid collisions with real GO ids
            name=name, pos=pos, rot_quat=rot, scale=scl,
            is_prefab_instance=True,
            prefab_source_guid=src["guid"] if src else None,
        )
        if parent_ref and parent_ref["fileID"] in transform_to_node:
            transform_to_node[parent_ref["fileID"]].children.append(node)
        else:
            roots.append(node)

    return roots, nodes_by_id


# ---------- Coord flip + emit ---------------------------------------------

def flip_pos(p):
    return (p[0], p[1], -p[2])


def quat_to_euler_deg_godot(q):
    """Convert Unity quaternion -> Godot rotation_degrees (Vector3, YXZ order).
    Includes the LH->RH flip: negate the Y and Z components of the result.
    """
    qx, qy, qz, qw = q
    # Unity rotates as XYZ (Tait-Bryan) intrinsic, but the Quaternion->Euler
    # conversion is order-independent in numeric value. Use ZYX-extrinsic ≡ XYZ-intrinsic:
    sinr_cosp = 2 * (qw * qx + qy * qz)
    cosr_cosp = 1 - 2 * (qx * qx + qy * qy)
    rx = math.atan2(sinr_cosp, cosr_cosp)

    sinp = 2 * (qw * qy - qz * qx)
    sinp = max(-1.0, min(1.0, sinp))
    ry = math.asin(sinp)

    siny_cosp = 2 * (qw * qz + qx * qy)
    cosy_cosp = 1 - 2 * (qy * qy + qz * qz)
    rz = math.atan2(siny_cosp, cosy_cosp)

    # LH -> RH flip
    return (math.degrees(rx), -math.degrees(ry), -math.degrees(rz))


def emit_manifest(roots):
    def walk(node, parent_path):
        path = f"{parent_path}/{node.name}" if parent_path else node.name
        if node.is_ui:
            return []
        entry = {
            "path": path,
            "name": node.name,
            "position": list(flip_pos(node.pos)),
            "rotation_quat_unity": list(node.rot_quat),
            "rotation_degrees_godot": list(quat_to_euler_deg_godot(node.rot_quat)),
            "scale": list(node.scale),
            "is_active": node.is_active,
            "is_prefab_instance": node.is_prefab_instance,
            "prefab_source_guid": node.prefab_source_guid,
            "has_mesh": node.mesh_ref is not None,
            "component_type_ids": node.components,
        }
        out = [entry]
        for c in node.children:
            out.extend(walk(c, path))
        return out
    flat = []
    for r in roots:
        flat.extend(walk(r, ""))
    return flat


def emit_tscn(roots, out_path: Path):
    lines = ["[gd_scene load_steps=1 format=3]", ""]
    # Godot requires exactly one root node with no `parent=` attribute. Insert
    # a synthetic root; everything from the Unity scene becomes its descendant.
    lines.append('[node name="MainScene" type="Node3D"]')
    lines.append("")

    # Track used child names per parent path to avoid sibling name collisions
    # (Unity scenes routinely have duplicate names; Godot .tscn does not allow it).
    used_names: dict[str, set[str]] = {}

    def unique_name(parent_path, base):
        siblings = used_names.setdefault(parent_path, set())
        if base not in siblings:
            siblings.add(base)
            return base
        i = 2
        while f"{base}_{i}" in siblings:
            i += 1
        candidate = f"{base}_{i}"
        siblings.add(candidate)
        return candidate

    def walk(node, parent_path):
        if node.is_ui or not node.is_active:
            return
        node_type = "Node3D"
        base = sanitize_name(node.name)
        name = unique_name(parent_path, base)
        lines.append(f'[node name="{name}" type="{node_type}" parent="{parent_path}"]')
        px, py, pz = flip_pos(node.pos)
        rx, ry, rz = quat_to_euler_deg_godot(node.rot_quat)
        sx, sy, sz = node.scale
        if (px, py, pz) != (0.0, 0.0, 0.0):
            lines.append(f"position = Vector3({px:.6g}, {py:.6g}, {pz:.6g})")
        if not (abs(rx) < 1e-4 and abs(ry) < 1e-4 and abs(rz) < 1e-4):
            lines.append(f"rotation_degrees = Vector3({rx:.6g}, {ry:.6g}, {rz:.6g})")
        if (sx, sy, sz) != (1.0, 1.0, 1.0):
            lines.append(f"scale = Vector3({sx:.6g}, {sy:.6g}, {sz:.6g})")
        if node.is_prefab_instance:
            lines.append(f"# TODO: replace with instance of ported prefab "
                         f"(source guid: {node.prefab_source_guid})")
        if node.mesh_ref:
            lines.append(f"# TODO: attach MeshInstance3D child with mesh "
                         f"(source guid: {node.mesh_ref.get('guid')})")
        lines.append("")
        child_parent = name if parent_path == "." else f"{parent_path}/{name}"
        for c in node.children:
            walk(c, child_parent)

    for r in roots:
        walk(r, ".")
    out_path.write_text("\n".join(lines))


def sanitize_name(name):
    # Godot node names can't contain . : @ / "
    return re.sub(r'[.:@/"\s]+', "_", name).strip("_") or "Node"


# ---------- Main ----------------------------------------------------------

def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    print(f"Reading {SCENE_FILE.relative_to(UNITY_ROOT.parent)}...")
    guid_index = build_guid_index()
    print(f"  indexed {len(guid_index)} GUIDs from .meta files")
    blocks = parse_scene(SCENE_FILE)
    print(f"  parsed {len(blocks)} YAML blocks")

    roots, by_id = build_scene_graph(blocks, guid_index)
    print(f"  built scene graph: {len(roots)} roots, {len(by_id)} game objects")

    manifest = emit_manifest(roots)
    manifest_path = OUT_DIR / "transform_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2))
    print(f"  wrote {manifest_path.relative_to(GODOT_ROOT)} ({len(manifest)} entries)")

    tscn_path = OUT_DIR / "main_scene.tscn"
    emit_tscn(roots, tscn_path)
    print(f"  wrote {tscn_path.relative_to(GODOT_ROOT)}")


if __name__ == "__main__":
    main()
