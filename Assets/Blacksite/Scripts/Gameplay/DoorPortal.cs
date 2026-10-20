using System;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class DoorPortal : MonoBehaviour, IPortalState
    {
        [SerializeField] private string portalId = "D01";
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private GameObject closedVisual;
        [SerializeField] private bool startsOpen;
        private bool isOpen;

        public string PortalId => portalId;
        public bool IsTraversable => isOpen;
        public event Action<IPortalState> Changed;

        private void Awake()
        {
            if (blockingCollider == null) blockingCollider = GetComponent<Collider>();
            SetOpen(startsOpen);
        }

        public void Toggle() => SetOpen(!isOpen);

        public void ConfigureSandbox(string id, Collider blocker, GameObject visual, bool open)
        {
            portalId = id;
            blockingCollider = blocker;
            closedVisual = visual;
            startsOpen = open;
            SetOpen(open);
        }

        public void SetOpen(bool open)
        {
            if (string.IsNullOrWhiteSpace(portalId))
            {
                Debug.LogError("DoorPortal requires a stable portal ID", this);
                return;
            }
            bool changed = isOpen != open;
            isOpen = open;
            if (blockingCollider != null) blockingCollider.enabled = !open;
            if (closedVisual != null) closedVisual.SetActive(!open);
            if (changed)
            {
                Changed?.Invoke(this);
                WorldSignals.NotifyDoorStateChanged(portalId, isOpen);
            }
        }
    }
}
