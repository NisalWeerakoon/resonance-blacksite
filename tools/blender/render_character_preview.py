"""Render a simple proof sheet from the generated Blender source.

Run with: blender --background --python tools/blender/render_character_preview.py -- OUTPUT.png
"""

import sys
from pathlib import Path

import bpy
from mathutils import Vector


root = Path(__file__).resolve().parents[2]
source = root / "Assets" / "Blacksite" / "Art" / "Characters" / "Blockouts" / "BLACKSITE_Character_Blockouts.blend"
output = Path(sys.argv[-1]).resolve()
bpy.ops.wm.open_mainfile(filepath=str(source))

camera_data = bpy.data.cameras.new("Preview_Camera")
camera = bpy.data.objects.new("Preview_Camera", camera_data)
bpy.context.scene.collection.objects.link(camera)
camera.location = (0, -11, 3.5)
target = Vector((0, 0, 1.2))
camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 9.0

scene = bpy.context.scene
scene.camera = camera
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.color_type = "MATERIAL"
scene.display.shading.light = "STUDIO"
scene.display.shading.show_cavity = True
scene.render.resolution_x = 1600
scene.render.resolution_y = 700
scene.render.resolution_percentage = 100
scene.render.filepath = str(output)
scene.render.image_settings.file_format = "PNG"
bpy.ops.render.render(write_still=True)
print(f"Rendered {output}")
