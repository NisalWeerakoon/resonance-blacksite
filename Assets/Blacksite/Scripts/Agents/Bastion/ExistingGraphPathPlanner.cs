using System.Collections.Generic;
using Blacksite.Agents.Trace;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Bastion
{
    // Reuses the existing graph and shared A*; it is not a second navigation algorithm.
    public sealed class ExistingGraphPathPlanner : MonoBehaviour, IPathPlanner
    {
        [SerializeField] private TraceWaypointGraph graph;
        private readonly List<string> nodeBuffer = new List<string>();

        public void ConfigureSandbox(TraceWaypointGraph waypointGraph) => graph = waypointGraph;

        public bool TryPlan(Vector3 start, Vector3 goal, ISet<string> blockedPortals,
            List<Vector3> pathBuffer)
        {
            if (pathBuffer == null) return false;
            pathBuffer.Clear();
            if (graph == null) return false;
            string from = graph.NearestNodeId(start);
            string to = graph.NearestNodeId(goal);
            if (!GraphAStar.TryFind(graph, from, to, blockedPortals, nodeBuffer)) return false;
            foreach (string id in nodeBuffer) pathBuffer.Add(graph.Position(id));
            if (pathBuffer.Count == 0 || Vector3.Distance(pathBuffer[pathBuffer.Count - 1], goal) > .1f)
                pathBuffer.Add(goal);
            return true;
        }
    }
}
