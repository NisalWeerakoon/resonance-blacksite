using System.Collections;
using System;
using Blacksite.Gameplay;
using Blacksite.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Blacksite.Tests
{
    public sealed class SubmissionPlayerInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private Action<NoiseSignal> noiseHandler;

        [TearDown]
        public void RemoveVirtualDevices()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (noiseHandler != null) WorldSignals.NoiseEmitted -= noiseHandler;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.captureFramerate = 0;
        }

        [UnityTest]
        public IEnumerator WasdMouseAndDecoyControlsWorkInSavedScene()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            Assert.IsNotNull(player);
            Vector3 start = player.transform.position;
            Vector3 forward = player.transform.forward;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(.7f);
            Assert.Greater(Vector3.Dot(player.transform.position - start, forward), .25f,
                "W should move the player forward through the actual controller");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;

            int pulseCount = 0;
            noiseHandler = signal => { if (signal.Loudness >= .95f) pulseCount++; };
            WorldSignals.NoiseEmitted += noiseHandler;
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            Assert.IsTrue(mouse.leftButton.isPressed);
            Assert.AreEqual(1, pulseCount, "Left click should fire one pulse and emit noise");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
            if (!Application.isBatchMode)
            {
                Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
                Quaternion rotation = player.transform.rotation;
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100f, 0f) });
                yield return null;
                Assert.Greater(Quaternion.Angle(rotation, player.transform.rotation), 1f,
                    "Mouse delta should turn the player's camera/body");
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<ThrowableDecoy>().Length,
                "G should create a decoy in the saved scene");
        }

        [UnityTest]
        public IEnumerator CoreExtractionAndRestartWorkThroughKeyboard()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            var mission = Object.FindFirstObjectByType<BlacksiteMission>();
            var core = GameObject.Find("CorePickup_Visual_Only");
            Assert.IsNotNull(player);
            Assert.IsNotNull(mission);
            Assert.IsNotNull(core);
            var controller = player.GetComponent<CharacterController>();
            foreach (SubmissionRelay relay in Object.FindObjectsByType<SubmissionRelay>()) relay.Activate();
            controller.enabled = false;
            player.transform.position = core.transform.position;
            controller.enabled = true;
            Physics.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            Assert.IsTrue(mission.HasCore, "E should collect the core");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            controller.enabled = false;
            player.transform.position = new Vector3(-29f, 1f, 0f);
            controller.enabled = true;
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(mission.Won, "Returning to ENTRY with the core should win");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            yield return null;
            yield return null;
            var restarted = Object.FindFirstObjectByType<BlacksiteMission>();
            Assert.IsNotNull(restarted);
            Assert.IsFalse(restarted.HasCore);
            Assert.IsFalse(restarted.Won);
        }

        [UnityTest]
        public IEnumerator MousePulseDisablesTargetAfterThreeHits()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            Assert.IsNotNull(player);
            foreach (SubmissionAgentHealth enemy in Object.FindObjectsByType<SubmissionAgentHealth>())
                enemy.gameObject.SetActive(false);
            Camera view = player.GetComponentInChildren<Camera>();
            Assert.IsNotNull(view);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "PlayerPulseTestTarget";
            target.transform.position = view.transform.position + view.transform.forward * 4f;
            target.transform.localScale = Vector3.one;
            target.AddComponent<SubmissionAgentHealth>();
            Physics.SyncTransforms();
            for (int hit = 0; hit < 3; hit++)
            {
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                yield return null;
                if (hit < 2) Assert.IsTrue(target.activeSelf, "Target must survive fewer than three hits");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return new WaitForSeconds(.4f);
            }
            Assert.IsFalse(target.activeSelf, "Three actual left-click pulses should disable the target");
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator ThreeRelaysRequireInteractionBeforeCoreUnlocks()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var mission = Object.FindFirstObjectByType<BlacksiteMission>();
            var player = GameObject.Find("DemoPlayer_FPS");
            var core = GameObject.Find("CorePickup_Visual_Only");
            var controller = player.GetComponent<CharacterController>();
            Assert.AreEqual(3, mission.RelayCount);
            Assert.AreEqual(0, mission.RelaysActive);
            controller.enabled = false;
            player.transform.position = core.transform.position;
            controller.enabled = true;
            Physics.SyncTransforms();
            Assert.IsFalse(mission.TryCollectCore(), "Core must remain locked before relays");
            foreach (SubmissionRelay relay in Object.FindObjectsByType<SubmissionRelay>())
            {
                controller.enabled = false;
                player.transform.position = relay.transform.position;
                controller.enabled = true;
                Physics.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
            }
            Assert.AreEqual(3, mission.RelaysActive, "E must activate all three room relays");
            controller.enabled = false;
            player.transform.position = core.transform.position;
            controller.enabled = true;
            Physics.SyncTransforms();
            Assert.IsTrue(mission.TryCollectCore());
        }

        [UnityTest]
        public IEnumerator PlayerCanWalkFromEntryToCoreAndBack()
        {
            // Headless test runners may render only a few frames per wall-clock second.
            // Fix simulated frame time so CharacterController movement is deterministic.
            Time.captureFramerate = 60;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.LoadScene("SubmissionPlayable");
            yield return null;
            var player = GameObject.Find("DemoPlayer_FPS");
            var mission = Object.FindFirstObjectByType<BlacksiteMission>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(mission);
            foreach (SubmissionAgentHealth enemy in Object.FindObjectsByType<SubmissionAgentHealth>())
                enemy.gameObject.SetActive(false);
            foreach (SubmissionRelay relay in Object.FindObjectsByType<SubmissionRelay>()) relay.Activate();

            Vector3[] outward = {
                new Vector3(-29f, 0f, 0f),
                new Vector3(-12f, 0f, 0f),
                new Vector3(-12f, 0f, -9f),
                new Vector3(-12f, 0f, -18f),
                // The core is mounted on a solid pedestal. Stand beside it to interact.
                new Vector3(6.5f, 0f, -18f)
            };
            foreach (Vector3 waypoint in outward)
                yield return WalkTo(player.transform, waypoint);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            Assert.IsTrue(mission.HasCore, "Player should be able to collect the core after walking there");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            for (int index = outward.Length - 2; index >= 0; index--)
                yield return WalkTo(player.transform, outward[index]);
            yield return null;
            Assert.IsTrue(mission.Won, "Player should be able to extract after walking back");
        }

        private IEnumerator WalkTo(Transform player, Vector3 target)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            float start = Time.time;
            while (Vector2.Distance(new Vector2(player.position.x, player.position.z),
                       new Vector2(target.x, target.z)) > .3f && Time.time - start < 12f)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.QueueStateEvent(mouse, new MouseState());
                Cursor.lockState = CursorLockMode.None;
                Vector3 direction = target - player.position;
                direction.y = 0f;
                player.rotation = Quaternion.LookRotation(direction);
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            float remaining = Vector2.Distance(new Vector2(player.position.x, player.position.z),
                new Vector2(target.x, target.z));
            Assert.LessOrEqual(remaining, .3f,
                $"Player could not walk to {target}; stopped at {player.position}");
        }
    }
}
