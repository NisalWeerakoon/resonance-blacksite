using System;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    public enum EchoNoiseCategory
    {
        Footstep,
        Gunshot,
        DecoyImpact,
        DoorImpact,
        Other
    }

    /// <summary>
    /// A sound event with a known origin, NOT a live player tracking reference.
    /// CapturedAtSeconds should use Time.timeAsDouble consistently at runtime.
    /// </summary>
    public readonly struct EchoNoiseEvent
    {
        public EchoNoiseEvent(int id, string sourceRoomId, Vector3 sourcePosition,
            float loudness, EchoNoiseCategory category, double capturedAtSeconds)
        {
            if (!EchoRoomIds.IsValid(sourceRoomId))
                throw new ArgumentException("Unknown acoustic room ID", nameof(sourceRoomId));
            if (double.IsNaN(capturedAtSeconds) || double.IsInfinity(capturedAtSeconds))
                throw new ArgumentOutOfRangeException(nameof(capturedAtSeconds));

            Id = id;
            SourceRoomId = sourceRoomId;
            SourcePosition = sourcePosition;
            Loudness = float.IsNaN(loudness) ? 0f : Mathf.Clamp01(loudness);
            Category = category;
            CapturedAtSeconds = capturedAtSeconds;
        }

        public int Id { get; }
        public string SourceRoomId { get; }
        public Vector3 SourcePosition { get; }
        public float Loudness { get; }
        public EchoNoiseCategory Category { get; }
        public double CapturedAtSeconds { get; }
    }
}
