using Blacksite.Agents.Trace;
using Blacksite.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Blacksite.Tests
{
    public sealed class TraceDecisionTests
    {
        [Test]
        public void ReversalChangesForecastWithoutReadingHiddenPlayerPosition()
        {
            var model = new TracePredictionModel();
            Assert.IsTrue(model.Add(new WorldObservation(Vector3.zero, 1f, 1d)));
            Assert.IsTrue(model.Add(new WorldObservation(new Vector3(2f, 0f, 0f), 1f, 2d)));
            Assert.IsTrue(model.TryForecast(2d, 3f, out Vector3 forward, out _));
            Assert.Greater(forward.x, 2f);
            Assert.IsTrue(model.Add(new WorldObservation(Vector3.zero, 1f, 3d)));
            Assert.IsTrue(model.TryForecast(3d, 3f, out Vector3 reversed, out _));
            Assert.Less(reversed.x, 0f);
        }

        [Test]
        public void StaleAndImpossibleSightingsDoNotDriveInterception()
        {
            var model = new TracePredictionModel();
            Assert.IsTrue(model.Add(new WorldObservation(Vector3.zero, 1f, 1d)));
            Assert.IsFalse(model.Add(new WorldObservation(new Vector3(100f, 0f, 0f), 1f, 2d)));
            Assert.IsFalse(model.TryForecast(8d, 2f, out _, out _));
        }

        [Test]
        public void CandidateNeedsReachableRouteAndPlausibleArrival()
        {
            var reachable = new TraceCandidate("HUB", Vector3.zero, 1f, 0.5f, 4f, 5f);
            Assert.IsTrue(TraceCandidateScoring.TryScore(reachable, Vector3.zero, 25f,
                out TraceCandidateScore score));
            Assert.Greater(score.Total, 0f);

            var late = new TraceCandidate("CORE", Vector3.zero, 1f, 1f, 20f, 2f);
            Assert.IsFalse(TraceCandidateScoring.TryScore(late, Vector3.zero, 25f, out _));
            var unreachable = new TraceCandidate("BLOCKED", Vector3.zero, 1f, 1f,
                float.PositiveInfinity, 5f);
            Assert.IsFalse(TraceCandidateScoring.TryScore(unreachable, Vector3.zero, 25f,
                out _));
        }
    }
}
