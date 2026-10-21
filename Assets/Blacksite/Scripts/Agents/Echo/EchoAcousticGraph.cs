using System;
using System.Collections.Generic;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// Acoustic attenuation state. Sealed doors block movement, but this demo
    /// still permits heavily attenuated SOUND through the door (cost 6).
    /// A wall without a portal has no acoustic edge.
    /// </summary>
    public enum EchoDoorAcousticState { Open, Closed, Sealed }

    public readonly struct EchoAcousticNeighbor
    {
        public EchoAcousticNeighbor(string roomId, string portalId, float cost)
        {
            RoomId = roomId;
            PortalId = portalId;
            Cost = cost;
        }
        public string RoomId { get; }
        public string PortalId { get; }
        public float Cost { get; }
    }

    public sealed class EchoAcousticGraph
    {
        private sealed class Portal
        {
            public string Id;
            public string RoomA;
            public string RoomB;
            public EchoDoorAcousticState State;
        }

        private readonly Dictionary<string, Portal> portals =
            new Dictionary<string, Portal>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<Portal>> adjacency =
            new Dictionary<string, List<Portal>>(StringComparer.Ordinal);

        private EchoAcousticGraph()
        {
            foreach (string roomId in EchoRoomIds.All)
                adjacency.Add(roomId, new List<Portal>());
        }

        /// <summary>Matches the agreed BLACKSITE-7 D01–D07 room topology.</summary>
        public static EchoAcousticGraph CreateDefault()
        {
            var graph = new EchoAcousticGraph();
            graph.Add("D01", EchoRoomIds.Entry, EchoRoomIds.Hub);
            graph.Add("D02", EchoRoomIds.Hub, EchoRoomIds.Acoustic);
            graph.Add("D03", EchoRoomIds.Acoustic, EchoRoomIds.Security);
            graph.Add("D04", EchoRoomIds.Security, EchoRoomIds.Maintenance);
            graph.Add("D05", EchoRoomIds.Hub, EchoRoomIds.Maintenance);
            graph.Add("D06", EchoRoomIds.Maintenance, EchoRoomIds.Core);
            graph.Add("D07", EchoRoomIds.Hub, EchoRoomIds.Core);
            return graph;
        }

        public int RoomCount => adjacency.Count;
        public int PortalCount => portals.Count;

        private void Add(string portalId, string a, string b)
        {
            if (!EchoRoomIds.IsValid(a) || !EchoRoomIds.IsValid(b) || a == b)
                throw new ArgumentException("Invalid acoustic portal endpoints");
            var portal = new Portal
            {
                Id = portalId,
                RoomA = a,
                RoomB = b,
                State = EchoDoorAcousticState.Open
            };
            portals.Add(portalId, portal);
            adjacency[a].Add(portal);
            adjacency[b].Add(portal);
        }

        /// <summary>For graph tests or permanently missing portals; unlike a sealed door,
        /// removing a portal also removes sound transmission.</summary>
        public bool TryRemovePortal(string portalId)
        {
            if (!portals.TryGetValue(portalId ?? string.Empty, out Portal portal)) return false;
            portals.Remove(portalId);
            adjacency[portal.RoomA].Remove(portal);
            adjacency[portal.RoomB].Remove(portal);
            return true;
        }

        public bool TrySetPortalState(string portalId, EchoDoorAcousticState state)
        {
            if (!portals.TryGetValue(portalId ?? string.Empty, out Portal portal)) return false;
            if (!Enum.IsDefined(typeof(EchoDoorAcousticState), state)) return false;
            portal.State = state;
            return true;
        }

        public bool TryGetPortalState(string portalId, out EchoDoorAcousticState state)
        {
            if (portals.TryGetValue(portalId ?? string.Empty, out Portal portal))
            {
                state = portal.State;
                return true;
            }
            state = default;
            return false;
        }

        public static float Attenuation(EchoDoorAcousticState state)
        {
            switch (state)
            {
                case EchoDoorAcousticState.Open: return 1f;
                case EchoDoorAcousticState.Closed: return 4f;
                case EchoDoorAcousticState.Sealed: return 6f;
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        public IEnumerable<EchoAcousticNeighbor> Neighbors(string roomId)
        {
            if (!adjacency.TryGetValue(roomId ?? string.Empty, out List<Portal> edges))
                yield break;

            foreach (Portal portal in edges)
            {
                string destination = portal.RoomA == roomId ? portal.RoomB : portal.RoomA;
                yield return new EchoAcousticNeighbor(destination, portal.Id, Attenuation(portal.State));
            }
        }

        /// <summary>Direct edge cost, not the shortest multi-room acoustic path.</summary>
        public bool TryGetDirectCost(string fromRoomId, string toRoomId, out float cost)
        {
            foreach (EchoAcousticNeighbor neighbor in Neighbors(fromRoomId))
            {
                if (neighbor.RoomId == toRoomId)
                {
                    cost = neighbor.Cost;
                    return true;
                }
            }
            cost = float.PositiveInfinity;
            return false;
        }
    }
}
