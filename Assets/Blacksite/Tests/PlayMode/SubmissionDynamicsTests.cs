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
        public IEnumerator CaptureAndPulseDisableReachExpectedStates()
        {
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var mission = Object.FindFirstObjectByType<BlacksiteMission>();
            var enemy = Object.FindFirstObjectByType<BastionBrain>().GetComponent<SubmissionAgentHealth>();
            var player = GameObject.Find("DemoPlayer_FPS");
            Assert.IsNotNull(mission);
            Assert.IsNotNull(enemy);
            Assert.IsNotNull(player);
            enemy.TakeHit();
            enemy.TakeHit();
            Assert.IsTrue(enemy.gameObject.activeInHierarchy);
            enemy.TakeHit();
            Assert.IsFalse(enemy.gameObject.activeInHierarchy);
            var rift = Object.FindFirstObjectByType<RiftBrain>();
            foreach (WaypointAgentMotor motor in Object.FindObjectsByType<WaypointAgentMotor>())
                motor.enabled = false;
            player.transform.position = rift.transform.position;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(1.4f);
            Assert.IsTrue(mission.Lost, "Sustained agent proximity should capture the player");
        }
    }
}
