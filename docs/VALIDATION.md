# Validation record

## 2026-10-21 gameplay repair

- Replaced the walk-only mission with three E-activated room relays, a locked
  core until those relays are active, core extraction, a start screen, and
  win/lose screens.
- Added a visible pulse carbine, 12-charge magazine, timed reload, hit marker,
  health/damage feedback, player death, and an on-screen four-agent tracker.
- Connected each of ECHO, BASTION, RIFT and TRACE to distinct damaging attacks;
  their existing navigation/decision components remain in the saved scene.
- The saved scene loads all four brains/motors and three relays. PlayMode tests
  separately checked that **each agent damages the player**, relay E input,
  core lock, pulse-hit disable, health-based loss, victory, retry, and a
  physical player walk to the core and back.
- Unity 6000.6.5f1: **15/15 PlayMode** and **34/34 EditMode** tests passed.
  Windows x64 build succeeded with **0 errors**. The rendered title and live
  gameplay views were visually inspected with the carbine and HUD present.
- Limitation: the computer-control tool did not deliver clicks/Enter to the
  full-screen Unity title reliably, so the rendered gameplay inspection used
  the build's `-skipTitle` inspection flag. A complete human playthrough,
  difficulty tuning, and animation quality are **not verified** by these checks.

## 2026-10-21 submission integration

- Merged Agent Controller/RIFT, Systems/BASTION and World Builder/ECHO feature
  branches into `integration/submission-2026-10-21`. Team feature branches and
  the World Builder's original level scene were preserved.
- Built `SubmissionPlayable.unity` with the World Builder environment, four
  active agent brains/motors, a 12-node waypoint graph, two usable doors, one
  movable barricade, cover, decoy/noise responses, a pulse weapon, and a
  core-pickup/return-to-entry win condition with capture/retry state.
- Unity 6000.6.5f1 EditMode suite: **34 passed, 0 failed**.
- Unity 6000.6.5f1 PlayMode suite: **9 passed, 0 failed**. Scene tests load the
  saved submission scene and check four brains/motors, entry-to-core A* route,
  two doors, barricade, no missing runtime components, pickup and extraction,
  agent perception/decisions, pulse-hit disable, capture, and RIFT physical
  corridor motion. A navigation-collider omission was found and fixed in the
  submitted scene before these final checks.
- Windows x64 standalone build **succeeded with 0 build errors**. A headless
  launch initialized the built player and loaded the scene without a logged
  exception; it was stopped after startup because the game normally keeps
  running. The build output is kept outside the Git checkout.
- The submitted copy removes nonportable legacy World Builder data component
  references that reported as missing at runtime; original source scene remains.
- These checks are automated. A human visual playthrough, animation quality,
  performance and captured submission evidence remain unverified until done.

## 2026-10-13 foundation check

- Repository structure: created.
- Unity version pin: `6000.3.15f1`.
- URP dependency: `com.unity.render-pipelines.universal` `17.3.0`.
- Assembly boundaries and starter C# contracts: created.
- Git LFS patterns: created; local Git LFS executable detected.
- Unity Editor compilation: **not run** — no Unity Editor installation was detected on this machine.
- Starter ZIP import: superseded by user request to recreate the foundation directly.
- A* graph planner, player controller, and editor scene/URP initializer: source added, **not editor-verified**.
- TRACE observation, prediction, intercept scoring, Weighted A* route selection and EditMode tests: source added on the Team Lead feature branch, **not editor-verified**.
- BASTION/TRACE Blender blockout generator: source added; initial availability check missed the installed Blender application.

## 2026-10-14 Team Lead character blockouts

- Blender 5.2.0 LTS found at `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.
- Generator ran successfully and saved an editable `.blend` plus BASTION and TRACE `.fbx` exports on the Team Lead feature branch.
- Reopened `.blend` in Blender background mode: 14 BASTION objects and 10 TRACE objects.
- Blender reported a thumbnail-cache write warning, but saved the `.blend` and completed both FBX exports.
- Visual appearance, mesh quality, UVs, rigging, FBX re-import and Unity import are **not yet verified**.

Open the repository in the pinned editor, allow package resolution, run **BLACKSITE → Initialize URP and prototype scenes**, then record the Console and test runner results here. Commit the generated scenes, URP assets, and their `.meta` files to `develop`.

## 2026-10-17 four-character blockout handoff

- Blender 5.2 regenerated the original source `.blend` and four separate FBXs.
- Blender re-import validation passed: BASTION 14 meshes, TRACE 10, ECHO 12,
  RIFT 10; all imported meshes had primitive UV layers and metre-scale bounds.
- A Workbench preview was rendered and visually inspected. The four signature
  silhouettes are distinct, but the models are intentionally crude blockouts.
- Animator handoff ZIP was packaged locally outside the Git checkout. It
  contains the source `.blend`, four FBXs, preview image and instructions.
- A separate Systems Engineer source/test ZIP was packaged outside the Git
  checkout. It has **not** been copied into the Unity project or pushed.
- Unity compilation, FBX import, rigging, animations, scene interactions and
  gameplay are **not verified**. Local Editor detection still shows only
  6000.6.5f1, not the pinned 6000.3.15f1.

## 2026-10-17 detailed character revision and Unity diagnosis

- Blender 5.2 built separate editable `.blend` and rigged `.fbx` files for
  BASTION, ECHO, RIFT and TRACE in `Assets/Blacksite/Art/Characters/Detailed`.
- FBX re-import checks passed: BASTION 114 meshes/18 bones, ECHO 112/18,
  RIFT 102/18, TRACE 40/6; all imported meshes had UVs, bone weights and an
  armature modifier. A test bone pose moved geometry in each export.
- Workbench previews were rendered and inspected. They are visibly more
  detailed modular robots, but not a claim of final art polish or animations.
- Unity 6000.3.15f1 is now installed, but `Editor.log` shows its
  `UnityReferenceAssemblies/unity-4.8-api/Facades` directory is missing. This
  editor-installation error occurs before project scripts compile; Safe Mode
  is not evidence of a C# defect. Repair/reinstall the pinned editor.
- The World Builder's latest branch contains `Assets/Scenes/final.unity` and
  a saved structural audit. A disposable Unity 6000.6 import is being run;
  **do not** claim world Play Mode is verified until it completes.

## 2026-10-18 Unity 6000.6.5f1 import checks

- The user's upgraded clone opened in Unity and the Console screenshot showed
  two warnings, zero red compilation errors. The warnings concerned URP global
  settings generation and the legacy Input Manager used by the prototype.
- A separate batch-mode work copy imported the four detailed FBXs and generated
  four prefabs with URP Lit material overrides and simplified colliders.
- `BLACKSITE → Audit character prefabs` passed: BASTION 114 renderers/114
  skinned meshes, ECHO 112/112, RIFT 102/102, TRACE 40/40; each has one simple
  collider and valid URP materials. The review scene opened with seven roots.
- Unity EditMode test run passed 4/4 tests (0 failed) in 6000.6.5f1. This
  covers the current shared graph/TRACE unit tests, not Play Mode behavior.
- The World Builder's committed `Assets/Scenes/final.unity` opened in a
  disposable Unity 6000.6.5f1 copy: 553 objects, 100 colliders, zero missing
  scripts. This is an Editor scene-load check, **not** Play Mode or a build.
- Still pending: visual review inside Unity, animation, TRACE movement hookup,
  four-agent integrated gameplay, target build and profiling.

## 2026-10-18 TRACE sandbox checkpoint

- Unity 6000.6.5f1 compiled the new `TraceSightSensor` and generated
  `TraceDecisionSandbox.unity` with a player collider, four graph nodes, four
  bidirectional links, three candidate intercepts and a TRACE prefab.
- The sight sensor uses range and physics occlusion before submitting an
  observation. The scene has **not** yet been Play Mode tested; the motor and
  animation remain the Agent Controller's deliverables.
- Unity's saved-reference audit passed for the TRACE brain, graph, sensor,
  player target and three candidates. EditMode tests remained 4/4 passed.

## 2026-10-18 World Builder + Team Lead integration branch

- Merged the World Builder's commit `c04786b` with the Team Lead branch on
  `integration/team-lead-world-2026-10-18` without source conflicts.
- Generated `WorldCharacterIntegration.unity` as a copy of the World Builder's
  original `final.unity`; placed four linked character prefabs in their
  matching rooms and disabled the static ECHO demo prototype in the copy only.
- Unity 6000.6.5f1 loaded the merged scene with 994 objects, 104 colliders,
  zero missing scripts and four complete linked character prefabs. Integrated
  EditMode tests passed 4/4.
- Static visual widths are ECHO 1.56 m, BASTION 2.95 m, RIFT 1.73 m and
  TRACE 1.37 m. The seven frame openings are 3.35 m. These measurements do
  not prove animated doorway clearance or Play Mode functionality.
- Remaining: visible scene inspection, walking/animation, TRACE real-world
  waypoint wiring, team AI integrations, gameplay and target build.

