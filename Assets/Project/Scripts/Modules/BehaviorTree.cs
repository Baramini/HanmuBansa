using System.Collections.Generic;

namespace BrmnModules.AI
{
    public enum NodeStatus { Success, Failure, Running }

    public abstract class Node
    {
        public abstract NodeStatus Tick();
    }

    // if one fails is fail
    public class Sequence : Node
    {
        private readonly List<Node> children;

        public Sequence(params Node[] nodes)
        {
            children = new List<Node>(nodes);
        }

        public override NodeStatus Tick()
        {
            foreach (Node child in children)
            {
                NodeStatus status = child.Tick();
                if (status != NodeStatus.Success) return status;
            }
            return NodeStatus.Success;
        }
    }

    // if one successes is success
    public class Selector : Node
    {
        private readonly List<Node> children;

        public Selector(params Node[] nodes)
        {
            children = new List<Node>(nodes);
        }

        public override NodeStatus Tick()
        {
            foreach (Node child in children)
            {
                NodeStatus status = child.Tick();
                if (status != NodeStatus.Failure) return status;
            }
            return NodeStatus.Failure;
        }
    }

    public class Condition : Node
    {
        private readonly System.Func<bool> predicate;

        public Condition(System.Func<bool> predicate)
        {
            this.predicate = predicate;
        }

        public override NodeStatus Tick()
        {
            return predicate() ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    public class ActionNode : Node
    {
        private readonly System.Func<NodeStatus> action;

        public ActionNode(System.Func<NodeStatus> action)
        {
            this.action = action;
        }

        public override NodeStatus Tick()
        {
            return action();
        }
    }

    public class Inverter : Node
    {
        private readonly Node child;

        public Inverter(Node child)
        {
            this.child = child;
        }

        public override NodeStatus Tick()
        {
            NodeStatus status = child.Tick();
            if (status == NodeStatus.Success) return NodeStatus.Failure;
            if (status == NodeStatus.Failure) return NodeStatus.Success;
            return NodeStatus.Running;
        }
    }
}
