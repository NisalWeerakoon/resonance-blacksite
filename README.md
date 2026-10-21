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

On October 18, Unity 6000.6.5f1 imported the models and passed 4/4 then-current
EditMode tests. The player prototype uses the classic Input Manager; set Active
Input Handling to **Both** or **Input Manager (Old)** if Unity prompts for it.

## Submission playable scene (October 21)

Open `Assets/Blacksite/Scenes/Integration/SubmissionPlayable.unity` in Unity
6000.6.5f1 and press Play. This is a copy of the World Builder integration scene;
her original `Assets/Scenes/final.unity` remains untouched. Move with WASD and
mouse, use left click for a pulse shot, E near the core to collect it or near
systems to interact, G to throw a decoy, and return to ENTRY to extract. Press
R after a win or capture to retry. ECHO, BASTION, RIFT and TRACE are included
with their feature-branch logic and scene motors. The target scene is the only
enabled Build Settings scene.

Automated Unity 6000.6.5f1 validation: 34/34 EditMode and 9/9 PlayMode tests
passed on October 21, including a four-agent scene load, agent decisions and
physical movement, route, combat/capture and mission pickup/extraction checks.
These tests do not establish visual polish,
animation quality or full human playthrough; see `docs/VALIDATION.md`.

