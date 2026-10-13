# RESONANCE // BLACKSITE

A four-person Unity 6.3 LTS URP project combining a tactical sci-fi FPS with four independently implemented autonomous agents.

## Required tooling

- Unity `6000.3.15f1` with the Universal 3D template/workload
- Git 2.40+ and Git LFS 3+
- Blender (current team-agreed version)

## First clone

```text
git lfs install
git clone <repository-url>
cd resonance-blacksite
git switch develop
```

Open the repository root in Unity Hub. Do not commit `Library`, `Temp`, `Logs`, generated IDE files, or local settings.
After packages resolve, run **BLACKSITE → Initialize URP and prototype scenes**. This creates the URP assets, the integration greybox, and four member sandbox scenes. Commit those generated files and their `.meta` files to `develop` after verifying the Unity Console.

## Branch model

- `main`: protected, demonstrable builds only.
- `develop`: daily integration branch.
- `feature/<owner>-<scope>`: one bounded feature per branch.
- Merge through pull requests into `develop`; require one teammate review where schedule permits.

See [CONTRIBUTING.md](CONTRIBUTING.md), [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), and [docs/TEAM-WORKFLOWS.md](docs/TEAM-WORKFLOWS.md).

## Milestones

- **October 13:** every member clones, creates their feature branch, and makes a genuine first commit.
- **October 17:** feature freeze; all four proxy agents visible in the integrated greybox.
- **October 18:** functional integrated build target.
- **October 19–20:** stability, profiling, evidence capture, video, and viva preparation.
- **October 21, 2026:** Graphics submission.

Compilation status: **not yet verified in Unity Editor**. The current machine does not have Unity installed; see `docs/VALIDATION.md`. The player prototype uses the classic Input Manager; set Active Input Handling to **Both** or **Input Manager (Old)** if Unity prompts for it.

