# World and character integration handoff

Branch: `integration/team-lead-world-2026-10-18`.

Open `Assets/Blacksite/Scenes/Integration/WorldCharacterIntegration.unity` in
Unity 6000.6.5f1. This scene is a copy of the World Builder's committed
`Assets/Scenes/final.unity`; that original scene is unchanged. The static
ECHO demonstration prototype is disabled only in the integration copy.

The four Team Lead prefabs are placed at the World Builder's room markers:

| Prefab | Room |
| --- | --- |
| ECHO | Acoustic Lab |
| BASTION | Security Ops |
| RIFT | Maintenance |
| TRACE | Core Vault |

The character prefabs are under `Assets/Blacksite/Prefabs/Agents/`. Their
editable Blender sources and FBX exports are under
`Assets/Blacksite/Art/Characters/Detailed/`. Miran can use this integrated
scene to test shared movement and animation, but should develop and commit
that work on his own feature branch and open a PR to `develop`.

## What Unity checks passed

- Unity 6000.6.5f1 batch import compiled the merged world and Team Lead code.
- The integrated scene opened with 994 objects, 104 colliders and zero missing
  scripts. All four character instances are linked prefabs with skinned meshes
  and simple colliders.
- Four EditMode tests passed after the branch merge.
- Static imported visual bounds: ECHO 1.56 m wide; BASTION 2.95 m; RIFT
  1.73 m; TRACE 1.37 m. The seven doorway frame openings measure 3.35 m.
  This is only a static size comparison; it does not prove animated clearance.

## Still required before calling the game functional

- Open the integration scene visibly and inspect orientation, lighting and
  floor contact. Run Play Mode and walk the level.
- Miran connects the shared motor/Animator to the prefabs, then checks real
  door traversal, shield/dish clipping, hover height and collision behavior.
- Wire TRACE's graph and actual room/portal events in the real world. The
  separate TRACE decision sandbox is implemented, but its route is not yet
  connected to the real-world motor.
- Integrate ECHO, BASTION and RIFT AI from their owners, then verify all four
  simultaneously, build the target platform and capture profiling evidence.

Do not merge this scene over the World Builder's original until the visible
walkthrough and character clearance checks pass.
