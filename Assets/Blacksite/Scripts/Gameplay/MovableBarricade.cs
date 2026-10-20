using System;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class MovableBarricade : MonoBehaviour, IPortalState
    {
        [SerializeField] private string portalId = "D02";
        [SerializeField] private Collider chokePointVolume;
        [SerializeField, Min(0.1f)] private float pushImpulse = 3f;
        private Collider bodyCollider;
        private Rigidbody body;
        private bool blocked;
        private Vector3 lastPublishedPosition;

        public string PortalId => portalId;
        public bool IsTraversable => !blocked;
        public event Action<IPortalState> Changed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<Collider>();
            lastPublishedPosition = transform.position;
            RefreshBlockage();
        }

        public void ConfigureSandbox(string id, Collider chokeVolume)
        {
            portalId = id;
            chokePointVolume = chokeVolume;
            if (bodyCollider == null) bodyCollider = GetComponent<Collider>();
            RefreshBlockage();
        }

        private void FixedUpdate()
        {
            if ((transform.position - lastPublishedPosition).sqrMagnitude > 0.01f)
            {
                lastPublishedPosition = transform.position;
                WorldSignals.NotifyBarricadeMoved(bodyCollider.bounds);
            }
            RefreshBlockage();
        }

        public void Push(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                body.AddForce(direction.normalized * pushImpulse, ForceMode.Impulse);
        }

        private void RefreshBlockage()
        {
            if (chokePointVolume == null) return;
            bool nowBlocked = bodyCollider.bounds.Intersects(chokePointVolume.bounds);
            if (nowBlocked == blocked) return;
            blocked = nowBlocked;
            Changed?.Invoke(this);
        }
    }
}
