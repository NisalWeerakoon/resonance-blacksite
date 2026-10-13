# Four member feature workflows

## Team Lead — TRACE + character pipeline

- Branch: `feature/team-lead-trace`
- Owns: project integration, original character modeling (including ECHO), UV/material/import pipeline, TRACE prediction and Weighted A*.
- Final integration owner: reviews member pull requests, resolves cross-feature conflicts, runs the full Unity smoke test, fixes integration defects, and decides when `develop` is ready to merge into `main` for the October 18 build and October 21 submission.
- First authentic commit: add TRACE intercept candidate data and a failing/passing scoring test.
- Avoid: editing another member's sandbox or squashing away their contribution history.

## World Builder — environment + ECHO AI

- World branch: `feature/world-builder` for the completed level upload and follow-up environment changes.
- Separate AI branch: `feature/world-builder-echo` for ECHO's Utility AI and acoustic Dijkstra. This agent is her Intelligent Systems contribution.
- Owns the six-room greybox, two route loops, D01–D07 portal IDs, and lighting/texturing baseline. Character modeling is assigned to the Team Lead.
- First authentic commit: commit the already completed world scene, its assets and `.meta` files. The commit must reflect her actual work.
- Evidence: top-down screenshot for the world; a door-open/closed acoustic-cost test if she also takes ECHO AI.

## Systems Engineer — BASTION + interactions

- Branch: `feature/systems-bastion`
- Owns: interactive door, throwable decoy, movable barricade, BASTION behaviour tree and cover score.
- First authentic commit: door state component and isolated test scene.
- Evidence: cover-choice comparison with visible score breakdown.

## Agent Controller — RIFT + movement

- Branch: `feature/controller-rift`
- Owns: shared waypoint motor, rotation/animation hooks, RIFT GOAP actions and replanning.
- First authentic commit: capsule PathFollower with arrival/blocked-path handling.
- Evidence: door closure invalidates a current plan and produces a new valid action sequence.

## Integration cadence

- Pull requests target `develop`, never `main`.
- Integrate at least once daily; do not wait for final models.
- Each PR includes exact test steps and a short screen capture or screenshot when visual.
- Each member fixes defects in their own feature first. The Team Lead owns cross-feature failures and final release validation; teammates must still provide test evidence and respond to review findings.
- By October 17, stop adding scope. October 18 is the integrated-build target; October 19–20 are for stabilization and submission evidence.

