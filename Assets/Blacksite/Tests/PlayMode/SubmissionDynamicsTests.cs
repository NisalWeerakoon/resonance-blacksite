using System.Collections;
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
    public sealed class SubmissionDynamicsTests
    {
        [TearDown]
        public void ResetCapturedFrameRate() => Time.captureFramerate = 0;

        [UnityTest]
        public IEnumerator AgentsHaveSolidNavigationColliders()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            foreach (WaypointAgentMotor motor in Object.FindObjectsByType<WaypointAgentMotor>())
            {
                Assert.IsTrue(motor.GetComponent<CapsuleCollider>() != null ||
                    motor.GetComponent<CharacterController>() != null,
                    motor.name + " cannot avoid walls without a navigation collider");
            }
        }

        [UnityTest]
        public IEnumerator RiftMotorActuallyTraversesCorridor()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var rift = Object.FindFirstObjectByType<RiftBrain>();
            var motor = rift.GetComponent<WaypointAgentMotor>();
            rift.enabled = false;
            Vector3 start = rift.transform.position;
            Assert.IsTrue(motor.SetDestination(new Vector3(8f, 0f, -10f)));
            Time.captureFramerate = 60;
            for (int frame = 0; frame < 120; frame++) yield return null;
            Time.captureFramerate = 0;
            Assert.Greater(Vector3.Distance(start, rift.transform.position), .35f,
                "RIFT should physically move along a world corridor");
        }

        [UnityTest]
        public IEnumerator AgentDecisionsRespondToObservationsAndNoise()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            var echo = Object.FindFirstObjectByType<EchoBrain>();
            var rift = Object.FindFirstObjectByType<RiftBrain>();
            var trace = Object.FindFirstObjectByType<TraceBrain>();
            var bastion = Object.FindFirstObjectByType<BastionBrain>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(echo);
            Assert.IsNotNull(rift);
            Assert.IsNotNull(trace);
            Assert.IsNotNull(bastion);

            WorldSignals.EmitNoise(echo.transform.position + Vector3.forward * 2f, 1f);
            echo.TickDecision(1f);
            Assert.AreNotEqual("Initializing", echo.LastReason);

            var observation = new WorldObservation(player.transform.position, 1f, Time.timeAsDouble);
            rift.Observe(observation);
            rift.TickDecision(1f);
            Assert.IsNotNull(rift.CurrentAction, "RIFT should plan after sighting the player");

            trace.Observe(observation);
            trace.TickDecision(1f);
            Assert.IsNotNull(trace.SelectedNodeId, "TRACE should select an interception point");
        }

        [UnityTest]
        public IEnumerator DamageAndPulseDisableReachExpectedStates()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var mission = Object.FindFirstObjectByType<BlacksiteMission>();
            var enemy = Object.FindFirstObjectByType<BastionBrain>().GetComponent<SubmissionAgentHealth>();
            var player = GameObject.Find("DemoPlayer_FPS");
            var vitals = player.GetComponent<PlayerVitals>();
            Assert.IsNotNull(mission);
            Assert.IsNotNull(enemy);
            Assert.IsNotNull(player);
            Assert.IsNotNull(vitals);
            enemy.TakeHit();
            enemy.TakeHit();
            Assert.IsTrue(enemy.gameObject.activeInHierarchy);
            enemy.TakeHit();
            Assert.IsFalse(enemy.gameObject.activeInHierarchy);
            vitals.TakeDamage(vitals.MaxHealth, "test");
            yield return null;
            Assert.IsTrue(mission.Lost, "Depleting health should end the run");
            yield return null;
            Assert.IsFalse(BlacksiteGameFlow.IsPlaying, "Defeat should show the end state");
        }

        [UnityTest]
        public IEnumerator EachOfFourActiveAgentsCanDamageThePlayer()
        {
            Time.captureFramerate = 60;
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            var vitals = player.GetComponent<PlayerVitals>();
            var controller = player.GetComponent<CharacterController>();
            var agents = new[] {
                Object.FindFirstObjectByType<EchoBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<BastionBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<RiftBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<TraceBrain>().GetComponent<SubmissionAgentHealth>()
            };
            foreach (WaypointAgentMotor motor in Object.FindObjectsByType<WaypointAgentMotor>())
                motor.enabled = false;
            for (int i = 0; i < agents.Length; i++)
            {
                for (int j = 0; j < agents.Length; j++) agents[j].gameObject.SetActive(i == j);
                controller.enabled = false;
                player.transform.position = agents[i].transform.position + Vector3.right * 1.7f + Vector3.up;
                controller.enabled = true;
                Physics.SyncTransforms();
                int before = vitals.Health;
                for (int frame = 0; frame < 65; frame++) yield return null;
                Assert.Less(vitals.Health, before, agents[i].name + " should damage the player");
            }
            Time.captureFramerate = 0;
        }

        [UnityTest]
        public IEnumerator PlayerCanShootAndDisableEachRealAgentInTheWorld()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            var controller = player.GetComponent<CharacterController>();
            var camera = player.GetComponentInChildren<Camera>();
            var agents = new[] {
                Object.FindFirstObjectByType<EchoBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<BastionBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<RiftBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<TraceBrain>().GetComponent<SubmissionAgentHealth>()
            };
            foreach (WaypointAgentMotor motor in Object.FindObjectsByType<WaypointAgentMotor>())
                motor.enabled = false;
            Vector3[] offsets = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (SubmissionAgentHealth agent in agents)
            {
                bool visible = false;
                foreach (Vector3 offset in offsets)
                {
                    controller.enabled = false;
                    player.transform.position = agent.transform.position + offset * 3f + Vector3.up;
                    controller.enabled = true;
                    camera.transform.rotation = Quaternion.LookRotation(
                        agent.transform.position + Vector3.up * 1.1f - camera.transform.position);
                    Physics.SyncTransforms();
                    RaycastHit[] candidates = Physics.RaycastAll(camera.transform.position,
                        camera.transform.forward, 8f, ~0, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(candidates, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (RaycastHit hit in candidates)
                    {
                        if (hit.transform.IsChildOf(player.transform)) continue;
                        visible = hit.collider.GetComponentInParent<SubmissionAgentHealth>() == agent;
                        break;
                    }
                    if (visible) break;
                }
                Assert.IsTrue(visible, agent.name + " must have a clear shootable approach");
                for (int hit = 0; hit < 3; hit++)
                    Assert.IsTrue(PulseProjectile.FirePlayer(camera, player.transform,
                        camera.transform.position, 8f), agent.name + " should register shot " + hit);
                Assert.IsFalse(agent.gameObject.activeInHierarchy,
                    agent.name + " must be disabled by three player shots");
            }
        }

        [UnityTest]
        public IEnumerator EachAgentShootsPlayerFromBeyondContactDistance()
        {
            Time.captureFramerate = 60;
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            var vitals = player.GetComponent<PlayerVitals>();
            var controller = player.GetComponent<CharacterController>();
            var agents = new[] {
                Object.FindFirstObjectByType<EchoBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<BastionBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<RiftBrain>().GetComponent<SubmissionAgentHealth>(),
                Object.FindFirstObjectByType<TraceBrain>().GetComponent<SubmissionAgentHealth>()
            };
            foreach (WaypointAgentMotor motor in Object.FindObjectsByType<WaypointAgentMotor>())
                motor.enabled = false;
            foreach (SubmissionAgentHealth agent in agents)
            {
                foreach (SubmissionAgentHealth other in agents)
                    other.gameObject.SetActive(other == agent);
                controller.enabled = false;
                player.transform.position = agent.transform.position + Vector3.right * 4.5f + Vector3.up;
                controller.enabled = true;
                Physics.SyncTransforms();
                int before = vitals.Health;
                for (int frame = 0; frame < 125; frame++) yield return null;
                Assert.Less(vitals.Health, before,
                    agent.name + " should land a visible ranged shot at 4.5m");
            }
        }
    }
}
