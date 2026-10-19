using System;
using System.Collections.Generic;
using Blacksite.Agents.Movement;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Rift
{
    /// <summary>Forward-search GOAP planner for pursuit, flanking, and last-known-position investigation.</summary>
    public sealed class RiftBrain : AgentBrainBase
    {
        [Flags]
        private enum Fact
        {
            None = 0,
            HasIntel = 1 << 0,
            TargetVisible = 1 << 1,
            HasFlankRoute = 1 << 2,
            AtTarget = 1 << 3,
            AtFlank = 1 << 4,
            RouteInvalidated = 1 << 5
        }

        private sealed class GoapAction
        {
            public string Name;
            public Fact Requires;
            public Fact Adds;
            public Fact Removes;
            public float Cost;
            public Vector3 Goal;
        }

        [SerializeField] private WaypointAgentMotor motor;
        [SerializeField, Min(0.1f)] private float decisionInterval = 0.4f;
        [SerializeField, Min(0.1f)] private float arrivalRadius = 0.8f;
        [SerializeField] private Transform flankPoint;
        [SerializeField] private bool hasFlankRoute = true;

        private readonly List<GoapAction> plan = new List<GoapAction>();
        private readonly List<GoapAction> actionSet = new List<GoapAction>();
        private Vector3 lastKnownPosition;
        private bool hasIntel;
        private float decisionTimer;
        private Vector3 lastIssuedGoal;
        private bool hasIssuedGoal;

        public override AgentKind Kind => AgentKind.Rift;
        public string CurrentAction { get; private set; }
        public IReadOnlyList<string> CurrentPlan { get; private set; } = Array.Empty<string>();
        public int LastSearchNodes { get; private set; }
        public event Action<string> PlanChanged;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<WaypointAgentMotor>();
        }

        public override void Observe(WorldObservation observation)
        {
            base.Observe(observation);
            lastKnownPosition = observation.Position;
            hasIntel = true;
            decisionTimer = decisionInterval;
        }

        /// <summary>Call from the world portal event; a changed door forces immediate replanning.</summary>
        public void SetPortalBlocked(string portalId, bool blocked)
        {
            if (string.IsNullOrWhiteSpace(portalId)) return;
            if (motor != null) motor.SetPortalBlocked(portalId, blocked);
            decisionTimer = decisionInterval;
        }

        public override void TickDecision(float deltaTime)
        {
            decisionTimer += deltaTime;
            if (decisionTimer < decisionInterval) return;
            decisionTimer = 0f;
            if (!hasIntel || motor == null) return;

            Fact state = Fact.HasIntel;
            if (hasFlankRoute && flankPoint != null) state |= Fact.HasFlankRoute;
            if (motor.HasArrived)
            {
                if (Vector3.Distance(transform.position, lastKnownPosition) <= arrivalRadius) state |= Fact.AtTarget;
                if (flankPoint != null && Vector3.Distance(transform.position, flankPoint.position) <= arrivalRadius)
                    state |= Fact.AtFlank;
            }
            if (LatestObservation.HasValue && Time.timeAsDouble - LatestObservation.Value.Time < 1.25)
                state |= Fact.TargetVisible;
            if (motor.IsBlocked) state |= Fact.RouteInvalidated;

            actionSet.Clear();
            actionSet.Add(new GoapAction { Name = "Pursue", Requires = Fact.HasIntel | Fact.TargetVisible,
                Adds = Fact.AtTarget, Removes = Fact.AtFlank, Cost = 1f, Goal = lastKnownPosition });
            actionSet.Add(new GoapAction { Name = "InvestigateLastKnown", Requires = Fact.HasIntel,
                Adds = Fact.AtTarget, Removes = Fact.TargetVisible, Cost = 1.2f, Goal = lastKnownPosition });
            if ((state & Fact.HasFlankRoute) != 0)
                actionSet.Add(new GoapAction { Name = "Flank", Requires = Fact.HasIntel | Fact.HasFlankRoute,
                    Adds = Fact.AtFlank, Removes = Fact.AtTarget, Cost = 0.8f, Goal = flankPoint.position });
            if ((state & Fact.RouteInvalidated) != 0)
                actionSet.Add(new GoapAction { Name = "ReplanRoute", Requires = Fact.HasIntel | Fact.RouteInvalidated,
                    Adds = Fact.HasIntel, Removes = Fact.RouteInvalidated, Cost = 0.1f,
                    Goal = motor.HasDestination ? motor.Destination : lastKnownPosition });

            Fact goal = (state & Fact.TargetVisible) != 0 && (state & Fact.HasFlankRoute) != 0
                ? Fact.AtFlank : Fact.AtTarget;
            if (!TryPlan(state, goal))
            {
                ClearPlan();
                return;
            }
            ExecuteNextAction();
        }

        private bool TryPlan(Fact initial, Fact goal)
        {
            plan.Clear();
            LastSearchNodes = 0;
            var open = new List<SearchNode> { new SearchNode(initial, 0f, null, null) };
            var bestCost = new Dictionary<Fact, float> { [initial] = 0f };
            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++) if (open[i].Cost < open[best].Cost) best = i;
                SearchNode current = open[best];
                open.RemoveAt(best);
                LastSearchNodes++;
                if ((current.State & goal) == goal)
                {
                    for (SearchNode node = current; node.Action != null; node = node.Parent)
                        plan.Add(node.Action);
                    plan.Reverse();
                    return true;
                }
                foreach (GoapAction action in actionSet)
                {
                    if ((current.State & action.Requires) != action.Requires) continue;
                    Fact next = (current.State & ~action.Removes) | action.Adds;
                    float cost = current.Cost + Mathf.Max(0.01f, action.Cost);
                    if (bestCost.TryGetValue(next, out float prior) && prior <= cost) continue;
                    bestCost[next] = cost;
                    open.Add(new SearchNode(next, cost, current, action));
                }
            }
            return false;
        }

        private void ExecuteNextAction()
        {
            if (plan.Count == 0) { ClearPlan(); return; }
            GoapAction action = plan[0];
            CurrentAction = action.Name;
            CurrentPlan = plan.ConvertAll(item => item.Name).ToArray();
            PlanChanged?.Invoke(CurrentAction);
            if (!hasIssuedGoal || Vector3.Distance(lastIssuedGoal, action.Goal) > 0.25f ||
                motor.IsBlocked || !motor.HasDestination)
            {
                motor.SetDestination(action.Goal);
                lastIssuedGoal = action.Goal;
                hasIssuedGoal = true;
            }
        }

        private void ClearPlan()
        {
            CurrentAction = null;
            CurrentPlan = Array.Empty<string>();
        }

        private sealed class SearchNode
        {
            public readonly Fact State;
            public readonly float Cost;
            public readonly SearchNode Parent;
            public readonly GoapAction Action;
            public SearchNode(Fact state, float cost, SearchNode parent, GoapAction action)
            { State = state; Cost = cost; Parent = parent; Action = action; }
        }
    }
}
