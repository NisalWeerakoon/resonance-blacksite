using Blacksite.Agents.Bastion;
using Blacksite.Agents.Rift;
using Blacksite.Agents.Trace;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Movement
{
    /// <summary>Forwards door state changes to an agent planner so it can replan around blocked portals.</summary>
    [DisallowMultipleComponent]
    public sealed class AgentPortalStateBridge : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour agentBrain;
        [SerializeField] private MonoBehaviour[] portalComponents;

        private void Awake()
        {
            ResolveBrain();
        }

        private void ResolveBrain()
        {
            if (agentBrain is IBlacksiteAgent) return;
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (!(component is IBlacksiteAgent)) continue;
                agentBrain = component;
                return;
            }
        }

        private void OnEnable()
        {
            ResolveBrain();
            if (portalComponents == null) return;
            foreach (MonoBehaviour component in portalComponents)
            {
                if (!(component is IPortalState portal)) continue;
                portal.Changed += OnPortalChanged;
                OnPortalChanged(portal);
            }
        }

        private void OnDisable()
        {
            if (portalComponents == null) return;
            foreach (MonoBehaviour component in portalComponents)
                if (component is IPortalState portal) portal.Changed -= OnPortalChanged;
        }

        private void OnPortalChanged(IPortalState portal)
        {
            if (portal == null || string.IsNullOrWhiteSpace(portal.PortalId)) return;
            bool blocked = !portal.IsTraversable;
            if (agentBrain is RiftBrain rift) rift.SetPortalBlocked(portal.PortalId, blocked);
            else if (agentBrain is TraceBrain trace) trace.SetPortalBlocked(portal.PortalId, blocked);
            else if (agentBrain is BastionBrain bastion) bastion.SetPortalBlocked(portal.PortalId, blocked);
        }
    }
}
