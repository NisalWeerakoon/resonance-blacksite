"""Re-import detailed FBXs and verify skeletons, mesh UVs, and pose response."""

from pathlib import Path

import bpy


root = Path(__file__).resolve().parents[2]
directory = root / "Assets" / "Blacksite" / "Art" / "Characters" / "Detailed"
expected_bones = {"BASTION": 18, "ECHO": 18, "RIFT": 18, "TRACE": 6}


def evaluated_position(obj):
    graph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(graph)
    mesh = evaluated.to_mesh()
    result = evaluated.matrix_world @ mesh.vertices[0].co.copy()
    evaluated.to_mesh_clear()
    return result


for name, bone_count in expected_bones.items():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    path = directory / f"{name}_detailed.fbx"
    bpy.ops.import_scene.fbx(filepath=str(path))
    rigs = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(rigs) != 1 or len(rigs[0].data.bones) != bone_count:
        raise RuntimeError(f"{name}: expected one {bone_count}-bone rig")
    if len(meshes) < 30:
        raise RuntimeError(f"{name}: only {len(meshes)} meshes")
    if not all(obj.data.uv_layers and obj.vertex_groups for obj in meshes):
        raise RuntimeError(f"{name}: some meshes lack UVs or bone weights")
    if not all(any(mod.type == "ARMATURE" for mod in obj.modifiers) for obj in meshes):
        raise RuntimeError(f"{name}: some meshes are not rigged")

    rig = rigs[0]
    moving_bone = "upper_arm.L" if name != "TRACE" else "stabilizer.1"
    sample = next((obj for obj in meshes if moving_bone in obj.vertex_groups), None)
    if sample is None:
        raise RuntimeError(f"{name}: no mesh bound to {moving_bone}")
    before = evaluated_position(sample)
    rig.pose.bones[moving_bone].rotation_mode = "XYZ"
    rig.pose.bones[moving_bone].rotation_euler.z = .55
    bpy.context.view_layer.update()
    after = evaluated_position(sample)
    moved = (after - before).length
    if moved < .01:
        raise RuntimeError(f"{name}: pose did not deform sample mesh ({moved:.4f}m)")
    print(f"PASS {name}: {len(meshes)} meshes, {bone_count} bones, UVs, weights, pose displacement {moved:.3f}m")
