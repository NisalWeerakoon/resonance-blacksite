using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ThrowableDecoy : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float impactNoise = 1f;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 1.5f;
        [SerializeField, Min(0f)] private float lifetime = 12f;
        private bool emitted;

        private void Start() => Destroy(gameObject, lifetime);

        private void OnCollisionEnter(Collision collision)
        {
            if (emitted || collision.relativeVelocity.magnitude < minimumImpactSpeed) return;
            emitted = true;
            WorldSignals.EmitNoise(transform.position, impactNoise);
        }
    }
}
