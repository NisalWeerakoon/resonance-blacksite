# Validation record

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

