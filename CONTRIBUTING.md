# Contributing

## One-time setup

1. Install Git LFS, then run `git lfs install`.
2. Clone the private repository and switch to `develop`.
3. Open the root folder with Unity `6000.3.15f1`.
4. Let packages resolve, then confirm there are no Console compile errors before editing.

## Daily workflow

1. `git switch develop` and pull the latest changes.
2. Create or reuse your own feature branch: `feature/<owner>-<scope>`.
3. Make small, authentic commits that describe completed work; never manufacture or backdate history.
4. Pull `develop`, resolve conflicts on your feature branch, and test your scene.
5. Open a pull request into `develop` with test evidence and screenshots where useful.

## Commit examples

- `feat(echo): add acoustic event scoring`
- `feat(world): block out loading bay loop`
- `test(rift): cover invalidated-door replanning`
- `fix(shared): prevent stale path after portal closes`

## Unity rules

- Never commit generated folders listed in `.gitignore`.
- Always commit `.meta` files with their matching assets.
- Avoid editing the same scene or prefab simultaneously. Prefer additive scenes and nested prefabs.
- Put personal experiments in `Assets/Blacksite/Sandboxes/<Owner>/`.
- Shared contracts live in `Assets/Blacksite/Scripts/Shared/`; discuss breaking changes before merging.
- Large binary assets must be tracked by Git LFS.

## Pull-request checklist

- [ ] Unity opens with no new Console errors.
- [ ] The feature works in its sandbox or integration scene.
- [ ] Tests were added or updated where practical.
- [ ] No generated or unrelated files are included.
- [ ] The PR identifies owner, agent/graphics role, test steps, and known limitations.

