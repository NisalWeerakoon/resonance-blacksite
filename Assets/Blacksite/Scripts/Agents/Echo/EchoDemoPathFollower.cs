using System.Collections.Generic;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// DEMO motor only. The team Agent Controller should supply the production
    /// path follower/motor and animator bridge. Uses waypoints, never queries player position.
    /// </summary>
    public sealed class EchoDemoPathFollower : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
        [SerializeField, Min(0.01f)] private float stoppingDistance = 0.22f;
        private readonly List<Vector3> route = new List<Vector3>();
        private int nextIndex;
        public int WaypointCount => route.Count;
        public bool Moving => nextIndex < route.Count;

        public void Follow(IReadOnlyList<Vector3> newRoute)
        {
            route.Clear();
            nextIndex = 0;
            if (newRoute == null) return;
            for (int i = 0; i < newRoute.Count; i++) route.Add(newRoute[i]);
        }

        public void Stop()
        {
            route.Clear();
            nextIndex = 0;
        }

        private void Update()
        {
            if (!Moving) return;
            Vector3 destination = route[nextIndex];
            destination.y = transform.position.y;
            Vector3 delta = destination - transform.position;
            if (delta.sqrMagnitude < stoppingDistance * stoppingDistance)
            {
                nextIndex++;
                return;
            }
            Vector3 heading = delta.normalized;
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            if (heading.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(heading, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, 320f * Time.deltaTime);
            }
        }
    }
}
