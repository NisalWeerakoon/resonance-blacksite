using Blacksite.Shared;

namespace Blacksite.Agents.Echo
{
    public sealed class EchoBrain : AgentBrainBase
    {
        public override AgentKind Kind => AgentKind.Echo;
        public override void TickDecision(float deltaTime) { /* Owner: Utility AI + acoustic Dijkstra. */ }
    }
}

