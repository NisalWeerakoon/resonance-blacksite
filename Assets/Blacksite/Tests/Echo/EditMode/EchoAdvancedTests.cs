using System;
using System.Collections.Generic;
using Blacksite.Agents.Echo;
using Blacksite.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Blacksite.Tests.Echo
{
    public sealed class EchoAdvancedTests
    {
        [Test]
        public void Dijkstra_OpenD02_UsesDirectDoor()
        {
            EchoAcousticResult path = EchoAcousticDijkstra.Find(EchoAcousticGraph.CreateDefault(),
                EchoRoomIds.Acoustic, EchoRoomIds.Hub);
            Assert.IsTrue(path.Success);
            Assert.AreEqual(1f, path.Cost, 0.0001f);
            CollectionAssert.AreEqual(new[] { EchoRoomIds.Acoustic, EchoRoomIds.Hub }, path.Rooms);
            Assert.Greater(path.NodesExpanded, 0);
        }

        [Test]
        public void Dijkstra_ClosedD02_UsesQuieterAlternateRoute()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            graph.TrySetPortalState("D02", EchoDoorAcousticState.Closed);
            EchoAcousticResult result = EchoAcousticDijkstra.Find(graph,
                EchoRoomIds.Acoustic, EchoRoomIds.Hub);
            Assert.IsTrue(result.Success);
            // Closed direct edge costs 4; alternate route D03+D04+D05 costs 3.
            Assert.AreEqual(3f, result.Cost, 0.0001f);
            CollectionAssert.AreEqual(new[] { "ACOUSTIC", "SECURITY", "MAINT", "HUB" }, result.Rooms);
        }

        [Test]
        public void Dijkstra_SealedD02_DoesNotMakeSoundDisappear()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            graph.TrySetPortalState("D02", EchoDoorAcousticState.Sealed);
            Assert.AreEqual(3f, EchoAcousticDijkstra.Find(graph, "HUB", "ACOUSTIC").Cost);
        }

        [Test]
        public void Dijkstra_RemovedBothAcousticDoors_IsUnreachable()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            Assert.IsTrue(graph.TryRemovePortal("D02"));
            Assert.IsTrue(graph.TryRemovePortal("D03"));
            EchoAcousticResult result = EchoAcousticDijkstra.Find(graph, "HUB", "ACOUSTIC");
            Assert.IsFalse(result.Success);
            Assert.IsTrue(float.IsPositiveInfinity(result.Cost));
            Assert.AreEqual(0, result.Rooms.Count);
        }

        [Test]
        public void Dijkstra_SourceEqualsListener_ZeroCost()
        {
            EchoAcousticResult result = EchoAcousticDijkstra.Find(EchoAcousticGraph.CreateDefault(), "CORE", "CORE");
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0f, result.Cost);
            CollectionAssert.AreEqual(new[] { "CORE" }, result.Rooms);
        }

        [Test]
        public void Confidence_LowerThroughMoreAttenuation()
        {
            float open = EchoPerceptionMemory.Confidence(1f, 1f, 0f);
            float closed = EchoPerceptionMemory.Confidence(1f, 3f, 0f);
            Assert.Greater(open, closed);
            Assert.AreEqual(0.6703f, open, 0.001f);
            Assert.AreEqual(0.3012f, closed, 0.001f);
        }

        [Test]
        public void Confidence_DecaysWithTime()
        {
            float early = EchoPerceptionMemory.Confidence(1f, 1f, 0f);
            float late = EchoPerceptionMemory.Confidence(1f, 1f, 10f);
            Assert.Greater(early, late);
        }

        [Test]
        public void Confidence_ZeroAndInvalidInputs_ClampSafely()
        {
            Assert.AreEqual(0f, EchoPerceptionMemory.Confidence(0f, 0f, 0f));
            Assert.AreEqual(0f, EchoPerceptionMemory.Confidence(-1f, 0f, 0f));
            Assert.AreEqual(0f, EchoPerceptionMemory.Confidence(float.NaN, 1f, 1f));
            Assert.AreEqual(0f, EchoPerceptionMemory.Confidence(1f, float.PositiveInfinity, 1f));
        }

        [Test]
        public void Memory_TwoSounds_ChoosesStrongerCredibleNoise()
        {
            var memory = new EchoPerceptionMemory();
            memory.Remember(new EchoNoiseEvent(1, "SECURITY", Vector3.zero,
                0.1f, EchoNoiseCategory.Footstep, 10d));
            memory.Remember(new EchoNoiseEvent(2, "HUB", Vector3.one,
                1f, EchoNoiseCategory.DecoyImpact, 11d));
            Assert.IsTrue(memory.TryBest(EchoAcousticGraph.CreateDefault(), "HUB", 11.5d,
                out EchoPerceivedNoise winner));
            Assert.AreEqual(2, winner.Noise.Id);
        }

        [Test]
        public void Memory_ExpiresOldSounds()
        {
            var memory = new EchoPerceptionMemory();
            memory.Remember(new EchoNoiseEvent(1, "HUB", Vector3.zero,
                1f, EchoNoiseCategory.Other, 0d));
            Assert.IsFalse(memory.TryBest(EchoAcousticGraph.CreateDefault(), "HUB", 30d,
                out EchoPerceivedNoise unused));
            Assert.AreEqual(0, memory.Count);
        }

        [Test]
        public void Memory_MissingAcousticPath_IgnoresNoise()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            graph.TryRemovePortal("D02"); graph.TryRemovePortal("D03");
            var memory = new EchoPerceptionMemory();
            memory.Remember(new EchoNoiseEvent(1, "ACOUSTIC", Vector3.zero,
                1f, EchoNoiseCategory.Gunshot, 0d));
            Assert.IsFalse(memory.TryBest(graph, "HUB", 0d, out EchoPerceivedNoise unused));
        }

        [Test]
        public void Utility_SilenceChoosesPatrol()
        {
            var input = new EchoUtilityInputs(0f, 0f, false, false, 100f, false, 0f);
            Assert.AreEqual(EchoAction.Patrol,
                EchoUtilityEvaluator.Evaluate(input, EchoAction.Patrol).Action);
        }

        [Test]
        public void Utility_LoudRecentSoundChoosesInvestigate()
        {
            var input = new EchoUtilityInputs(0.8f, 1f, true, false, 0f, false, 0f);
            Assert.AreEqual(EchoAction.InvestigateSound,
                EchoUtilityEvaluator.Evaluate(input, EchoAction.Patrol).Action);
        }

        [Test]
        public void Utility_WeakNoise_UsesPatrol()
        {
            var input = new EchoUtilityInputs(0.08f, 0.9f, true, false, 0f, false, 0f);
            Assert.AreEqual(EchoAction.Patrol,
                EchoUtilityEvaluator.Evaluate(input, EchoAction.Patrol).Action);
        }

        [Test]
        public void Utility_FadingClue_TriggersLastKnownSearch()
        {
            var input = new EchoUtilityInputs(0f, 0f, false, true, 1f, false, 0f);
            Assert.AreEqual(EchoAction.SearchLastKnown,
                EchoUtilityEvaluator.Evaluate(input, EchoAction.Patrol).Action);
        }

        [Test]
        public void Utility_FreshValidSighting_ChoosesChase()
        {
            var input = new EchoUtilityInputs(0.7f, 1f, true, true, 2f, true, 1f);
            Assert.AreEqual(EchoAction.ChaseVisible,
                EchoUtilityEvaluator.Evaluate(input, EchoAction.InvestigateSound).Action);
        }

        [Test]
        public void SpatialAStar_DoorClosed_UsesAlternateLoop()
        {
            var graph = new EchoSpatialWaypointGraph();
            var blocked = new HashSet<string> { "D02" };
            var nodes = new List<string>();
            bool result = GraphAStar.TryFind(graph, "ACOUSTIC", "HUB", blocked, nodes);
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new[] { "ACOUSTIC", "SECURITY", "MAINT", "HUB" }, nodes);
        }

        [Test]
        public void SpatialAStar_ClosedEveryHubExit_IsUnreachable()
        {
            var graph = new EchoSpatialWaypointGraph();
            var blocked = new HashSet<string> { "D01", "D02", "D05", "D07" };
            var nodes = new List<string>();
            Assert.IsFalse(GraphAStar.TryFind(graph, "HUB", "ACOUSTIC", blocked, nodes));
        }
    }
}
