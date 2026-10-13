using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents
{
    public abstract class AgentBrainBase : MonoBehaviour, IBlacksiteAgent
    {
        public abstract AgentKind Kind { get; }
        protected WorldObservation? LatestObservation { get; private set; }

        public virtual void Observe(WorldObservation observation) => LatestObservation = observation;
        public abstract void TickDecision(float deltaTime);

        private void Update() => TickDecision(Time.deltaTime);
    }
}

