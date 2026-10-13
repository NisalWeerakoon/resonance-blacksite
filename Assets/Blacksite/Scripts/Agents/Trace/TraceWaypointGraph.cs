using System;
using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Trace
{
    public sealed class TraceWaypointGraph : MonoBehaviour, IWaypointGraph
    {
        [Serializable] private sealed class Node
        {
            public string id;
            public Transform point;
        }

        [Serializable] private sealed class Link
        {
            public string from;
            public string to;
            [Min(0f)] public float cost = 1f;
            public string portalId;
            public bool bidirectional = true;
        }

        [SerializeField] private Node[] nodes = Array.Empty<Node>();
        [SerializeField] private Link[] links = Array.Empty<Link>();
        private readonly Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, List<GraphEdge>> adjacency =
            new Dictionary<string, List<GraphEdge>>();

        private void Awake() => Rebuild();

        public void Rebuild()
        {
            positions.Clear();
            adjacency.Clear();
            foreach (Node node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id) || node.point == null)
                    continue;
                positions[node.id] = node.point.position;
                adjacency[node.id] = new List<GraphEdge>();
            }
            foreach (Link link in links)
            {
                if (link == null || !adjacency.ContainsKey(link.from) ||
                    !adjacency.ContainsKey(link.to)) continue;
                float cost = Mathf.Max(link.cost,
                    Vector3.Distance(positions[link.from], positions[link.to]));
                adjacency[link.from].Add(new GraphEdge(link.to, cost, link.portalId));
                if (link.bidirectional)
                    adjacency[link.to].Add(new GraphEdge(link.from, cost, link.portalId));
            }
        }

        public Vector3 Position(string nodeId) => positions[nodeId];
        public IEnumerable<GraphEdge> Neighbors(string nodeId) =>
            adjacency.TryGetValue(nodeId, out List<GraphEdge> edges)
                ? edges : Array.Empty<GraphEdge>();

        public string NearestNodeId(Vector3 position)
        {
            string best = null;
            float distance = float.PositiveInfinity;
            foreach (var pair in positions)
            {
                float candidate = (pair.Value - position).sqrMagnitude;
                if (candidate < distance) { best = pair.Key; distance = candidate; }
            }
            return best;
        }

        public bool HasNode(string nodeId) => nodeId != null && positions.ContainsKey(nodeId);
    }
}
