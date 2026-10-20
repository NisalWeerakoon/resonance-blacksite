using System;
using UnityEngine;

namespace Blacksite.Shared
{
    public readonly struct NoiseSignal
    {
        public NoiseSignal(Vector3 position, float loudness, double time)
        {
            Position = position;
            Loudness = Mathf.Max(0f, loudness);
            Time = time;
        }

        public Vector3 Position { get; }
        public float Loudness { get; }
        public double Time { get; }
    }

    public static class WorldSignals
    {
        public static event Action<NoiseSignal> NoiseEmitted;
        public static event Action<string, bool> DoorStateChanged;
        public static event Action<Bounds> BarricadeMoved;

        public static void EmitNoise(Vector3 position, float loudness)
        {
            NoiseEmitted?.Invoke(new NoiseSignal(position, loudness, Time.timeAsDouble));
        }

        public static void NotifyBarricadeMoved(Bounds bounds) => BarricadeMoved?.Invoke(bounds);
        public static void NotifyDoorStateChanged(string portalId, bool isOpen) =>
            DoorStateChanged?.Invoke(portalId, isOpen);
    }
}
