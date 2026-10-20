using System.Collections.Generic;
using UnityEngine;

namespace Blacksite.Agents.Bastion
{
    // Isolated proof-of-behaviour only. Replace with Miran's shared path follower in the game.
    [RequireComponent(typeof(BastionBrain))]
    public sealed class BastionSandboxActuator : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float speed = 2f;
        [SerializeField, Min(.05f)] private float waypointRadius = .3f;
        private readonly List<Vector3> route = new List<Vector3>();
        private BastionBrain brain;
        private int index;

        private void Awake() => brain = GetComponent<BastionBrain>();
        private void OnEnable()
        {
            if (brain == null) brain = GetComponent<BastionBrain>();
            brain.RouteChosen += Follow;
            brain.AttackRequested += AimOnly;
        }
        private void OnDisable()
        {
            if (brain == null) return;
            brain.RouteChosen -= Follow;
            brain.AttackRequested -= AimOnly;
        }
        private void Follow(IReadOnlyList<Vector3> waypoints)
        {
            route.Clear();
            if (waypoints != null) route.AddRange(waypoints);
            index = 0;
            Debug.Log($"BASTION sandbox route requested: {route.Count} waypoints", this);
        }
        private void Update()
        {
            if (index >= route.Count) return;
            Vector3 target = route[index];
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.magnitude <= waypointRadius) { index++; return; }
            Vector3 direction = delta.normalized;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), 6f * Time.deltaTime);
        }
        private void AimOnly(Transform target)
        {
            // Combat damage is intentionally not faked by this sandbox adapter.
        }
    }
}
