using System.Collections.Generic;
using Blacksite.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Blacksite.Tests
{
    public sealed class GraphAStarTests
    {
        private sealed class TestGraph : IWaypointGraph
        {
            public Vector3 Position(string nodeId) => nodeId switch
            {
                "A" => Vector3.zero,
                "B" => Vector3.right,
                "C" => Vector3.forward,
                _ => Vector3.right + Vector3.forward
            };
            public IEnumerable<GraphEdge> Neighbors(string nodeId)
            {
                if (nodeId == "A") { yield return new GraphEdge("B", 1f, "D01"); yield return new GraphEdge("C", 2f); }
                if (nodeId == "B") yield return new GraphEdge("D", 1f);
                if (nodeId == "C") yield return new GraphEdge("D", 2f);
            }
        }

        [Test]
        public void ClosedPortalForcesAlternateRoute()
        {
            var path = new List<string>();
            Assert.IsTrue(GraphAStar.TryFind(new TestGraph(), "A", "D", new HashSet<string> { "D01" }, path));
            CollectionAssert.AreEqual(new[] { "A", "C", "D" }, path);
        }
    }
}
