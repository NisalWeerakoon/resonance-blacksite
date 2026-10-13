# Architecture

## Boundaries

- `Shared`: stable contracts, world-state events, graph/path abstractions, shared diagnostics.
- `Agents/Echo`: Utility AI, acoustic-room Dijkstra, A* execution.
- `Agents/Bastion`: behaviour tree, visibility/cover scoring, A* execution.
- `Agents/Rift`: GOAP action search, invalidation/replanning, A* execution.
- `Agents/Trace`: route forecasting, influence/candidate scoring, Weighted A* execution.
- `World`: doors D01–D07, rooms, cover points, acoustic edges, movable obstacles.
- `Gameplay`: player interaction, decoy, objective, extraction, health/combat.
- `Presentation`: character visuals, animation, VFX, audio, UI, camera.

Agents may depend on `Shared`; `Shared` must never depend on a concrete agent. World changes publish state through contracts rather than directly calling agent-specific code.

## Scene strategy

- One additive integration scene owned by the integrator.
- Each member owns a sandbox scene and their prefabs.
- Prefer nested prefabs over shared-scene edits.
- Demonstrate every algorithm with a proxy capsule before final art.

## Definition of integrated

The player can traverse the six-zone loop, alter at least one door/portal, emit a decoy sound, and observe all four proxy agents choose visibly different responses without Console errors.

