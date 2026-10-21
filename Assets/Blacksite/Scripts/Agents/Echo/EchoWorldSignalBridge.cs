using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    public sealed class EchoWorldSignalBridge : MonoBehaviour
    {
        [SerializeField] private EchoWorldContext world;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<EchoWorldContext>();
        }

        private void OnEnable()
        {
            WorldSignals.NoiseEmitted += OnNoise;
            WorldSignals.DoorStateChanged += OnDoor;
        }

        private void OnDisable()
        {
            WorldSignals.NoiseEmitted -= OnNoise;
            WorldSignals.DoorStateChanged -= OnDoor;
        }

        private void OnNoise(NoiseSignal signal)
        {
            if (world == null) return;
            EchoNoiseCategory category = signal.Loudness <= .3f ? EchoNoiseCategory.Footstep :
                signal.Loudness >= .95f ? EchoNoiseCategory.Gunshot : EchoNoiseCategory.DecoyImpact;
            EchoNoiseBus.EmitNow(world.ClosestRoom(signal.Position), signal.Position,
                signal.Loudness, category);
        }

        private void OnDoor(string id, bool open)
        {
            if (world == null) return;
            world.SetPortal(id, open ? EchoDoorAcousticState.Open : EchoDoorAcousticState.Closed);
        }
    }
}
