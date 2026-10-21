# RESONANCE // BLACKSITE — Systems Engineer handoff

Prepared separately for the `feature/systems-bastion` branch on 2026-10-18.
This package has **not** been pushed to GitHub or merged into the game. The
recipient should inspect, understand, test, and adapt it before committing.
It contains an isolated proof scene, not a claim that the integrated game is
complete. Do not misrepresent who authored or tested it if asked.

## Included

- Three Unity prefabs in `Assets/Blacksite/Prefabs/Systems/`: an E-key door
  with persistent handle, a Rigidbody thrown decoy, and a movable barricade.
  Matching URP materials and `.meta` files are included.
- `Assets/Blacksite/Scenes/Test_Interactions_Bastion.unity`, an isolated scene
  with a first-person test player, one door, decoy, barricade/choke volume,
  BASTION proxy, five graph nodes, three cover positions and live event HUD.
- Runtime code for interactions, noise/door/barricade signals, BASTION
  Behaviour Tree, cover scoring, sight memory and shared A* graph adapter.
- `BastionSandboxActuator` proves route requests in this **test scene only**.
  Replace it with Miran's shared path follower in the integrated game.
- `BastionDecisionTests.cs` covering Running propagation, selector fallback,
  numeric scoring and candidate rejection.
- Three automated Play Mode smoke tests for door state/collision events,
  decoy impact noise and barricade choke-state changes.
- An editor menu builder/auditor so the sandbox can be regenerated.

## Validation actually performed

- Unity `6000.6.5f1` imported and compiled this overlay in a separate copy of
  the project; batch run exited 0 with no `error CS` compilation errors.
- Editor generation produced the scene and three prefabs.
- Structural audit: **zero missing scripts**, one door, one barricade, one
  BASTION brain and three cover points.
- Unity EditMode tests: **7 passed, 0 failed** (includes four existing project
  tests and three handoff tests).
- Unity automated Play Mode smoke tests: **3 passed, 0 failed**. These check
  the isolated door, falling decoy and barricade state, not the whole scene.
- **Not yet verified:** human-controlled Play Mode, actual E/G input,
  door re-planning during play, cover reachability in the world
  scene, animation, weapon damage, all-four-agent gameplay or final build.

## How to use on the member's branch

1. In the member's own clone, switch to `feature/systems-bastion` and make
   sure there are no uncommitted changes that could be overwritten. The ZIP
   is an `Assets/` overlay, **not** a standalone Unity project.
2. Copy this package's `Assets/` folder into the Unity project root. Inspect
   the diff before accepting overlapping files: it replaces the original
   placeholder `BastionBrain.cs`. Preserve existing `.meta` GUIDs and commit
   new `.meta` files alongside their assets.
3. Open the project with Unity `6000.6.5f1`, let it import, and open
   `Assets/Blacksite/Scenes/Test_Interactions_Bastion.unity`.
4. Enter Play Mode. Click Game to lock mouse, use WASD to move, E to
   toggle/push, G to throw a decoy, and Esc to release the cursor. Check the
   HUD and Console for door/decoy/barricade signals and BASTION cover scores.
   The door handle remains raycastable when open. The barricade's D02 route
   opens only once it clears its assigned choke volume.
5. Test a closed vs open D01 route, blocked vs clear D02 route, all three
   cover scores, a cover point marked Occupied, no available cover, lost LOS,
   and movement of the BASTION proxy. Record actual results and fix failures.
6. To regenerate only the sandbox, use `BLACKSITE > Build Systems Engineer
   sandbox`; `BLACKSITE > Audit Systems Engineer sandbox` checks structure.
   Regeneration overwrites the sandbox scene; do not run after hand-editing
   that scene unless changes are saved elsewhere.
7. Commit the reviewed work from the member's own branch and open a PR into
   `develop` only after Play Mode evidence exists. The member should be able
   to explain BT statuses, score weights, rejected covers and A* portal IDs.

## Integration contract

- The world scene's D01–D07 IDs must map to the same graph edges. The sandbox
  uses D01 for the door and D02 for the barricade. These IDs are examples,
  not an automatic world-scene installation.
- `WorldSignals.NoiseEmitted` carries decoy impacts; ECHO must subscribe on
  its own branch. `WorldSignals.DoorStateChanged` carries door ID/open state.
  BASTION also reads `IPortalState.Changed` for immediate blocked-path data.
- `BastionBrain.RouteChosen` needs the shared movement layer; its
  `AttackRequested` is an aim/fire request, **not** a damage implementation.
  The sandbox actuator moves a proxy but intentionally does not fire.
- `MovableBarricade` requires a scene-specific choke trigger assigned. It
  uses bounding-box overlap as an MVP approximation; verify in final geometry.
- The handoff uses the existing `TraceWaypointGraph` plus shared `GraphAStar`
  rather than duplicating A*. Replace or adapt the graph nodes to world data.
- BASTION's `LowHealthGuard` and `PatrolPlaceholder` are safe fallbacks;
  retreat, patrol and player-weapon adaptation are not implemented here.

Notion source: [BASTION AI](https://app.notion.com/p/3f47f9f5e64381938fddd837627eb298),
[technical architecture](https://app.notion.com/p/3f47f9f5e6438166a377d30c0b3e021e).
