# Character model handoff to Agent Controller

Owner of source models: Team Lead. Use the **detailed** `.blend`/`.fbx` pairs
in `Assets/Blacksite/Art/Characters/Detailed/`, not the older Blockouts folder.
Each model has separate mesh parts and a named skeleton. The three humanoids
share 18 bone names; TRACE has a six-bone mechanical rig. These assets now have
Unity prefabs, but still need animation and movement before they are playable.

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
- Unity 6000.6.5f1 imported all detailed FBXs. The four prefabs in
  `Assets/Blacksite/Prefabs/Agents/` have URP Lit materials and simple
  colliders. `Assets/Blacksite/Sandboxes/TeamLead/CharacterReview.unity`
  places all four for inspection. **BLACKSITE → Audit character prefabs**
  repeats the import checks.

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
  unwrapped or visually checked for final textures. The Unity URP materials
  are simple colour/metallic overrides, not final texture work.
- Armatures and simple collision prefabs exist, but no animation clips, LODs,
  detailed hitboxes or gameplay hookups are included. Visual cleanup remains.
- Unity batch import and prefab audit passed in 6000.6.5f1. Play Mode,
  movement, animation and a target build remain unverified.

The Agent Controller should commit their own movement/animation implementation
and test evidence on `feature/controller-rift`. The Team Lead retains ownership
of source model refinement and cross-feature integration fixes.
