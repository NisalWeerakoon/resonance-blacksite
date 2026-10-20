using System;
using System.Collections.Generic;

namespace Blacksite.Agents.Echo
{
    public sealed class EchoAcousticResult
    {
        public bool Success { get; internal set; }
        public float Cost { get; internal set; }
        public int NodesExpanded { get; internal set; }
        public List<string> Rooms { get; } = new List<string>();
    }

    /// <summary>Nonnegative acoustic costs; separate from spatial GraphAStar.</summary>
    public static class EchoAcousticDijkstra
    {
        public static EchoAcousticResult Find(EchoAcousticGraph graph, string source, string listener)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var result = new EchoAcousticResult { Cost = float.PositiveInfinity };
            if (!EchoRoomIds.IsValid(source) || !EchoRoomIds.IsValid(listener)) return result;

            var dist = new Dictionary<string, float> { [source] = 0f };
            var prev = new Dictionary<string, string>();
            var remaining = new HashSet<string>(EchoRoomIds.All);

            while (remaining.Count != 0)
            {
                string nearest = null;
                float best = float.PositiveInfinity;
                foreach (string node in remaining)
                {
                    if (dist.TryGetValue(node, out float candidate) && candidate < best)
                    {
                        best = candidate;
                        nearest = node;
                    }
                }
                if (nearest == null) break;
                remaining.Remove(nearest);
                result.NodesExpanded++;
                if (nearest == listener)
                {
                    result.Success = true;
                    result.Cost = best;
                    for (string node = listener; node != source; node = prev[node])
                        result.Rooms.Add(node);
                    result.Rooms.Add(source);
                    result.Rooms.Reverse();
                    return result;
                }
                foreach (EchoAcousticNeighbor edge in graph.Neighbors(nearest))
                {
                    if (edge.Cost < 0f || float.IsNaN(edge.Cost))
                        throw new InvalidOperationException("Acoustic costs must be nonnegative");
                    if (!remaining.Contains(edge.RoomId)) continue;
                    float next = best + edge.Cost;
                    if (!dist.TryGetValue(edge.RoomId, out float old) || next < old)
                    {
                        dist[edge.RoomId] = next;
                        prev[edge.RoomId] = nearest;
                    }
                }
            }
            return result;
        }
    }
}
