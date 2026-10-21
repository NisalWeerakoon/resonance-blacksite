using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class PlayerPulseWeapon : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(.1f)] private float range = 30f;
        [SerializeField, Min(.05f)] private float fireInterval = .35f;
        private float nextFire;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) || Time.time < nextFire || viewCamera == null) return;
            nextFire = Time.time + fireInterval;
            WorldSignals.EmitNoise(viewCamera.transform.position, 1f);
            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                    out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore)) return;
            SubmissionAgentHealth health = hit.collider.GetComponentInParent<SubmissionAgentHealth>();
            if (health != null) health.TakeHit();
        }
    }
}
