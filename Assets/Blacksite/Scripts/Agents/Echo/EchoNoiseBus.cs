using System;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>Publish discrete observed sound events; never publishes the player's live Transform.</summary>
    public static class EchoNoiseBus
    {
        private static int nextId;
        public static event Action<EchoNoiseEvent> NoiseEmitted;

        public static EchoNoiseEvent Emit(string room, Vector3 position, float loudness,
            EchoNoiseCategory category, double timeSeconds)
        {
            var sound = new EchoNoiseEvent(++nextId, room, position, loudness, category, timeSeconds);
            NoiseEmitted?.Invoke(sound);
            return sound;
        }

        public static EchoNoiseEvent EmitNow(string room, Vector3 position, float loudness,
            EchoNoiseCategory category)
            => Emit(room, position, loudness, category, Time.timeAsDouble);
    }
}
