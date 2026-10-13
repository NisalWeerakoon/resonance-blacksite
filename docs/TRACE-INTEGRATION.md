# TRACE integration checkpoint

Owner: Team Lead. Branch: `feature/team-lead-trace`.

## Implemented source

- `TracePredictionModel` accepts timed, valid observations; rejects old timestamps and implausible speeds; smooths velocity; decays stale confidence.
- `TraceCandidateScoring` ranks reachable intercept points with visible alignment, chokepoint, route-history and travel components. It rejects late/unreachable interceptions.
- `TraceWaypointGraph` stores waypoint and portal edges. `GraphAStar` now reports path cost and expanded-node count for ordinary or Weighted A*.
- `TraceBrain` selects the strongest reachable candidate on a timed decision tick and emits `RouteChosen` for the Agent Controller's movement component. It does not read the hidden player's Transform or move its own Transform.
- EditMode tests cover reversal, stale/outlier observations, and unreachable/late candidates.

## In Unity Editor

1. Open the project in Unity 6000.3.15f1 and resolve packages. Fix any compile errors before treating this as working.
2. Run **BLACKSITE → Initialize URP and prototype scenes**. The generated scenes and renderer assets still need a Unity validation commit.
3. In the TRACE sandbox, add a `TraceWaypointGraph` with waypoint Transforms and bidirectional links; use D01–D07 portal IDs where appropriate. Add `TraceInterceptPoint` components to HUB and CORE chokepoints.
4. Assign the graph and candidate points on `TraceBrain`. Feed observations only from real perception/shared alarms via `Observe`; connect door events to `SetPortalBlocked`.
5. Have the Agent Controller's motor subscribe to `RouteChosen` and follow the emitted waypoints. Until connected, TRACE makes route decisions but does not visibly move.
6. Run EditMode tests. Demonstrate movement forward, reversal, stale/no sightings, blocked portal, and all candidates unreachable. Capture selected scores, path cost, and expanded nodes.

## Model pipeline

Blender is unavailable on this machine. The original BASTION and TRACE blockouts, UVs, rigs, exports and Unity prefabs remain to be created and verified in Blender/Unity. Use the Notion character sheet and keep original `.blend` files in Git LFS once made.
