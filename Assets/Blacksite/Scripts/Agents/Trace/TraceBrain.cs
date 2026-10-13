using Blacksite.Shared;

namespace Blacksite.Agents.Trace
{
    public sealed class TraceBrain : AgentBrainBase
    {
        public override AgentKind Kind => AgentKind.Trace;
        public override void TickDecision(float deltaTime) { /* Owner: prediction + Weighted A*. */ }
    }
}

