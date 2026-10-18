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

Unity 6000.6.5f1 batch-mode model import and 4/4 EditMode tests passed on October 18; the user's Editor Console showed two warnings and zero red errors. Play Mode, animation and an integrated build are not yet verified; see `docs/VALIDATION.md`. The player prototype uses the classic Input Manager; set Active Input Handling to **Both** or **Input Manager (Old)** if Unity prompts for it.

