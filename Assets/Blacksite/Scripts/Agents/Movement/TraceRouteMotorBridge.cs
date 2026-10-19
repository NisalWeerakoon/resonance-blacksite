using System.Collections.Generic;
using Blacksite.Agents.Trace;
using UnityEngine;

namespace Blacksite.Agents.Movement
{
    /// <summary>Passes TRACE's chosen intercept route to the shared movement motor.</summary>
    [DisallowMultipleComponent]
    public sealed class TraceRouteMotorBridge : MonoBehaviour
    {
        [SerializeField] private TraceBrain brain;
        [SerializeField] private WaypointAgentMotor motor;

        private void Awake()
        {
            if (brain == null) brain = GetComponent<TraceBrain>();
            if (motor == null) motor = GetComponent<WaypointAgentMotor>();
        }

        private void OnEnable()
        {
            if (brain == null) brain = GetComponent<TraceBrain>();
            if (motor == null) motor = GetComponent<WaypointAgentMotor>();
            if (brain == null) return;
            brain.RouteChosen += ApplyRoute;
            ApplyRoute(brain.SelectedWaypoints, brain.SelectedNodeId);
        }

        private void OnDisable()
        {
            if (brain != null) brain.RouteChosen -= ApplyRoute;
        }

        private void ApplyRoute(IReadOnlyList<Vector3> waypoints, string destinationNodeId)
        {
            if (motor == null) return;
            if (string.IsNullOrEmpty(destinationNodeId) || waypoints == null || waypoints.Count == 0)
            {
                motor.ClearDestination();
                return;
            }

            motor.SetRoute(waypoints);
        }
    }
}
