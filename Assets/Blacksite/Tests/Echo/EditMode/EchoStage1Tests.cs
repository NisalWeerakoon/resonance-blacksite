using System;
using System.Linq;
using Blacksite.Agents.Echo;
using NUnit.Framework;
using UnityEngine;

namespace Blacksite.Tests.Echo
{
    public sealed class EchoStage1Tests
    {
        [Test]
        public void DefaultGraph_HasSixRoomsAndSevenPortals()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            Assert.AreEqual(6, graph.RoomCount);
            Assert.AreEqual(7, graph.PortalCount);
        }

        [Test]
        public void DefaultGraph_D02OpenIsBidirectionalWithCostOne()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            Assert.IsTrue(graph.TryGetDirectCost(EchoRoomIds.Hub, EchoRoomIds.Acoustic, out float forward));
            Assert.IsTrue(graph.TryGetDirectCost(EchoRoomIds.Acoustic, EchoRoomIds.Hub, out float reverse));
            Assert.AreEqual(1f, forward);
            Assert.AreEqual(forward, reverse);
        }

        [Test]
        public void D02ClosedAndSealed_ChangeDirectSoundCost()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            Assert.IsTrue(graph.TrySetPortalState("D02", EchoDoorAcousticState.Closed));
            Assert.IsTrue(graph.TryGetDirectCost(EchoRoomIds.Hub, EchoRoomIds.Acoustic, out float closedCost));
            Assert.AreEqual(4f, closedCost);
            Assert.IsTrue(graph.TrySetPortalState("D02", EchoDoorAcousticState.Sealed));
            Assert.IsTrue(graph.TryGetDirectCost(EchoRoomIds.Hub, EchoRoomIds.Acoustic, out float sealedCost));
            Assert.AreEqual(6f, sealedCost);
        }

        [Test]
        public void RoomsWithoutPortal_HaveNoDirectSoundEdge()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            Assert.IsFalse(graph.TryGetDirectCost(EchoRoomIds.Entry, EchoRoomIds.Core, out float cost));
            Assert.IsTrue(float.IsPositiveInfinity(cost));
        }

        [Test]
        public void UnknownPortal_CannotBeUpdated()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            Assert.IsFalse(graph.TrySetPortalState("D99", EchoDoorAcousticState.Closed));
        }

        [Test]
        public void NoiseEvent_ClampsLoudnessAndPreservesSource()
        {
            var noise = new EchoNoiseEvent(7, EchoRoomIds.Acoustic, new Vector3(1, 2, 3),
                1.5f, EchoNoiseCategory.DecoyImpact, 12.0);
            Assert.AreEqual(1f, noise.Loudness);
            Assert.AreEqual(EchoRoomIds.Acoustic, noise.SourceRoomId);
            Assert.AreEqual(EchoNoiseCategory.DecoyImpact, noise.Category);
            Assert.AreEqual(7, noise.Id);
            Assert.AreEqual(12.0, noise.CapturedAtSeconds);
        }

        [Test]
        public void NoiseEvent_RejectsUnknownRoom()
        {
            Assert.Throws<ArgumentException>(() => new EchoNoiseEvent(
                1, "NOT_A_ROOM", Vector3.zero, 0.5f, EchoNoiseCategory.Footstep, 1.0));
        }

        [Test]
        public void NoiseEvent_NegativeAndNaNLoudnessBecomeZero()
        {
            var negative = new EchoNoiseEvent(1, EchoRoomIds.Entry, Vector3.zero,
                -10f, EchoNoiseCategory.Other, 0.0);
            var nan = new EchoNoiseEvent(2, EchoRoomIds.Entry, Vector3.zero,
                float.NaN, EchoNoiseCategory.Other, 0.0);
            Assert.AreEqual(0f, negative.Loudness);
            Assert.AreEqual(0f, nan.Loudness);
        }

        [Test]
        public void PortalConnections_IncludeTheAgreedSevenIDs()
        {
            EchoAcousticGraph graph = EchoAcousticGraph.CreateDefault();
            string[] ids = EchoRoomIds.All.SelectMany(graph.Neighbors)
                .Select(edge => edge.PortalId).Distinct().OrderBy(id => id).ToArray();
            CollectionAssert.AreEquivalent(new[] { "D01", "D02", "D03", "D04", "D05", "D06", "D07" }, ids);
        }
    }
}
