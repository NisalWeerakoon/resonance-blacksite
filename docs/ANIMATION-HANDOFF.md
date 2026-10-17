# Character model handoff to Agent Controller

Owner of source models: Team Lead. Use the **detailed** `.blend`/`.fbx` pairs
in `Assets/Blacksite/Art/Characters/Detailed/`, not the older Blockouts folder.
Each model has separate mesh parts and a named skeleton. The three humanoids
share 18 bone names; TRACE has a six-bone mechanical rig. These assets still
need Unity validation and animation, so they are not yet playable characters.

## What is ready

- Four visually distinct original modular robots with named armour, joints,
  hands, footwear and signature geometry: BASTION shield, ECHO dishes, RIFT
  swept shoulder panel, TRACE halo/eye/stabilizers.
- Blender 5.2 re-imported every detailed FBX on 2026-10-17: BASTION 114
  meshes / 18 bones, ECHO 112 / 18, RIFT 102 / 18, TRACE 40 / 6. All
  imported meshes had UV layers, bone weights and armature modifiers. A test
  bone pose moved attached geometry in all four files.
- `*_detailed_preview.png` files are quick appearance checks. They are not
  Unity screenshots or evidence of finished materials/animation.

## Animation teammate's work

1. Import the FBXs to a separate Unity sandbox. Check orientation, scale,
   normals, material conversion, and appearance before rigging.
2. Use the existing humanoid skeleton and refine its animation controls for
   BASTION, ECHO and RIFT. Rigid meshes are fully weighted to individual bones,
   not smoothly deforming skin. Shield, dishes and asymmetric panel are
   separate parts; verify their assigned pivots before creating clips.
3. Use TRACE's separate mechanical bones for root, halo, eye and three
   stabilizers; implement hover/turn motion rather than a humanoid controller.
4. Implement the shared waypoint motor and arrival/blocked-path behavior on
   proxy capsules first. Then connect movement states to idle, move, turn and
   attack/aim animation hooks. Do not let visual rigs own path planning.
5. Test all four in the real corridor widths. Record shield/dish clearance,
   clipping, animation screenshots, Unity Console status and test results.

## Known limitations

- Primitive-generated UV layers exist but have **not** been carefully
  unwrapped or visually checked for final textures. Materials are Blender node
  colours, not validated Unity URP materials.
- Armatures exist, but no animation clips, collision prefabs, LODs or hitboxes
  are included. The models still require Unity import tests and visual cleanup.
- No Unity Editor compilation, FBX Unity import, gameplay, or animation has
  been verified. The project is pinned to Unity 6000.3.15f1. Its current local
  installation is incomplete (missing reference assemblies), so repair it
  before relying on Console or animation results.

The Agent Controller should commit their own movement/animation implementation
and test evidence on `feature/controller-rift`. The Team Lead retains ownership
of source model refinement and cross-feature integration fixes.
