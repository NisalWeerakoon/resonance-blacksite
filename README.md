# RESONANCE // BLACKSITE

A four-person Unity 6 URP project combining a tactical sci-fi FPS with four independently implemented autonomous agents.

## Required tooling

- Unity `6000.6.5f1` on this Team Lead branch (the earlier `6000.3.15f1` installation was incomplete)
- Git 2.40+ and Git LFS 3+
- Blender 5.2 if editing the original `.blend` model sources; not required to use the FBX prefabs in Unity

## First clone

```text
git lfs install
git clone https://github.com/NisalWeerakoon/resonance-blacksite.git
cd resonance-blacksite
git switch develop
```

Open the repository root in Unity Hub. Do not commit `Library`, `Temp`, `Logs`, generated IDE files, or local settings.
After packages resolve, run **BLACKSITE → Initialize URP and prototype scenes** for the starter greybox. The Team Lead branch also includes **BLACKSITE → Build character prefabs and review scene** and **BLACKSITE → Audit character prefabs**. The review scene is `Assets/Blacksite/Sandboxes/TeamLead/CharacterReview.unity`.

## Branch model

- `main`: demonstrable builds only; enable branch protection in GitHub settings when collaborator access is configured.
- `develop`: daily integration branch.
- `feature/<owner>-<scope>`: one bounded feature per branch.
- `feature/world-builder`: the World Builder's level upload. `feature/world-builder-echo`: her ECHO AI work. The Team Lead models ECHO's visual character.
- Merge through pull requests into `develop`; require one teammate review where schedule permits.

See [CONTRIBUTING.md](CONTRIBUTING.md), [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), and [docs/TEAM-WORKFLOWS.md](docs/TEAM-WORKFLOWS.md).

## Milestones

- **October 13:** every member clones, creates their feature branch, and makes a genuine first commit.
- **October 17:** feature freeze; all four proxy agents visible in the integrated greybox.
- **October 18:** functional integrated build target.
- **October 19–20:** stability, profiling, evidence capture, video, and viva preparation.
- **October 21, 2026:** Graphics submission.

The submission player uses Unity's new Input System. Leave Active Input Handling
at the committed project setting, **Both**.

## Submission playable scene (October 21)

Open `Assets/Blacksite/Scenes/Integration/SubmissionPlayable.unity` in Unity
6000.6.5f1 and press Play, or launch the Windows standalone build. This is a
copy of the World Builder integration scene; her original `Assets/Scenes/final.unity`
remains untouched. Press **Start Mission** or **Enter**. Activate the ACOUSTIC,
SECURITY and MAINT relays with **E**, collect the core in CORE VAULT with **E**,
and return to ENTRY to extract. The core stays locked until all three relays
are active. Use WASD and mouse to move/aim, left click to fire the visible pulse
carbine, **R** to reload, and **G** to throw a decoy. The HUD shows health, ammo,
relay progress and the status of ECHO, BASTION, RIFT and TRACE. Each threat can
damage the player; three pulse hits disable one. Zero health ends the run. The
win/lose screen offers a retry, and **R** also restarts after either outcome.

Automated Unity 6000.6.5f1 validation on October 21: **34/34 EditMode** and
**15/15 PlayMode** tests, plus a Windows x64 build with zero errors. The rendered
start and gameplay views were inspected. A complete human end-to-end playthrough
and visual input through the automated computer-control tool were not verified;
see `docs/VALIDATION.md`.

