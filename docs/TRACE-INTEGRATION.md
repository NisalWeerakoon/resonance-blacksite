# TRACE integration checkpoint

Owner: Team Lead. Branch: `feature/team-lead-trace`.

## Implemented source

- `TracePredictionModel` accepts timed, valid observations; rejects old timestamps and implausible speeds; smooths velocity; decays stale confidence.
- `TraceCandidateScoring` ranks reachable intercept points with visible alignment, chokepoint, route-history and travel components. It rejects late/unreachable interceptions.
- `TraceWaypointGraph` stores waypoint and portal edges. `GraphAStar` now reports path cost and expanded-node count for ordinary or Weighted A*.
- `TraceBrain` selects the strongest reachable candidate on a timed decision tick and emits `RouteChosen` for the Agent Controller's movement component. It does not read the hidden player's Transform or move its own Transform.
- EditMode tests cover reversal, stale/outlier observations, and unreachable/late candidates.
- `TraceSightSensor` supplies observations only when a raycast reaches the player
  collider within range; occluded or absent targets produce no new sighting.
- `TraceDecisionSandbox.unity` wires the TRACE prefab, player, four-node loop,
  and three intercept candidates for isolated decision tests. It deliberately
  does not move TRACE; the Agent Controller owns the shared movement layer.

## In Unity Editor

1. Open the project in Unity 6000.6.5f1 after the team's version change. The user's Console showed zero red compile errors; recheck after each merge.
2. Run **BLACKSITE → Initialize URP and prototype scenes**. The generated scenes and renderer assets still need a Unity validation commit.
3. Open `Assets/Blacksite/Sandboxes/TeamLead/TraceDecisionSandbox.unity` to inspect the working sandbox graph. The real world scene still needs its own waypoints and D01–D07 portal IDs.
4. The sandbox assigns the graph, candidates and a line-of-sight player sensor. In the real world, connect door events to `SetPortalBlocked` and use only real perception/shared alarm observations.
5. Have the Agent Controller's motor subscribe to `RouteChosen` and follow the emitted waypoints. Until connected, TRACE makes route decisions but does not visibly move.
6. Run EditMode tests. Demonstrate movement forward, reversal, stale/no sightings, blocked portal, and all candidates unreachable. Capture selected scores, path cost, and expanded nodes.

## Model pipeline

Blender 5.2 is available. Original blockouts and newer detailed rigged prototypes for BASTION, ECHO, RIFT and TRACE exist on this Team Lead branch; see `docs/MODEL-BLOCKOUTS.md` and `docs/ANIMATION-HANDOFF.md`. The detailed FBXs re-imported in Blender with bones and weights, and a test pose moved geometry. Unity 6000.6.5f1 imported the FBXs and prefab audit passed. UV quality, animation, Play Mode and gameplay remain unverified. Keep the editable `.blend` sources in Git LFS.
