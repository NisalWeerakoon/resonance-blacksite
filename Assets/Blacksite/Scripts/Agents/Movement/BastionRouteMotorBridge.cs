using System.Collections.Generic;
using Blacksite.Agents.Bastion;
using UnityEngine;

namespace Blacksite.Agents.Movement
{
    /// <summary>Passes BASTION's chosen cover/search route to the shared WaypointAgentMotor.</summary>
    [DisallowMultipleComponent]
    public sealed class BastionRouteMotorBridge : MonoBehaviour
    {
        [SerializeField] private BastionBrain brain;
        [SerializeField] private WaypointAgentMotor motor;

        private void Awake()
        {
            if (brain == null) brain = GetComponent<BastionBrain>();
            if (motor == null) motor = GetComponent<WaypointAgentMotor>();
        }

        private void OnEnable()
        {
            if (brain == null) brain = GetComponent<BastionBrain>();
            if (motor == null) motor = GetComponent<WaypointAgentMotor>();
            if (brain == null) return;
            brain.RouteChosen += ApplyRoute;
        }

        private void OnDisable()
        {
            if (brain != null) brain.RouteChosen -= ApplyRoute;
        }

        private void ApplyRoute(IReadOnlyList<Vector3> waypoints)
        {
            if (motor == null) return;
            if (waypoints == null || waypoints.Count == 0)
            {
                motor.ClearDestination();
                return;
            }
            motor.SetRoute(waypoints);
        }
    }
}
