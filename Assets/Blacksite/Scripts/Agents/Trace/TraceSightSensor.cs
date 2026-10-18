using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Trace
{
    /// <summary>Reports only unobstructed, in-range player sightings to TRACE.</summary>
    [RequireComponent(typeof(TraceBrain))]
    public sealed class TraceSightSensor : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Vector3 localEyeOffset = new Vector3(0f, 1.55f, -0.4f);
        [SerializeField, Min(0.1f)] private float sightRange = 18f;
        [SerializeField, Min(0.05f)] private float observationInterval = 0.25f;
        [SerializeField] private LayerMask sightLayers = ~0;

        private TraceBrain brain;
        private float nextObservationTime;

        private void Awake() => brain = GetComponent<TraceBrain>();

        private void Update()
        {
            if (playerTarget == null || Time.time < nextObservationTime) return;
            nextObservationTime = Time.time + observationInterval;
            var origin = transform.TransformPoint(localEyeOffset);
            var target = playerTarget.position + Vector3.up;
            var direction = target - origin;
            float distance = direction.magnitude;
            if (distance > sightRange || distance < 0.01f) return;
            if (!Physics.Raycast(origin, direction / distance, out var hit, distance + 0.1f,
                    sightLayers, QueryTriggerInteraction.Ignore)) return;
            if (!hit.transform.IsChildOf(playerTarget)) return;
            brain.Observe(new WorldObservation(playerTarget.position,
                Mathf.Clamp01(1f - distance / (sightRange * 1.5f)), Time.timeAsDouble));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(localEyeOffset), sightRange);
        }
    }
}
