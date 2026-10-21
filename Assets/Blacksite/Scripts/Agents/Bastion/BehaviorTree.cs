using System;

namespace Blacksite.Agents.Bastion
{
    public enum NodeStatus { Running, Success, Failure }

    public interface IBehaviorNode
    {
        NodeStatus Tick();
    }

    public sealed class ConditionNode : IBehaviorNode
    {
        private readonly Func<bool> condition;
        public ConditionNode(Func<bool> condition) => this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
        public NodeStatus Tick() => condition() ? NodeStatus.Success : NodeStatus.Failure;
    }

    public sealed class ActionNode : IBehaviorNode
    {
        private readonly Func<NodeStatus> action;
        public ActionNode(Func<NodeStatus> action) => this.action = action ?? throw new ArgumentNullException(nameof(action));
        public NodeStatus Tick() => action();
    }

    public sealed class SelectorNode : IBehaviorNode
    {
        private readonly IBehaviorNode[] children;
        public SelectorNode(params IBehaviorNode[] children) => this.children = children;
        public NodeStatus Tick()
        {
            foreach (IBehaviorNode child in children)
            {
                NodeStatus result = child.Tick();
                if (result != NodeStatus.Failure) return result;
            }
            return NodeStatus.Failure;
        }
    }

    public sealed class SequenceNode : IBehaviorNode
    {
        private readonly IBehaviorNode[] children;
        public SequenceNode(params IBehaviorNode[] children) => this.children = children;
        public NodeStatus Tick()
        {
            foreach (IBehaviorNode child in children)
            {
                NodeStatus result = child.Tick();
                if (result != NodeStatus.Success) return result;
            }
            return NodeStatus.Success;
        }
    }
}
