using System.Collections;
using System.Collections.Generic;
using Blacksite.Agents.Bastion;
using Blacksite.Agents.Echo;
using Blacksite.Agents.Movement;
using Blacksite.Agents.Rift;
using Blacksite.Agents.Trace;
using Blacksite.Gameplay;
using Blacksite.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Blacksite.Tests
{
    public sealed class SubmissionSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneLoadsWithFourBrainsAndViableRoute()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<EchoBrain>().Length);
            Assert.AreEqual(1, Object.FindObjectsByType<BastionBrain>().Length);
            Assert.AreEqual(1, Object.FindObjectsByType<RiftBrain>().Length);
            Assert.AreEqual(1, Object.FindObjectsByType<TraceBrain>().Length);
            Assert.AreEqual(4, Object.FindObjectsByType<WaypointAgentMotor>().Length);
            var graph = Object.FindFirstObjectByType<TraceWaypointGraph>();
            Assert.IsNotNull(graph);
            var route = new List<string>();
            Assert.IsTrue(GraphAStar.TryFind(graph, "ENTRY", "CORE", new HashSet<string>(), route));
            Assert.GreaterOrEqual(route.Count, 3);
            Assert.AreEqual(2, Object.FindObjectsByType<DoorPortal>().Length);
            Assert.AreEqual(1, Object.FindObjectsByType<MovableBarricade>().Length);
            int missing = 0;
            foreach (Transform item in Object.FindObjectsByType<Transform>())
                foreach (MonoBehaviour component in item.GetComponents<MonoBehaviour>())
                    if (component == null) { missing++; Debug.LogWarning("MISSING_COMPONENT " + item.name); }
            Assert.AreEqual(0, missing, "Submission scene contains missing runtime scripts");
        }

        [UnityTest]
        public IEnumerator CoreCanBeCollectedAndExtracted()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var mission = Object.FindFirstObjectByType<BlacksiteMission>();
            Assert.IsNotNull(mission);
            var player = GameObject.Find("DemoPlayer_FPS");
            var core = GameObject.Find("CorePickup_Visual_Only");
            Assert.IsNotNull(player);
            Assert.IsNotNull(core);
            player.transform.position = core.transform.position;
            Physics.SyncTransforms();
            Assert.IsTrue(mission.TryCollectCore());
            Assert.IsTrue(mission.HasCore);
            player.transform.position = new Vector3(-29f, 1f, 0f);
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(mission.Won);
            Assert.IsFalse(mission.Lost);
        }
    }
}
