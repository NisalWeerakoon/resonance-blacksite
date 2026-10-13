using Blacksite.Shared;

namespace Blacksite.Agents.Bastion
{
    public sealed class BastionBrain : AgentBrainBase
    {
        public override AgentKind Kind => AgentKind.Bastion;
        public override void TickDecision(float deltaTime) { /* Owner: behaviour tree + cover scoring. */ }
    }
}

