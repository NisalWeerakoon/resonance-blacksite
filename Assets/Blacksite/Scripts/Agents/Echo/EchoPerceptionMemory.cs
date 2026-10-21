using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    public readonly struct EchoPerceivedNoise
    {
        public EchoPerceivedNoise(EchoNoiseEvent noise, float confidence, float ageSeconds,
            EchoAcousticResult propagation)
        {
            Noise = noise;
            Confidence = confidence;
            AgeSeconds = ageSeconds;
            Propagation = propagation;
        }
        public EchoNoiseEvent Noise { get; }
        public float Confidence { get; }
        public float AgeSeconds { get; }
        public EchoAcousticResult Propagation { get; }
    }

    /// <summary>Bounded auditory memory with dynamic graph-cost recalculation.</summary>
    public sealed class EchoPerceptionMemory
    {
        private readonly List<EchoNoiseEvent> sounds = new List<EchoNoiseEvent>();
        public const float AttenuationPerCost = 0.4f;
        public const float DecayPerSecond = 0.1f;
        public const float MemoryLifetimeSeconds = 25f;
        public int Count => sounds.Count;

        public void Remember(EchoNoiseEvent sound)
        {
            sounds.RemoveAll(s => s.Id == sound.Id);
            sounds.Add(sound);
            if (sounds.Count > 16) sounds.RemoveAt(0);
        }

        public static float Confidence(float loudness, float cost, float age)
        {
            if (float.IsNaN(loudness) || float.IsNaN(cost) || float.IsNaN(age) ||
                loudness <= 0f || cost < 0f || float.IsPositiveInfinity(cost)) return 0f;
            return Mathf.Clamp01(loudness * Mathf.Exp(-AttenuationPerCost * cost) *
                                  Mathf.Exp(-DecayPerSecond * Mathf.Max(0f, age)));
        }

        public bool TryBest(EchoAcousticGraph graph, string listenerRoom, double now,
            out EchoPerceivedNoise best)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            best = default;
            if (!EchoRoomIds.IsValid(listenerRoom)) return false;
            sounds.RemoveAll(s => now - s.CapturedAtSeconds > MemoryLifetimeSeconds);
            bool found = false;
            float bestConfidence = 0f;
            foreach (EchoNoiseEvent sound in sounds)
            {
                float age = Mathf.Max(0f, (float)(now - sound.CapturedAtSeconds));
                EchoAcousticResult route = EchoAcousticDijkstra.Find(graph, sound.SourceRoomId, listenerRoom);
                if (!route.Success) continue;
                float confidence = Confidence(sound.Loudness, route.Cost, age);
                if (confidence <= 0.001f || (found && confidence <= bestConfidence)) continue;
                best = new EchoPerceivedNoise(sound, confidence, age, route);
                bestConfidence = confidence;
                found = true;
            }
            return found;
        }
    }
}
