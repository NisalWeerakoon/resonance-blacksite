"""Build original, rig-ready BLACKSITE hard-surface character prototypes.

Blender 5.2: blender --background --python tools/blender/build_detailed_characters.py
Each character is saved as an editable .blend and exported as a separate rigged
FBX. The geometry is a production prototype, not final animation or UV polish.
"""

import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Blacksite" / "Art" / "Characters" / "Detailed"
OUT.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0


def reset():
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0


def make_collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def make_material(name, rgb, metallic=.0, roughness=.45, glow=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if glow and "Emission Color" in shader.inputs:
        shader.inputs["Emission Color"].default_value = (*rgb, 1)
        shader.inputs["Emission Strength"].default_value = glow
    return mat


def palette(name, accent_rgb):
    return {
        "ceramic": make_material(f"{name}_Ceramic_Armor", (.64, .70, .72), .30, .35),
        "light": make_material(f"{name}_Light_Panel", (.83, .87, .85), .22, .32),
        "gunmetal": make_material(f"{name}_Gunmetal", (.13, .18, .21), .75, .4),
        "joint": make_material(f"{name}_Joint_Rubber", (.025, .035, .045), .08, .8),
        "accent": make_material(f"{name}_Accent", accent_rgb, .35, .25, 2.0),
        "glass": make_material(f"{name}_Dark_Glass", (.01, .025, .035), .4, .13),
    }


def place(obj, col, name, mat):
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    col.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


def bind(obj, arm, bone):
    if arm is None or bone is None:
        return obj
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("Rigid bone deformation", "ARMATURE")
    modifier.object = arm
    return obj


def block(col, name, loc, size, mat, arm=None, bone=None, bevel=.025, rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = place(bpy.context.object, col, name, mat)
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rot is not None:
        obj.rotation_euler = rot
    if bevel:
        mod = obj.modifiers.new("Machined bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 3
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return bind(obj, arm, bone)


def cylinder(col, name, loc, radius, depth, mat, arm=None, bone=None,
             vertices=24, rot=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius,
                                        depth=depth, location=loc)
    obj = place(bpy.context.object, col, name, mat)
    if rot is not None:
        obj.rotation_euler = rot
    bevel = obj.modifiers.new("Edge round", "BEVEL")
    bevel.width = min(radius, depth) * .16
    bevel.segments = 2
    obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return bind(obj, arm, bone)


def sphere(col, name, loc, scale, mat, arm=None, bone=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=loc)
    obj = place(bpy.context.object, col, name, mat)
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return bind(obj, arm, bone)


def torus(col, name, loc, major, minor, mat, arm=None, bone=None, rot=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=10,
                                     major_radius=major, minor_radius=minor,
                                     location=loc)
    obj = place(bpy.context.object, col, name, mat)
    if rot is not None:
        obj.rotation_euler = rot
    return bind(obj, arm, bone)


def make_armature(name, col, width=1, height=1, depth=1):
    data = bpy.data.armatures.new(f"{name}_Skeleton")
    arm = bpy.data.objects.new(f"{name}_Rig", data)
    col.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = {}

    def bone(label, head, tail, parent=None):
        edit = data.edit_bones.new(label)
        edit.head = (head[0] * width, head[1] * depth, head[2] * height)
        edit.tail = (tail[0] * width, tail[1] * depth, tail[2] * height)
        if parent:
            edit.parent = bones[parent]
        bones[label] = edit

    bone("root", (0, 0, 0), (0, 0, .92))
    bone("hips", (0, 0, .92), (0, 0, 1.05), "root")
    bone("spine", (0, 0, 1.05), (0, 0, 1.43), "hips")
    bone("chest", (0, 0, 1.43), (0, 0, 1.63), "spine")
    bone("neck", (0, 0, 1.63), (0, 0, 1.78), "chest")
    bone("head", (0, 0, 1.78), (0, 0, 2.02), "neck")
    for side, suffix in ((-1, "L"), (1, "R")):
        x = side
        bone(f"upper_arm.{suffix}", (x * .42, 0, 1.57),
             (x * .67, 0, 1.20), "chest")
        bone(f"forearm.{suffix}", (x * .67, 0, 1.20),
             (x * .67, -.01, .86), f"upper_arm.{suffix}")
        bone(f"hand.{suffix}", (x * .67, -.01, .86),
             (x * .67, -.02, .69), f"forearm.{suffix}")
        bone(f"thigh.{suffix}", (x * .18, 0, .91),
             (x * .22, 0, .53), "hips")
        bone(f"shin.{suffix}", (x * .22, 0, .53),
             (x * .22, 0, .17), f"thigh.{suffix}")
        bone(f"foot.{suffix}", (x * .22, 0, .17),
             (x * .22, -.15, .08), f"shin.{suffix}")
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    arm.display_type = "WIRE"
    arm.select_set(False)
    return arm


def humanoid(name, width, height, depth, accent_rgb):
    reset()
    col = make_collection(f"{name}_Detailed_Character")
    mats = palette(name, accent_rgb)
    arm = make_armature(name, col, width, height, depth)

    def p(x, y, z):
        return (x * width, y * depth, z * height)

    def d(x, y, z):
        return (x * width, y * depth, z * height)

    def box(label, loc, size, material, bone, bevel=.025, rot=None):
        return block(col, f"{name}_{label}", p(*loc), d(*size), mats[material],
                     arm, bone, bevel, rot)

    def ball(label, loc, size, material, bone):
        return sphere(col, f"{name}_{label}", p(*loc), d(*size), mats[material], arm, bone)

    def round_piece(label, loc, radius, length, material, bone, rotation=None):
        return cylinder(col, f"{name}_{label}", p(*loc), radius * width,
                        length * depth, mats[material], arm, bone, rot=rotation)

    # A dark articulated undersuit sits behind individual rigid armour plates.
    box("Abdomen_Undersuit", (0, 0, 1.18), (.49, .33, .42), "joint", "spine")
    box("Chest_Core", (0, 0, 1.49), (.72, .41, .40), "gunmetal", "chest", .055)
    box("Back_Armour", (0, .25, 1.49), (.66, .16, .39), "ceramic", "chest")
    box("Pelvis_Shell", (0, 0, .94), (.56, .38, .24), "gunmetal", "hips")
    box("Pelvis_Front_Plate", (0, -.22, .93), (.40, .07, .22), "ceramic", "hips")
    box("Neck_Seal", (0, 0, 1.72), (.29, .27, .19), "joint", "neck")
    box("Collar_Front", (0, -.27, 1.69), (.48, .12, .13), "light", "chest")

    # Segmented front armour and cable/vent grooves read at gameplay distance.
    for side, suffix in ((-1, "L"), (1, "R")):
        box(f"Chest_Plate_{suffix}", (side * .19, -.27, 1.52),
            (.33, .13, .28), "light", "chest", .04)
        box(f"Pectoral_Accent_{suffix}", (side * .23, -.345, 1.57),
            (.19, .015, .025), "accent", "chest", .005)
        box(f"Rib_Plate_{suffix}", (side * .20, -.18, 1.29),
            (.27, .10, .16), "ceramic", "spine")
        for rib in range(3):
            box(f"Side_Vent_{suffix}_{rib}",
                (side * .37, -.03, 1.43 - rib * .07),
                (.07, .16, .022), "joint", "chest", .006)
    for strip in range(3):
        box(f"Abdominal_Segment_{strip}", (0, -.19, 1.30 - strip * .085),
            (.31 - strip * .02, .085, .05), "ceramic", "spine", .013)
    round_piece("Chest_Reactor_Rim", (0, -.365, 1.48), .075, .035,
                "gunmetal", "chest", (math.pi / 2, 0, 0))
    round_piece("Chest_Reactor_Light", (0, -.387, 1.48), .049, .025,
                "accent", "chest", (math.pi / 2, 0, 0))

    # Helmet, recessed face and differentiated red/cyan/blue visor hardware.
    ball("Helmet_Shell", (0, 0, 1.88), (.25, .23, .24), "ceramic", "head")
    box("Face_Mask", (0, -.235, 1.86), (.36, .09, .25), "gunmetal", "head", .035)
    box("Visor_Glass", (0, -.291, 1.93), (.30, .025, .08), "glass", "head", .011)
    box("Visor_Light", (0, -.31, 1.93), (.28, .014, .034), "accent", "head", .006)
    box("Jaw_Protector", (0, -.28, 1.75), (.27, .10, .08), "light", "head")
    for side, suffix in ((-1, "L"), (1, "R")):
        round_piece(f"Ear_Rotor_{suffix}", (side * .255, 0, 1.87),
                    .09, .07, "gunmetal", "head", (0, math.pi / 2, 0))
        round_piece(f"Ear_Light_{suffix}", (side * .291, 0, 1.87),
                    .055, .022, "accent", "head", (0, math.pi / 2, 0))

    # Fully separated arms/legs are weighted to a shared humanoid bone scheme.
    for side, suffix in ((-1, "L"), (1, "R")):
        x = side
        box(f"Shoulder_Cap_{suffix}", (x * .50, 0, 1.58),
            (.35, .40, .28), "light", f"upper_arm.{suffix}", .065)
        ball(f"Shoulder_Joint_{suffix}", (x * .43, 0, 1.52),
             (.12, .12, .13), "joint", f"upper_arm.{suffix}")
        box(f"UpperArm_Frame_{suffix}", (x * .66, 0, 1.35),
            (.21, .24, .34), "gunmetal", f"upper_arm.{suffix}")
        box(f"Bicep_Plate_{suffix}", (x * .66, -.16, 1.37),
            (.22, .09, .27), "ceramic", f"upper_arm.{suffix}")
        box(f"Bicep_Stripe_{suffix}", (x * .66, -.215, 1.43),
            (.15, .012, .022), "accent", f"upper_arm.{suffix}", .004)
        ball(f"Elbow_Joint_{suffix}", (x * .67, 0, 1.18),
             (.12, .12, .12), "joint", f"forearm.{suffix}")
        round_piece(f"Elbow_Hinge_{suffix}", (x * .67, 0, 1.18),
                    .075, .23, "gunmetal", f"forearm.{suffix}",
                    (0, math.pi / 2, 0))
        box(f"Forearm_Frame_{suffix}", (x * .67, 0, 1.02),
            (.20, .24, .30), "gunmetal", f"forearm.{suffix}")
        box(f"Forearm_Gauntlet_{suffix}", (x * .67, -.16, 1.01),
            (.23, .11, .29), "ceramic", f"forearm.{suffix}")
        box(f"Gauntlet_Accent_{suffix}", (x * .67, -.221, 1.09),
            (.15, .013, .025), "accent", f"forearm.{suffix}", .004)
        ball(f"Wrist_Seal_{suffix}", (x * .67, 0, .85),
             (.09, .09, .07), "joint", f"hand.{suffix}")
        box(f"Palm_{suffix}", (x * .67, -.02, .77),
            (.17, .16, .16), "gunmetal", f"hand.{suffix}")
        for finger in range(3):
            box(f"Finger_{suffix}_{finger}",
                (x * (.60 + finger * .065), -.08, .665),
                (.045, .09, .15), "light", f"hand.{suffix}", .009)
        box(f"Hip_Guard_{suffix}", (x * .32, -.08, .86),
            (.25, .25, .22), "ceramic", "hips")
        ball(f"Hip_Joint_{suffix}", (x * .18, 0, .89),
             (.13, .14, .13), "joint", f"thigh.{suffix}")
        box(f"Thigh_Frame_{suffix}", (x * .21, 0, .70),
            (.27, .30, .37), "gunmetal", f"thigh.{suffix}")
        box(f"Thigh_Front_{suffix}", (x * .21, -.18, .70),
            (.25, .09, .33), "light", f"thigh.{suffix}")
        box(f"Thigh_Side_Rail_{suffix}", (x * .365, -.01, .72),
            (.045, .21, .28), "ceramic", f"thigh.{suffix}")
        ball(f"Knee_Joint_{suffix}", (x * .22, 0, .52),
             (.12, .12, .11), "joint", f"shin.{suffix}")
        box(f"Knee_Armor_{suffix}", (x * .22, -.18, .52),
            (.24, .12, .19), "ceramic", f"shin.{suffix}")
        box(f"Shin_Frame_{suffix}", (x * .22, 0, .34),
            (.23, .27, .33), "gunmetal", f"shin.{suffix}")
        box(f"Shin_Front_{suffix}", (x * .22, -.16, .33),
            (.23, .09, .32), "light", f"shin.{suffix}")
        for vent in range(3):
            box(f"Shin_Vent_{suffix}_{vent}",
                (x * .22, -.22, .39 - vent * .06),
                (.15, .013, .016), "joint", f"shin.{suffix}", .003)
        ball(f"Ankle_Seal_{suffix}", (x * .22, 0, .17),
             (.10, .11, .08), "joint", f"foot.{suffix}")
        box(f"Foot_Boot_{suffix}", (x * .22, -.12, .10),
            (.26, .45, .18), "gunmetal", f"foot.{suffix}")
        box(f"Toe_Armor_{suffix}", (x * .22, -.30, .12),
            (.25, .20, .12), "light", f"foot.{suffix}")
    return col, arm, mats, p, d, box, ball, round_piece


def build_bastion():
    name = "BASTION"
    col, arm, mats, p, d, box, ball, round_piece = humanoid(
        name, 1.22, 1.04, 1.15, (.95, .045, .025))
    box("Heavy_Backpack", (0, .36, 1.51), (.62, .33, .49),
        "gunmetal", "chest", .07)
    for side, suffix in ((-1, "L"), (1, "R")):
        box(f"High_Shoulder_Bulk_{suffix}", (side * .53, -.02, 1.66),
            (.43, .48, .23), "ceramic", f"upper_arm.{suffix}", .06)
        box(f"Shoulder_Threat_Stripe_{suffix}", (side * .53, -.27, 1.67),
            (.30, .013, .03), "accent", f"upper_arm.{suffix}", .005)
        box(f"Hydraulic_Piston_{suffix}", (side * .40, .23, 1.20),
            (.06, .08, .53), "gunmetal", f"upper_arm.{suffix}")
        box(f"Knee_Reinforcement_{suffix}", (side * .22, -.24, .52),
            (.29, .06, .14), "gunmetal", f"shin.{suffix}")
    box("Shield_Inner_Frame", (-1.04, -.37, 1.11), (.81, .16, 1.49),
        "gunmetal", "forearm.L", .07)
    box("Shield_Ceramic_Face", (-1.04, -.48, 1.11), (.73, .08, 1.39),
        "light", "forearm.L", .065)
    box("Shield_Centre_Spine", (-1.04, -.54, 1.11), (.14, .05, 1.22),
        "gunmetal", "forearm.L", .02)
    for rail, offset in (("L", -.34), ("R", .34)):
        box(f"Shield_Outer_Rail_{rail}", (-1.04 + offset, -.54, 1.11),
            (.06, .08, 1.30), "gunmetal", "forearm.L")
    for index, z in enumerate((.74, 1.05, 1.40)):
        box(f"Shield_Red_Indicator_{index}", (-1.04, -.58, z),
            (.42, .014, .028), "accent", "forearm.L", .004)
    for index in range(4):
        box(f"Shield_Lower_Vent_{index}", (-1.25 + index * .14, -.55, .56),
            (.065, .018, .08), "joint", "forearm.L", .004)
    return col, arm


def build_echo():
    name = "ECHO"
    col, arm, mats, p, d, box, ball, round_piece = humanoid(
        name, .78, .94, .8, (.035, .75, .87))
    box("Acoustic_Backpack", (0, .31, 1.47), (.44, .25, .52),
        "gunmetal", "chest", .05)
    box("Hood_Crown", (0, .02, 2.04), (.45, .35, .11),
        "ceramic", "head", .04)
    box("Hood_Brow", (0, -.24, 1.98), (.43, .10, .10),
        "light", "head", .03)
    for side, suffix in ((-1, "L"), (1, "R")):
        sx = side * .43
        round_piece(f"Dish_Pivot_{suffix}", (sx, 0, 1.89),
                    .12, .14, "gunmetal", "head", (0, math.pi / 2, 0))
        torus(col, f"ECHO_Acoustic_Rim_{suffix}", p(side * .53, 0, 1.89),
              .265 * .78, .033 * .78, mats["ceramic"], arm, "head",
              (0, math.pi / 2, 0))
        torus(col, f"ECHO_Acoustic_Light_{suffix}", p(side * .545, 0, 1.89),
              .205 * .78, .014 * .78, mats["accent"], arm, "head",
              (0, math.pi / 2, 0))
        round_piece(f"Dish_Centre_{suffix}", (side * .55, 0, 1.89),
                    .105, .03, "glass", "head", (0, math.pi / 2, 0))
        for rib in range(3):
            box(f"Backpack_Acoustic_Rib_{suffix}_{rib}",
                (side * .17, .47, 1.35 + rib * .12),
                (.07, .05, .055), "accent", "chest", .006)
        box(f"Quiet_Shoulder_Trim_{suffix}", (side * .53, -.18, 1.57),
            (.19, .02, .025), "accent", f"upper_arm.{suffix}", .005)
    return col, arm


def build_rift():
    name = "RIFT"
    col, arm, mats, p, d, box, ball, round_piece = humanoid(
        name, .87, .93, .86, (.035, .19, .95))
    box("Angular_Hood_Crest", (0, 0, 2.07), (.28, .30, .16),
        "light", "head", .045, (0, -.12, 0))
    left = box("V_Visor_Left", (-.085, -.34, 1.94),
               (.17, .026, .035), "accent", "head", .006)
    right = box("V_Visor_Right", (.085, -.34, 1.94),
                (.17, .026, .035), "accent", "head", .006)
    left.rotation_euler.y = -.38
    right.rotation_euler.y = .38
    box("Asymmetric_Shoulder_Panel", (.65, .12, 1.39),
        (.30, .17, .78), "light", "chest", .045, (0, -.18, -.12))
    box("Panel_Dark_Inner", (.68, .03, 1.35),
        (.22, .04, .60), "gunmetal", "chest", .02, (0, -.18, -.12))
    box("Panel_Blue_Rail", (.78, -.02, 1.50),
        (.035, .04, .35), "accent", "chest", .006, (0, -.18, -.12))
    box("Diagonal_Chest_Strap", (.10, -.37, 1.52),
        (.075, .035, .47), "gunmetal", "chest", .01, (0, -.33, -.4))
    for side, suffix in ((-1, "L"), (1, "R")):
        box(f"Fast_Thigh_Rail_{suffix}", (side * .34, -.12, .69),
            (.035, .12, .29), "accent", f"thigh.{suffix}", .005)
    return col, arm


def build_trace():
    reset()
    name = "TRACE"
    col = make_collection("TRACE_Detailed_Interceptor")
    mats = palette(name, (.02, .90, .32))
    data = bpy.data.armatures.new("TRACE_Mechanical_Skeleton")
    arm = bpy.data.objects.new("TRACE_Rig", data)
    col.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    root = data.edit_bones.new("root")
    root.head, root.tail = (0, 0, 0), (0, 0, 1.55)
    halo = data.edit_bones.new("halo")
    halo.head, halo.tail, halo.parent = (0, 0, 1.78), (0, 0, 2.08), root
    eye = data.edit_bones.new("eye")
    eye.head, eye.tail, eye.parent = (0, -.33, 1.55), (0, -.52, 1.55), root
    for index in range(3):
        angle = index * math.tau / 3
        x, y = .35 * math.cos(angle), .35 * math.sin(angle)
        bone = data.edit_bones.new(f"stabilizer.{index + 1}")
        bone.head, bone.tail, bone.parent = (x, y, 1.40), (x, y, .92), root
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    arm.display_type = "WIRE"
    arm.select_set(False)

    sphere(col, "TRACE_Central_Chassis", (0, 0, 1.55),
           (.47, .37, .34), mats["gunmetal"], arm, "root")
    sphere(col, "TRACE_Ceramic_Top", (0, .03, 1.70),
           (.43, .33, .18), mats["light"], arm, "root")
    sphere(col, "TRACE_Ceramic_Belly", (0, .01, 1.40),
           (.39, .29, .14), mats["ceramic"], arm, "root")
    sphere(col, "TRACE_Eye_Bezel", (0, -.36, 1.56),
           (.20, .065, .18), mats["light"], arm, "eye")
    sphere(col, "TRACE_Eye_Glass", (0, -.42, 1.56),
           (.15, .05, .13), mats["glass"], arm, "eye")
    sphere(col, "TRACE_Eye_Iris", (0, -.468, 1.56),
           (.08, .018, .08), mats["accent"], arm, "eye")
    torus(col, "TRACE_Halo_Armor", (0, 0, 2.04),
          .61, .075, mats["light"], arm, "halo")
    torus(col, "TRACE_Halo_Glow_Channel", (0, 0, 2.04),
          .55, .018, mats["accent"], arm, "halo")
    cylinder(col, "TRACE_Halo_Centre_Mount", (0, 0, 1.89),
             .15, .24, mats["gunmetal"], arm, "halo")
    for rail in range(8):
        angle = rail * math.tau / 8
        x, y = .58 * math.cos(angle), .58 * math.sin(angle)
        obj = block(col, f"TRACE_Halo_Segment_{rail}", (x, y, 2.04),
                    (.11, .07, .09), mats["gunmetal"], arm, "halo", .015)
        obj.rotation_euler.z = angle
    for index in range(3):
        angle = index * math.tau / 3
        x, y = .38 * math.cos(angle), .38 * math.sin(angle)
        bone = f"stabilizer.{index + 1}"
        cylinder(col, f"TRACE_Stabilizer_Hinge_{index + 1}",
                 (x, y, 1.39), .105, .14, mats["gunmetal"], arm, bone)
        blade = block(col, f"TRACE_Tapered_Stabilizer_{index + 1}",
                      (x, y, 1.15), (.15, .24, .44),
                      mats["light"], arm, bone, .045)
        blade.rotation_euler.z = angle
        block(col, f"TRACE_Stabilizer_Stripe_{index + 1}",
              (x, y - .13, 1.18), (.07, .025, .28),
              mats["accent"], arm, bone, .006)
        cylinder(col, f"TRACE_Thruster_{index + 1}",
                 (x, y, .92), .095, .15, mats["gunmetal"], arm, bone)
        cylinder(col, f"TRACE_Thruster_Glow_{index + 1}",
                 (x, y, .83), .07, .035, mats["accent"], arm, bone)
    for side, suffix in ((-1, "L"), (1, "R")):
        block(col, f"TRACE_Eye_Guard_{suffix}",
              (side * .26, -.29, 1.58), (.07, .18, .26),
              mats["ceramic"], arm, "root", .03)
        for vent in range(3):
            block(col, f"TRACE_Side_Vent_{suffix}_{vent}",
                  (side * .425, -.05 + vent * .07, 1.53),
                  (.03, .038, .10), mats["joint"], arm, "root", .006)
    return col, arm


def save_export_preview(name, col, arm):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        if obj.type in {"MESH", "ARMATURE"}:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    blend_path = OUT / f"{name}_detailed.blend"
    fbx_path = OUT / f"{name}_detailed.fbx"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.fbx(filepath=str(fbx_path), use_selection=True,
                             object_types={"ARMATURE", "MESH"},
                             apply_unit_scale=True, add_leaf_bones=False,
                             bake_anim=False, axis_forward="-Z", axis_up="Y")

    camera_data = bpy.data.cameras.new("Preview_Camera")
    camera = bpy.data.objects.new("Preview_Camera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = (3.0, -7.5, 3.0)
    target = Vector((0, 0, 1.25))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 3.4 if name != "TRACE" else 2.9
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.show_cavity = True
    scene.render.resolution_x = 900
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(OUT / f"{name}_detailed_preview.png")
    scene.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print(f"PASS {name}: {len([o for o in col.objects if o.type == 'MESH'])} meshes, "
          f"{len(arm.data.bones)} bones, saved {blend_path.name} and {fbx_path.name}")


def main():
    for name, builder in (("BASTION", build_bastion),
                          ("ECHO", build_echo),
                          ("RIFT", build_rift),
                          ("TRACE", build_trace)):
        col, arm = builder()
        save_export_preview(name, col, arm)


if __name__ == "__main__":
    main()
