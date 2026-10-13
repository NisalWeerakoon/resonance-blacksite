using System;
using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Trace
{
    /// <summary>
    /// Chooses a route from actual sightings. A movement component consumes RouteChosen;
    /// TRACE never moves its own Transform or reads the hidden player's Transform.
    /// </summary>
    public sealed class TraceBrain : AgentBrainBase
    {
        [SerializeField] private TraceWaypointGraph graph;
        [SerializeField] private TraceInterceptPoint[] interceptPoints = Array.Empty<TraceInterceptPoint>();
        [SerializeField] private float decisionIntervalSeconds = 0.5f;
        [SerializeField] private float forecastSeconds = 3f;
        [SerializeField] private float candidateRange = 25f;
        [SerializeField] private float weightedAStarWeight = 1.2f;

        private readonly TracePredictionModel prediction = new TracePredictionModel();
        private readonly HashSet<string> blockedPortals = new HashSet<string>();
        private readonly List<string> scratchPath = new List<string>();
        private readonly List<string> selectedPath = new List<string>();
        private readonly List<Vector3> selectedWaypoints = new List<Vector3>();
        private float decisionTimer;

        public override AgentKind Kind => AgentKind.Trace;
        public string SelectedNodeId { get; private set; }
        public TraceCandidateScore? LastScore { get; private set; }
        public int LastNodesExpanded { get; private set; }
        public float LastPathCost { get; private set; }
        public IReadOnlyList<Vector3> SelectedWaypoints => selectedWaypoints;
        public event Action<IReadOnlyList<Vector3>, string> RouteChosen;

        public override void Observe(WorldObservation observation)
        {
            if (prediction.Add(observation)) base.Observe(observation);
        }

        public void SetPortalBlocked(string portalId, bool blocked)
        {
            if (string.IsNullOrEmpty(portalId)) return;
            if (blocked) blockedPortals.Add(portalId);
            else blockedPortals.Remove(portalId);
            decisionTimer = decisionIntervalSeconds;
        }

        public override void TickDecision(float deltaTime)
        {
            decisionTimer += deltaTime;
            if (decisionTimer < decisionIntervalSeconds) return;
            decisionTimer = 0f;
            if (graph == null || !prediction.TryForecast(Time.timeAsDouble, forecastSeconds,
                    out Vector3 forecast, out float confidence))
            {
                ClearDecision();
                return;
            }

            string start = graph.NearestNodeId(transform.position);
            if (start == null) { ClearDecision(); return; }
            string bestNode = null;
            TraceCandidateScore bestScore = default;
            float bestPathCost = 0f;
            int bestExpanded = 0;
            List<string> bestPath = null;

            foreach (TraceInterceptPoint point in interceptPoints)
            {
                if (point == null || !graph.HasNode(point.NodeId)) continue;
                if (!GraphAStar.TryFind(graph, start, point.NodeId, blockedPortals,
                        scratchPath, out float pathCost, out int expanded,
                        Mathf.Max(1f, weightedAStarWeight))) continue;

                float playerCost = prediction.Latest.HasValue
                    ? Vector3.Distance(prediction.Latest.Value.Position, point.transform.position)
                    : float.PositiveInfinity;
                var candidate = new TraceCandidate(point.NodeId, point.transform.position,
                    point.ChokeImportance, point.HistoricalUse, pathCost, playerCost);
                if (!TraceCandidateScoring.TryScore(candidate, forecast, candidateRange,
                        out TraceCandidateScore score)) continue;
                if (score.Total * confidence <= 0.01f) continue;
                if (bestNode != null && score.Total <= bestScore.Total) continue;
                bestNode = point.NodeId;
                bestScore = score;
                bestPathCost = pathCost;
                bestExpanded = expanded;
                bestPath = new List<string>(scratchPath);
            }

            if (bestNode == null) { ClearDecision(); return; }
            bool changed = SelectedNodeId != bestNode || selectedPath.Count != bestPath.Count;
            if (!changed)
                for (int i = 0; i < bestPath.Count; i++)
                    if (selectedPath[i] != bestPath[i]) { changed = true; break; }
            SelectedNodeId = bestNode;
            LastScore = bestScore;
            LastPathCost = bestPathCost;
            LastNodesExpanded = bestExpanded;
            selectedPath.Clear();
            selectedPath.AddRange(bestPath);
            selectedWaypoints.Clear();
            foreach (string node in selectedPath) selectedWaypoints.Add(graph.Position(node));
            if (changed) RouteChosen?.Invoke(selectedWaypoints, bestNode);
        }

        private void ClearDecision()
        {
            bool hadRoute = SelectedNodeId != null;
            SelectedNodeId = null;
            LastScore = null;
            LastPathCost = 0f;
            LastNodesExpanded = 0;
            selectedPath.Clear();
            selectedWaypoints.Clear();
            if (hadRoute) RouteChosen?.Invoke(selectedWaypoints, null);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            for (int i = 1; i < selectedWaypoints.Count; i++)
                Gizmos.DrawLine(selectedWaypoints[i - 1], selectedWaypoints[i]);
        }
    }
}

