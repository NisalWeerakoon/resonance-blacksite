# Agent Controller implementation handoff

The shared waypoint motor is `Assets/Blacksite/Scripts/Agents/Movement/WaypointAgentMotor.cs`. Add it to an agent root, assign the scene's waypoint graph component, and optionally assign a `CharacterController` and `Animator`. The graph component must implement `IWaypointGraph` and `IWaypointNodeProvider`; `TraceWaypointGraph` currently implements both. RIFT portal changes can be wired with `AgentPortalStateBridge`, which subscribes to components implementing `IPortalState` and forwards blocked state to the brain and motor. For TRACE, `TraceRouteMotorBridge` passes selected intercept waypoints directly to `WaypointAgentMotor.SetRoute`.

The motor moves along graph nodes and then to the requested world position. It exposes arrival and blocked-route state, and provides Animator parameters `Speed`, `MoveX`, `MoveY`, `Moving`, and `Turn`, plus the `Arrive` trigger. Those hooks do not create animation clips or an Animator Controller. Add idle/move/turn/attack states in an Animator Controller and bind it to the humanoid rigs; TRACE can use the same movement interface with hover-specific clips or a separate presentation driver.

`RiftBrain` uses uniform-cost forward search over action preconditions and effects. It selects flank actions while the target is freshly observed and has a flank point, otherwise pursues/investigates the most recent observed position. On a portal event, send the portal state through `SetPortalBlocked`; the motor reroutes and RIFT reevaluates. It reports `CurrentAction`, `CurrentPlan`, and `LastSearchNodes` for a debug overlay. Perception must call `Observe` with actual sightings; do not wire the hidden player Transform directly into RIFT.

## Scene hookup

1. Add `WaypointAgentMotor` and `RiftBrain` to the RIFT root; assign the same scene graph to the motor's Graph Component field.
2. Assign the motor reference on `RiftBrain`, then assign a flank waypoint if one is available.
3. Connect the real sight sensor to `RiftBrain.Observe` and each door's state event to `RiftBrain.SetPortalBlocked`.
4. Add a CharacterController sized for RIFT and an Animator Controller with the parameters above when clips are ready.
5. Verify routes, closing-door reroutes, arrival, and animation transitions in Play Mode before treating the character as integrated.

For a TRACE scene, add `WaypointAgentMotor` and `TraceRouteMotorBridge` to the TRACE root, assign the `TraceBrain` graph and intercept points, and assign the same `WaypointAgentMotor` to the bridge. `AgentSightSensor` can submit line-of-sight observations to any brain implementing `IBlacksiteAgent`; set its player target, range, and sight layers in the Inspector. `AgentPortalStateBridge` accepts the scene's portal components in its portal list.

This code handoff has not been validated in Unity Play Mode. The integration scene and character prefabs still need the component and graph references assigned in the Editor.
