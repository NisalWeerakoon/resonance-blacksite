using System.Collections.Generic;
using UnityEngine;

namespace Blacksite.Shared
{
    public enum AgentKind { Echo, Bastion, Rift, Trace }

    public readonly struct WorldObservation
    {
        public WorldObservation(Vector3 position, float confidence, double time)
        {
            Position = position;
            Confidence = Mathf.Clamp01(confidence);
            Time = time;
        }

        public Vector3 Position { get; }
        public float Confidence { get; }
        public double Time { get; }
    }

    public interface IBlacksiteAgent
    {
        AgentKind Kind { get; }
        void Observe(WorldObservation observation);
        void TickDecision(float deltaTime);
    }

    public interface IPathPlanner
    {
        bool TryPlan(Vector3 start, Vector3 goal, ISet<string> blockedPortals,
            List<Vector3> pathBuffer);
    }

    public interface IPortalState
    {
        string PortalId { get; }
        bool IsTraversable { get; }
        event System.Action<IPortalState> Changed;
    }
}

