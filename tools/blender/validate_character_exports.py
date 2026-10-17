"""Re-import all character FBXs in Blender and check handoff basics."""

from pathlib import Path

import bpy
from mathutils import Vector


root = Path(__file__).resolve().parents[2]
directory = root / "Assets" / "Blacksite" / "Art" / "Characters" / "Blockouts"
names = ("BASTION", "TRACE", "ECHO", "RIFT")

for name in names:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    path = directory / f"{name}_blockout.fbx"
    if not path.exists():
        raise RuntimeError(f"Missing export: {path}")
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"{name}: FBX has no meshes")
    if not all(obj.name.startswith(name) for obj in meshes):
        raise RuntimeError(f"{name}: unexpected object names")
    if not all(obj.data.uv_layers for obj in meshes):
        raise RuntimeError(f"{name}: mesh is missing primitive UVs")
    positions = [obj.matrix_world @ Vector(corner) for obj in meshes
                 for corner in obj.bound_box]
    width = max(p.x for p in positions) - min(p.x for p in positions)
    height = max(p.z for p in positions) - min(p.z for p in positions)
    center_x = (max(p.x for p in positions) + min(p.x for p in positions)) / 2
    if not 0.5 < width < 3.0 or not 0.5 < height < 3.0 or abs(center_x) > 0.3:
        raise RuntimeError(f"{name}: unexpected size/origin {width=:.2f} {height=:.2f} {center_x=:.2f}")
    print(f"PASS {name}: {len(meshes)} meshes, width={width:.2f}m, height={height:.2f}m, center_x={center_x:.2f}m")
