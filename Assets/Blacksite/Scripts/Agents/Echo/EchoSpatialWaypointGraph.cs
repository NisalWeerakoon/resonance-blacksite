using System;
using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// A small demo IWaypointGraph; uses the team's shared GraphAStar for travel.
    /// Replace with the project's production IWaypointGraph once it is populated.
    /// </summary>
    public sealed class EchoSpatialWaypointGraph : IWaypointGraph
    {
        public const string BypassTop = "BYPASS_TOP";
        public const string BypassCorner = "BYPASS_CORNER";
        private readonly Dictionary<string, Vector3> positions =
            new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<GraphEdge>> adjacency =
            new Dictionary<string, List<GraphEdge>>(StringComparer.Ordinal);

        public EchoSpatialWaypointGraph()
        {
            AddNode(EchoRoomIds.Entry, -29f, 0f);
            AddNode(EchoRoomIds.Hub, -12f, 0f);
            AddNode(EchoRoomIds.Acoustic, -12f, 18f);
            AddNode(EchoRoomIds.Security, 8f, 18f);
            AddNode(EchoRoomIds.Maintenance, 8f, 0f);
            AddNode(EchoRoomIds.Core, 8f, -18f);
            AddNode(BypassTop, -12f, -9f);
            AddNode(BypassCorner, -12f, -18f);
            Connect(EchoRoomIds.Entry, EchoRoomIds.Hub, "D01");
            Connect(EchoRoomIds.Hub, EchoRoomIds.Acoustic, "D02");
            Connect(EchoRoomIds.Acoustic, EchoRoomIds.Security, "D03");
            Connect(EchoRoomIds.Security, EchoRoomIds.Maintenance, "D04");
            Connect(EchoRoomIds.Hub, EchoRoomIds.Maintenance, "D05");
            Connect(EchoRoomIds.Maintenance, EchoRoomIds.Core, "D06");
            Connect(EchoRoomIds.Hub, BypassTop, "D07");
            Connect(BypassTop, BypassCorner);
            Connect(BypassCorner, EchoRoomIds.Core);
        }

        private void AddNode(string name, float x, float z)
        {
            positions.Add(name, new Vector3(x, 0f, z));
            adjacency.Add(name, new List<GraphEdge>());
        }

        private void Connect(string from, string to, string portalId = null)
        {
            float length = Vector3.Distance(positions[from], positions[to]);
            adjacency[from].Add(new GraphEdge(to, length, portalId));
            adjacency[to].Add(new GraphEdge(from, length, portalId));
        }

        public Vector3 Position(string nodeId) => positions[nodeId];
        public IEnumerable<GraphEdge> Neighbors(string nodeId) => adjacency[nodeId];
        public IEnumerable<string> Nodes => positions.Keys;
    }
}
