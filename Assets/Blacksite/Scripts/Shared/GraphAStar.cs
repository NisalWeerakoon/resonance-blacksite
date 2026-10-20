using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blacksite.Shared
{
    public readonly struct GraphEdge
    {
        public GraphEdge(string destination, float cost, string portalId = null)
        {
            Destination = destination;
            Cost = cost;
            PortalId = portalId;
        }
        public string Destination { get; }
        public float Cost { get; }
        public string PortalId { get; }
    }

    public interface IWaypointGraph
    {
        Vector3 Position(string nodeId);
        IEnumerable<GraphEdge> Neighbors(string nodeId);
    }

    public static class GraphAStar
    {
        // Use an admissible heuristic for an optimal route. Weight > 1 enables Weighted A*.
        public static bool TryFind(IWaypointGraph graph, string start, string goal,
            ISet<string> blockedPortals, List<string> result, float heuristicWeight = 1f)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (heuristicWeight < 1f) throw new ArgumentOutOfRangeException(nameof(heuristicWeight));
            result.Clear();
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(goal)) return false;
            var open = new List<string> { start };
            var closed = new HashSet<string>();
            var previous = new Dictionary<string, string>();
            var cost = new Dictionary<string, float> { [start] = 0f };
            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = float.PositiveInfinity;
                for (int i = 0; i < open.Count; i++)
                {
                    string candidate = open[i];
                    float score = cost[candidate] + heuristicWeight *
                        Vector3.Distance(graph.Position(candidate), graph.Position(goal));
                    if (score < bestScore) { bestScore = score; bestIndex = i; }
                }
                string current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal)
                {
                    for (string node = goal; node != start; node = previous[node]) result.Add(node);
                    result.Add(start);
                    result.Reverse();
                    return true;
                }
                closed.Add(current);
                foreach (GraphEdge edge in graph.Neighbors(current))
                {
                    if (edge.Cost < 0f) throw new InvalidOperationException("Negative graph edge cost");
                    if (edge.PortalId != null && blockedPortals != null && blockedPortals.Contains(edge.PortalId)) continue;
                    float nextCost = cost[current] + edge.Cost;
                    if (cost.TryGetValue(edge.Destination, out float oldCost) && nextCost >= oldCost) continue;
                    cost[edge.Destination] = nextCost;
                    previous[edge.Destination] = current;
                    closed.Remove(edge.Destination);
                    if (!open.Contains(edge.Destination)) open.Add(edge.Destination);
                }
            }
            return false;
        }
    }
}
