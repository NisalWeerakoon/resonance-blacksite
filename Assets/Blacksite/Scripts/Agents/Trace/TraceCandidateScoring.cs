using System;
using UnityEngine;

namespace Blacksite.Agents.Trace
{
    public readonly struct TraceCandidate
    {
        public TraceCandidate(string id, Vector3 position, float chokeImportance,
            float historicalUse, float droneTravelCost, float estimatedPlayerTravelCost)
        {
            Id = id;
            Position = position;
            ChokeImportance = Mathf.Clamp01(chokeImportance);
            HistoricalUse = Mathf.Clamp01(historicalUse);
            DroneTravelCost = droneTravelCost;
            EstimatedPlayerTravelCost = estimatedPlayerTravelCost;
        }
        public string Id { get; }
        public Vector3 Position { get; }
        public float ChokeImportance { get; }
        public float HistoricalUse { get; }
        public float DroneTravelCost { get; }
        public float EstimatedPlayerTravelCost { get; }
    }

    public readonly struct TraceCandidateScore
    {
        public TraceCandidateScore(string id, float alignment, float choke, float history,
            float travelPenalty, float total)
        {
            Id = id;
            Alignment = alignment;
            Choke = choke;
            History = history;
            TravelPenalty = travelPenalty;
            Total = total;
        }
        public string Id { get; }
        public float Alignment { get; }
        public float Choke { get; }
        public float History { get; }
        public float TravelPenalty { get; }
        public float Total { get; }
    }

    public static class TraceCandidateScoring
    {
        public static bool TryScore(TraceCandidate candidate, Vector3 forecast, float range,
            out TraceCandidateScore score)
        {
            score = default;
            if (string.IsNullOrEmpty(candidate.Id) || candidate.DroneTravelCost < 0f ||
                float.IsInfinity(candidate.DroneTravelCost) ||
                float.IsNaN(candidate.DroneTravelCost)) return false;
            float distance = Vector3.Distance(candidate.Position, forecast);
            float alignment = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, range));
            float normalizedTravel = Mathf.Clamp01(candidate.DroneTravelCost /
                Mathf.Max(0.1f, range));
            float total = 0.35f * alignment + 0.30f * candidate.ChokeImportance +
                          0.20f * candidate.HistoricalUse - 0.15f * normalizedTravel;
            // Arriving too late makes a predicted interception invalid, however high its score.
            if (candidate.DroneTravelCost > candidate.EstimatedPlayerTravelCost + 2f)
                return false;
            score = new TraceCandidateScore(candidate.Id, alignment,
                candidate.ChokeImportance, candidate.HistoricalUse, normalizedTravel, total);
            return true;
        }
    }
}
