"""Build four original BLACKSITE hard-surface character blockouts in Blender.

Run from the repository root:
  blender --background --python tools/blender/build_character_blockouts.py

Requires Blender 5.2 or later. Generated .blend and .fbx files are intentionally
separate from this source script and must be inspected in Blender and Unity.
"""

import math
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Blacksite" / "Art" / "Characters" / "Blockouts"
OUT.mkdir(parents=True, exist_ok=True)


def material(name, color, metallic=0.0, roughness=0.5):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    if principled:
        principled.inputs["Base Color"].default_value = (*color, 1.0)
        principled.inputs["Metallic"].default_value = metallic
        principled.inputs["Roughness"].default_value = roughness
    return mat


ARMOR = material("BS_Charcoal_Armor", (0.10, 0.12, 0.14), 0.65)
STEEL = material("BS_Brushed_Steel", (0.30, 0.36, 0.40), 0.8)
RUBBER = material("BS_Black_Rubber", (0.035, 0.045, 0.05), 0.0, 0.8)
RED = material("BS_Bastion_Red", (0.8, 0.05, 0.04), 0.25)
GREEN = material("BS_Trace_Green", (0.03, 0.8, 0.35), 0.2)
CYAN = material("BS_Echo_Cyan", (0.02, 0.65, 0.8), 0.2)
BLUE = material("BS_Rift_Blue", (0.04, 0.18, 0.9), 0.2)


def collection(name):
    result = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(result)
    return result


def place(obj, target, name, mat):
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    target.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


def cube(target, name, location, scale, mat, bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = place(bpy.context.object, target, name, mat)
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Hard surface bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        obj.modifiers.new("Weighted face normals", "WEIGHTED_NORMAL")
    return obj


def cylinder(target, name, location, radius, depth, mat, vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                        location=location)
    return place(bpy.context.object, target, name, mat)


def sphere(target, name, location, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=location)
    obj = place(bpy.context.object, target, name, mat)
    obj.scale = scale
    return obj


def make_bastion():
    col = collection("BASTION_Original_Blockout")
    x = -3.0
    cube(col, "BASTION_Torso_Armour", (x, 0, 1.36), (0.85, 0.48, 0.72), ARMOR, 0.09)
    cube(col, "BASTION_Waist", (x, 0, 0.92), (0.55, 0.38, 0.28), RUBBER)
    cube(col, "BASTION_Helmet", (x, 0, 1.88), (0.47, 0.43, 0.32), STEEL)
    cube(col, "BASTION_Visor", (x, -0.225, 1.91), (0.35, 0.025, 0.06), RED, 0.01)
    for side, suffix in ((-1, "L"), (1, "R")):
        cube(col, f"BASTION_Shoulder_{suffix}", (x + side * 0.60, 0, 1.58),
             (0.42, 0.52, 0.30), STEEL)
        cube(col, f"BASTION_Arm_{suffix}", (x + side * 0.62, 0, 1.15),
             (0.26, 0.31, 0.60), ARMOR)
        cube(col, f"BASTION_Leg_{suffix}", (x + side * 0.25, 0, 0.49),
             (0.29, 0.34, 0.83), ARMOR)
        cube(col, f"BASTION_Boot_{suffix}", (x + side * 0.25, -0.10, 0.13),
             (0.32, 0.52, 0.25), STEEL)
    cube(col, "BASTION_Shield_Main", (x - 0.95, -0.32, 1.08),
         (0.68, 0.18, 1.35), STEEL, 0.08)
    cube(col, "BASTION_Shield_Red_Bar", (x - 0.95, -0.43, 1.47),
         (0.48, 0.02, 0.07), RED, 0.01)
    return col


def make_trace():
    col = collection("TRACE_Original_Blockout")
    x = 3.0
    sphere(col, "TRACE_Core_Shell", (x, 0, 1.52), (0.45, 0.35, 0.32), ARMOR)
    sphere(col, "TRACE_Eye_Lens", (x, -0.35, 1.55), (0.17, 0.05, 0.16), GREEN)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.58, minor_radius=0.08,
                                     location=(x, 0, 1.93))
    place(bpy.context.object, col, "TRACE_Signature_Halo", STEEL)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.47, minor_radius=0.018,
                                     location=(x, 0, 1.93))
    place(bpy.context.object, col, "TRACE_Halo_Green_Channel", GREEN)
    for index in range(3):
        angle = index * math.tau / 3
        px = x + 0.42 * math.cos(angle)
        py = 0.42 * math.sin(angle)
        arm = cube(col, f"TRACE_Stabilizer_{index + 1}", (px, py, 1.27),
                   (0.16, 0.34, 0.58), STEEL, 0.06)
        arm.rotation_euler.z = angle
        cylinder(col, f"TRACE_Thruster_{index + 1}",
                 (px, py, 0.96), 0.09, 0.12, GREEN)
    return col


def make_echo():
    col = collection("ECHO_Original_Blockout")
    x = -1.0
    cube(col, "ECHO_Narrow_Torso", (x, 0, 1.27), (0.43, 0.32, 0.65), ARMOR)
    cube(col, "ECHO_Sensor_Backpack", (x, 0.25, 1.35), (0.32, 0.18, 0.50), STEEL)
    cube(col, "ECHO_Faceted_Hood", (x, 0, 1.76), (0.38, 0.37, 0.37), STEEL, 0.08)
    cube(col, "ECHO_Cyan_Visor", (x, -0.20, 1.76), (0.27, 0.025, 0.055), CYAN, 0.01)
    for side, suffix in ((-1, "L"), (1, "R")):
        sx = x + side * 0.29
        cube(col, f"ECHO_Arm_{suffix}", (sx, 0, 1.17),
             (0.16, 0.20, 0.65), RUBBER)
        cube(col, f"ECHO_Leg_{suffix}", (x + side * 0.13, 0, 0.48),
             (0.17, 0.25, 0.88), ARMOR)
        dish = cylinder(col, f"ECHO_Acoustic_Dish_{suffix}",
                        (x + side * 0.34, 0, 1.80), 0.30, 0.08, STEEL, 32)
        dish.rotation_euler.y = math.pi / 2
        ring = cylinder(col, f"ECHO_Dish_Cyan_Core_{suffix}",
                        (x + side * 0.39, 0, 1.80), 0.22, 0.085, CYAN, 32)
        ring.rotation_euler.y = math.pi / 2
    return col


def make_rift():
    col = collection("RIFT_Original_Blockout")
    x = 1.0
    cube(col, "RIFT_Lean_Torso", (x, 0, 1.28), (0.48, 0.32, 0.62), ARMOR)
    cube(col, "RIFT_Point_Helmet", (x, 0, 1.75), (0.39, 0.36, 0.37), STEEL, 0.08)
    left_visor = cube(col, "RIFT_V_Visor_L", (x - 0.09, -0.20, 1.77),
                      (0.18, 0.025, 0.045), BLUE, 0.01)
    right_visor = cube(col, "RIFT_V_Visor_R", (x + 0.09, -0.20, 1.77),
                       (0.18, 0.025, 0.045), BLUE, 0.01)
    left_visor.rotation_euler.y = -0.35
    right_visor.rotation_euler.y = 0.35
    for side, suffix in ((-1, "L"), (1, "R")):
        cube(col, f"RIFT_Arm_{suffix}", (x + side * 0.36, 0, 1.17),
             (0.17, 0.22, 0.65), RUBBER)
        cube(col, f"RIFT_Leg_{suffix}", (x + side * 0.14, 0, 0.48),
             (0.19, 0.26, 0.87), ARMOR)
    panel = cube(col, "RIFT_Asymmetric_Shoulder_Plate", (x + 0.37, 0.15, 1.38),
                 (0.35, 0.15, 0.85), STEEL, 0.03)
    panel.rotation_euler.y = -0.18
    cube(col, "RIFT_Blue_Shoulder_Channel", (x + 0.47, 0.06, 1.52),
         (0.05, 0.02, 0.37), BLUE, 0.01)
    return col


def export_collection(col, name, origin_x):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.location.x -= origin_x
        obj.select_set(True)
    if col.objects:
        bpy.context.view_layer.objects.active = col.objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT / f"{name}.fbx"), use_selection=True,
                             apply_unit_scale=True, add_leaf_bones=False)
    for obj in col.objects:
        obj.location.x += origin_x


def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for col in list(bpy.data.collections):
        if col.name.startswith(("BASTION_Original_", "TRACE_Original_",
                                "ECHO_Original_", "RIFT_Original_")):
            bpy.data.collections.remove(col)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bastion = make_bastion()
    trace = make_trace()
    echo = make_echo()
    rift = make_rift()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "BLACKSITE_Character_Blockouts.blend"))
    export_collection(bastion, "BASTION_blockout", -3.0)
    export_collection(trace, "TRACE_blockout", 3.0)
    export_collection(echo, "ECHO_blockout", -1.0)
    export_collection(rift, "RIFT_blockout", 1.0)
    print(f"Saved original blockout sources and FBX exports in {OUT}")


if __name__ == "__main__":
    main()
