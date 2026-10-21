using Blacksite.Agents.Bastion;
using NUnit.Framework;

namespace Blacksite.Tests
{
    public sealed class BastionDecisionTests
    {
        [Test]
        public void SequencePropagatesRunningBeforeLaterAction()
        {
            bool laterRan = false;
            var node = new SequenceNode(
                new ConditionNode(() => true),
                new ActionNode(() => NodeStatus.Running),
                new ActionNode(() => { laterRan = true; return NodeStatus.Success; }));
            Assert.AreEqual(NodeStatus.Running, node.Tick());
            Assert.IsFalse(laterRan);
        }

        [Test]
        public void SelectorFallsBackWhenFirstBranchFails()
        {
            var node = new SelectorNode(new ConditionNode(() => false),
                new ActionNode(() => NodeStatus.Success));
            Assert.AreEqual(NodeStatus.Success, node.Tick());
        }

        [Test]
        public void CoverScoreMatchesBriefAndRejectsUnavailableCandidate()
        {
            var a = new CoverFactors(.4f, .9f, .6f, .7f, .2f);
            var b = new CoverFactors(.9f, .7f, .8f, .8f, .3f);
            Assert.AreEqual(.54f, CoverScoring.Score(a, CoverScoring.Default), .001f);
            Assert.AreEqual(.70f, CoverScoring.Score(b, CoverScoring.Default), .001f);
            Assert.AreEqual(1, CoverScoring.BestIndex(new[] { a, b },
                new[] { true, true }, CoverScoring.Default));
            Assert.AreEqual(0, CoverScoring.BestIndex(new[] { a, b },
                new[] { true, false }, CoverScoring.Default));
            Assert.AreEqual(-1, CoverScoring.BestIndex(new[] { a, b },
                new[] { false, false }, CoverScoring.Default));
        }
    }
}
