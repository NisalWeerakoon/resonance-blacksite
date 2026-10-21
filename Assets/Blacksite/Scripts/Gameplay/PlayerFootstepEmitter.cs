using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class PlayerFootstepEmitter : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float distancePerStep = 2.2f;
        [SerializeField, Range(0f, 1f)] private float loudness = .24f;
        private Vector3 previous;
        private float travelled;

        private void OnEnable() => previous = transform.position;

        private void Update()
        {
            Vector3 movement = transform.position - previous;
            movement.y = 0f;
            previous = transform.position;
            if (movement.magnitude > 2f) { travelled = 0f; return; }
            travelled += movement.magnitude;
            if (travelled < distancePerStep) return;
            travelled -= distancePerStep;
            WorldSignals.EmitNoise(transform.position, loudness);
        }
    }
}
