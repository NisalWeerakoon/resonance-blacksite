# Team Lead character blockouts

`tools/blender/build_character_blockouts.py` creates original BASTION and TRACE
hard-surface blockouts at metre scale. They follow the Notion silhouette brief:
BASTION has oversized armour and shield; TRACE has a halo, single eye and three
stabilizers. The generated blockouts were opened and structurally checked in
Blender 5.2.0 on 2026-10-14; they are **not finished character models**.

Regenerate from the repository root with Blender 5.2.0:

```text
blender --background --python tools/blender/build_character_blockouts.py
```

The script saves one editable `.blend` source and two `.fbx` exports under
`Assets/Blacksite/Art/Characters/Blockouts/`. Git LFS tracks those formats.
The saved `.blend` was reopened and contains 14 BASTION objects and 10 TRACE
objects; both FBX exports completed. Their appearance, UVs, rigging and Unity
import remain unverified.
Inspect each mesh in Blender, refine topology and UVs, then import into Unity.
Check normals, scale, material conversion, corridor/shield clearance, and
animation before calling either model playable. Preserve screenshots of the
editable meshes and UVs for the Graphics submission.
