using System;
using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Bastion
{
    public sealed class BastionBrain : AgentBrainBase
    {
        [Header("Perception")]
        [SerializeField] private Transform eye;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private LayerMask visibilityMask = ~0;
        [SerializeField, Range(1f, 180f)] private float fieldOfView = 100f;
        [SerializeField, Min(1f)] private float sightRange = 25f;
        [SerializeField, Min(0f)] private float memorySeconds = 5f;

        [Header("Decision")]
        [SerializeField, Min(.1f)] private float decisionInterval = .4f;
        [SerializeField, Range(0f, 1f)] private float healthFraction = 1f;
        [SerializeField, Min(1f)] private float preferredRange = 10f;
        [SerializeField, Min(1f)] private float pathCostScale = 30f;
        [SerializeField, Min(.1f)] private float arrivalRadius = .75f;
        [SerializeField] private CoverPoint[] coverPoints = Array.Empty<CoverPoint>();
        [SerializeField] private MonoBehaviour pathPlannerSource;
        [SerializeField] private MonoBehaviour[] portalSources = Array.Empty<MonoBehaviour>();

        private readonly HashSet<string> blockedPortals = new HashSet<string>();
        private readonly List<Vector3> pathBuffer = new List<Vector3>();
        private readonly List<Vector3> chosenPath = new List<Vector3>();
        private IPathPlanner planner;
        private IBehaviorNode tree;
        private CoverPoint selectedCover;
        private float nextTick;
        private bool visibleNow;
        private bool wasVisible;
        private bool searchMode;
        private bool recovering;
        private bool emergencyRecovery;
        private bool routeRequested;
        private string currentNode = "Uninitialized";

        public override AgentKind Kind => AgentKind.Bastion;
        public string CurrentNode => currentNode;
        public CoverPoint SelectedCover => selectedCover;
        public event Action<IReadOnlyList<Vector3>> RouteChosen;
        public event Action<Transform> AttackRequested;

        private void Awake()
        {
            if (eye == null) eye = transform;
            planner = pathPlannerSource as IPathPlanner;
            tree = BuildTree();
        }

        public void ConfigureSandbox(Transform sight, Transform target, CoverPoint[] points,
            MonoBehaviour plannerSource, MonoBehaviour[] portals)
        {
            eye = sight;
            playerTarget = target;
            coverPoints = points ?? Array.Empty<CoverPoint>();
            pathPlannerSource = plannerSource;
            portalSources = portals ?? Array.Empty<MonoBehaviour>();
            planner = pathPlannerSource as IPathPlanner;
            routeRequested = false;
        }

        private void OnEnable()
        {
            foreach (MonoBehaviour source in portalSources)
            {
                if (!(source is IPortalState portal)) continue;
                portal.Changed += OnPortalChanged;
                OnPortalChanged(portal);
            }
        }

        private void OnDisable()
        {
            foreach (MonoBehaviour source in portalSources)
                if (source is IPortalState portal) portal.Changed -= OnPortalChanged;
        }

        public void SetPortalBlocked(string portalId, bool blocked)
        {
            if (string.IsNullOrEmpty(portalId)) return;
            if (blocked) blockedPortals.Add(portalId);
            else blockedPortals.Remove(portalId);
            routeRequested = false;
        }

        public void SetHealthFraction(float fraction) => healthFraction = Mathf.Clamp01(fraction);
        public void SetSearchMode(bool searching) => searchMode = searching;
        public void SetRecovering(bool value) => recovering = value;
        public void SetEmergencyRecovery(bool value) => emergencyRecovery = value;
        public void RequestReplan() => routeRequested = false;

        private void OnPortalChanged(IPortalState portal)
        {
            if (portal == null || string.IsNullOrEmpty(portal.PortalId)) return;
            bool blocked = false;
            foreach (MonoBehaviour source in portalSources)
                if (source is IPortalState state && state.PortalId == portal.PortalId &&
                    !state.IsTraversable) blocked = true;
            SetPortalBlocked(portal.PortalId, blocked);
        }

        public override void TickDecision(float deltaTime)
        {
            if (Time.time < nextTick || tree == null) return;
            nextTick = Time.time + decisionInterval;
            visibleNow = CanSeePlayer();
            if (visibleNow != wasVisible) routeRequested = false;
            wasVisible = visibleNow;
            if (visibleNow)
            {
                searchMode = false;
                Observe(new WorldObservation(playerTarget.position, 1f, Time.timeAsDouble));
            }
            else if (LatestObservation.HasValue &&
                     Time.timeAsDouble - LatestObservation.Value.Time <= memorySeconds)
            {
                // Sweep the full surroundings while following fresh last-seen intel;
                // otherwise a retreat turn can leave the player behind its cone.
                searchMode = true;
            }
            tree.Tick();
        }

        private IBehaviorNode BuildTree()
        {
            return new SelectorNode(
                new SequenceNode(new ConditionNode(() => healthFraction <= 0f),
                    new ActionNode(() => Status("Dead", NodeStatus.Running))),
                new SequenceNode(new ConditionNode(() => recovering || healthFraction <= .375f),
                    new SelectorNode(
                        new SequenceNode(new ConditionNode(() => !emergencyRecovery),
                            new ActionNode(SelectCover), new ActionNode(MoveToCover),
                            new ActionNode(() => Status("RecoveringInCover", NodeStatus.Running))),
                        new ActionNode(() => Status(recovering ? "RecoveringAtFallback" : "LowHealthNoCover",
                            NodeStatus.Running)))),
                new SequenceNode(new ConditionNode(HasThreat),
                    new SelectorNode(
                        new SequenceNode(new ConditionNode(() => visibleNow),
                            new SelectorNode(
                                new SequenceNode(new ConditionNode(GoodCurrentCover),
                                    new ActionNode(Attack)),
                                new SequenceNode(new ActionNode(SelectCover),
                                    new ActionNode(MoveToCover), new ActionNode(Attack)),
                                new ActionNode(() => Status("NoCoverGuard", NodeStatus.Running)))),
                        new ActionNode(SearchLastSeen))),
                new ActionNode(() => Status("PatrolPlaceholder", NodeStatus.Running)));
        }

        private NodeStatus Status(string label, NodeStatus result)
        {
            currentNode = label;
            return result;
        }

        private bool HasThreat()
        {
            return visibleNow || LatestObservation.HasValue &&
                Time.timeAsDouble - LatestObservation.Value.Time <= memorySeconds;
        }

        private bool CanSeePlayer()
        {
            if (playerTarget == null) return false;
            Vector3 from = eye.position;
            Vector3 to = playerTarget.position + Vector3.up;
            Vector3 delta = to - from;
            if (delta.magnitude > sightRange ||
                !searchMode && Vector3.Angle(eye.forward, delta) > fieldOfView * .5f) return false;
            return !Physics.Linecast(from, to, out RaycastHit hit, visibilityMask,
                QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(playerTarget);
        }

        private bool GoodCurrentCover()
        {
            return selectedCover != null &&
                Vector3.Distance(transform.position, selectedCover.transform.position) <= arrivalRadius;
        }

        private NodeStatus SelectCover()
        {
            if (planner == null || playerTarget == null) return Status("NoPlannerOrTarget", NodeStatus.Failure);
            float bestScore = float.NegativeInfinity;
            CoverPoint best = null;
            List<Vector3> bestPath = null;
            Vector3 targetEye = playerTarget.position + Vector3.up;
            foreach (CoverPoint point in coverPoints)
            {
                if (point == null || point.Occupied) continue;
                pathBuffer.Clear();
                if (!planner.TryPlan(transform.position, point.transform.position,
                    blockedPortals, pathBuffer) || pathBuffer.Count == 0) continue;
                float pathLength = 0f;
                Vector3 previous = transform.position;
                foreach (Vector3 waypoint in pathBuffer)
                {
                    pathLength += Vector3.Distance(previous, waypoint);
                    previous = waypoint;
                }
                bool protectedBody = Physics.Linecast(targetEye,
                    point.transform.position + Vector3.up, out RaycastHit bodyHit,
                    visibilityMask, QueryTriggerInteraction.Ignore) &&
                    !bodyHit.transform.IsChildOf(playerTarget);
                bool clearPeek = !Physics.Linecast(point.PeekPosition, targetEye,
                    out RaycastHit peekHit, visibilityMask, QueryTriggerInteraction.Ignore) ||
                    peekHit.transform.IsChildOf(playerTarget);
                float distance = Vector3.Distance(point.transform.position, playerTarget.position);
                var factors = new CoverFactors(protectedBody ? 1f : 0f,
                    1f - Mathf.Abs(distance - preferredRange) / preferredRange,
                    clearPeek ? 1f : 0f, point.StructuralSafety,
                    pathLength / pathCostScale);
                float score = CoverScoring.Score(factors, CoverScoring.Default);
                Debug.Log($"BASTION {point.CoverId}: score={score:F2}, protection={factors.Protection:F2}, range={factors.Distance:F2}, angle={factors.FiringAngle:F2}, safety={factors.Safety:F2}, path={factors.PathCost:F2}", point);
                if (score <= bestScore) continue;
                bestScore = score;
                best = point;
                bestPath = new List<Vector3>(pathBuffer);
            }
            if (best == null) return Status("NoReachableCover", NodeStatus.Failure);
            if (selectedCover != best) routeRequested = false;
            selectedCover = best;
            chosenPath.Clear();
            chosenPath.AddRange(bestPath);
            return Status("CoverSelected:" + best.CoverId, NodeStatus.Success);
        }

        private NodeStatus MoveToCover()
        {
            if (selectedCover == null) return Status("CoverLost", NodeStatus.Failure);
            if (GoodCurrentCover()) return Status("AtCover", NodeStatus.Success);
            if (RouteChosen == null) return Status("MotorNotConnected", NodeStatus.Failure);
            if (!routeRequested)
            {
                RouteChosen.Invoke(chosenPath);
                routeRequested = true;
            }
            return Status("MoveToCover", NodeStatus.Running);
        }

        private NodeStatus Attack()
        {
            if (!visibleNow || playerTarget == null) return NodeStatus.Failure;
            AttackRequested?.Invoke(playerTarget);
            return Status("AttackRequest", NodeStatus.Running);
        }

        private NodeStatus SearchLastSeen()
        {
            if (!LatestObservation.HasValue) return NodeStatus.Failure;
            if (planner == null || RouteChosen == null) return Status("SearchGuard", NodeStatus.Running);
            pathBuffer.Clear();
            if (!planner.TryPlan(transform.position, LatestObservation.Value.Position,
                blockedPortals, pathBuffer)) return Status("LastSeenUnreachable", NodeStatus.Running);
            if (!routeRequested)
            {
                chosenPath.Clear();
                chosenPath.AddRange(pathBuffer);
                RouteChosen.Invoke(chosenPath);
                routeRequested = true;
            }
            return Status("SearchLastSeen", NodeStatus.Running);
        }
    }
}
