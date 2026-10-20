using System.Collections;
using Blacksite.Gameplay;
using Blacksite.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blacksite.Tests
{
    public sealed class SystemsInteractionSmokeTests
    {
        [UnityTest]
        public IEnumerator DoorTogglesCollisionAndPublishesState()
        {
            var root = new GameObject("DoorTest");
            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform);
            var blocker = panel.AddComponent<BoxCollider>();
            var portal = root.AddComponent<DoorPortal>();
            portal.ConfigureSandbox("D_TEST", blocker, panel, false);
            string seenId = null;
            bool seenOpen = false;
            void OnDoor(string id, bool open) { seenId = id; seenOpen = open; }
            WorldSignals.DoorStateChanged += OnDoor;
            try
            {
                portal.Toggle();
                Assert.IsTrue(portal.IsTraversable);
                Assert.IsFalse(blocker.enabled);
                Assert.AreEqual("D_TEST", seenId);
                Assert.IsTrue(seenOpen);
                portal.Toggle();
                Assert.IsFalse(portal.IsTraversable);
                Assert.IsTrue(blocker.enabled);
                Assert.IsFalse(seenOpen);
            }
            finally
            {
                WorldSignals.DoorStateChanged -= OnDoor;
                Object.Destroy(root);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DecoyImpactEmitsOneNoiseSignal()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -.5f, 0f);
            floor.transform.localScale = new Vector3(10f, 1f, 10f);
            var decoy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            decoy.transform.position = new Vector3(0f, 4f, 0f);
            decoy.transform.localScale = Vector3.one * .3f;
            decoy.AddComponent<Rigidbody>();
            decoy.AddComponent<ThrowableDecoy>();
            int count = 0;
            void OnNoise(NoiseSignal signal) => count++;
            WorldSignals.NoiseEmitted += OnNoise;
            try
            {
                for (int i = 0; i < 180 && count == 0; i++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, count, "Falling decoy should emit one impact signal");
                for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, count, "Decoy should not emit again after bounce");
            }
            finally
            {
                WorldSignals.NoiseEmitted -= OnNoise;
                Object.Destroy(decoy);
                Object.Destroy(floor);
            }
        }

        [UnityTest]
        public IEnumerator BarricadeClearsChokeAfterMoving()
        {
            var choke = new GameObject("Choke");
            var volume = choke.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = Vector3.one * 2f;
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.position = Vector3.zero;
            var body = block.AddComponent<Rigidbody>();
            body.useGravity = false;
            var barricade = block.AddComponent<MovableBarricade>();
            barricade.ConfigureSandbox("D_TEST", volume);
            try
            {
                Assert.IsFalse(barricade.IsTraversable);
                block.transform.position = new Vector3(4f, 0f, 0f);
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(barricade.IsTraversable);
            }
            finally
            {
                Object.Destroy(block);
                Object.Destroy(choke);
            }
        }
    }
}
