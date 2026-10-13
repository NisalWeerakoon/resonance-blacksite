using Blacksite.Shared;

namespace Blacksite.Agents.Rift
{
    public sealed class RiftBrain : AgentBrainBase
    {
        public override AgentKind Kind => AgentKind.Rift;
        public override void TickDecision(float deltaTime) { /* Owner: GOAP + replanning. */ }
    }
}

