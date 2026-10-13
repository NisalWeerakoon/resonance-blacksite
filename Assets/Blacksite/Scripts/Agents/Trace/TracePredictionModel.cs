using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Trace
{
    /// <summary>Forecasts from reported sightings only. This class never reads the player Transform.</summary>
    public sealed class TracePredictionModel
    {
        private readonly float maxObservedSpeed;
        private readonly float velocitySmoothing;
        private readonly float memorySeconds;
        private WorldObservation? previous;
        private WorldObservation? latest;
        private Vector3 velocity;

        public TracePredictionModel(float maxObservedSpeed = 12f, float velocitySmoothing = 0.7f,
            float memorySeconds = 6f)
        {
            this.maxObservedSpeed = Mathf.Max(0.1f, maxObservedSpeed);
            this.velocitySmoothing = Mathf.Clamp01(velocitySmoothing);
            this.memorySeconds = Mathf.Max(0.1f, memorySeconds);
        }

        public Vector3 Velocity => velocity;
        public WorldObservation? Latest => latest;

        public bool Add(WorldObservation observation)
        {
            if (latest.HasValue)
            {
                double elapsed = observation.Time - latest.Value.Time;
                if (elapsed <= 0d) return false;
                Vector3 measured = (observation.Position - latest.Value.Position) / (float)elapsed;
                if (measured.magnitude > maxObservedSpeed) return false;
                velocity = previous.HasValue
                    ? Vector3.Lerp(velocity, measured, velocitySmoothing)
                    : measured;
                previous = latest;
            }
            latest = observation;
            return true;
        }

        public bool TryForecast(double now, float horizonSeconds, out Vector3 position,
            out float confidence)
        {
            position = default;
            confidence = 0f;
            if (!latest.HasValue || now < latest.Value.Time) return false;
            float age = (float)(now - latest.Value.Time);
            if (age >= memorySeconds) return false;
            confidence = latest.Value.Confidence * (1f - age / memorySeconds);
            if (confidence <= 0.01f) return false;
            position = latest.Value.Position + velocity * (age + Mathf.Max(0f, horizonSeconds));
            return true;
        }
    }
}
