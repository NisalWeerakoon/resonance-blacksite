using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Movement
{
    /// <summary>Reports visible player sightings to any agent brain implementing IBlacksiteAgent.</summary>
    [DisallowMultipleComponent]
    public sealed class AgentSightSensor : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour brainComponent;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Vector3 localEyeOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField, Min(0.1f)] private float sightRange = 18f;
        [SerializeField, Min(0.05f)] private float observationInterval = 0.25f;
        [SerializeField] private LayerMask sightLayers = ~0;

        private IBlacksiteAgent brain;
        private float nextObservationTime;

        private void Awake()
        {
            ResolveBrain();
        }

        private void ResolveBrain()
        {
            brain = brainComponent as IBlacksiteAgent;
            if (brain != null) return;
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (!(component is IBlacksiteAgent candidate)) continue;
                brainComponent = component;
                brain = candidate;
                return;
            }
        }

        private void Update()
        {
            if (brain == null) ResolveBrain();
            if (brain == null || playerTarget == null || Time.time < nextObservationTime) return;
            nextObservationTime = Time.time + observationInterval;

            Vector3 origin = transform.TransformPoint(localEyeOffset);
            Vector3 target = playerTarget.position + Vector3.up;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance > sightRange || distance < 0.01f) return;
            if (!Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance + 0.1f,
                    sightLayers, QueryTriggerInteraction.Ignore)) return;
            if (hit.transform != playerTarget && !hit.transform.IsChildOf(playerTarget)) return;

            float confidence = Mathf.Clamp01(1f - distance / (sightRange * 1.5f));
            brain.Observe(new WorldObservation(playerTarget.position, confidence, Time.timeAsDouble));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(localEyeOffset), sightRange);
        }
    }
}
