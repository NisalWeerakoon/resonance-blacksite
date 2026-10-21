using UnityEngine;

namespace Blacksite.Agents.Echo
{
    public enum EchoAction { Patrol, InvestigateSound, SearchLastKnown, ChaseVisible }

    public readonly struct EchoUtilityInputs
    {
        public EchoUtilityInputs(float confidence, float recency, bool soundReachable,
            bool hasLastKnown, float lastKnownAge, bool hasFreshSighting, float sightConfidence)
        {
            Confidence = Mathf.Clamp01(confidence);
            Recency = Mathf.Clamp01(recency);
            SoundReachable = soundReachable;
            HasLastKnown = hasLastKnown;
            LastKnownAge = Mathf.Max(0f, lastKnownAge);
            HasFreshSighting = hasFreshSighting;
            SightConfidence = Mathf.Clamp01(sightConfidence);
        }
        public float Confidence { get; }
        public float Recency { get; }
        public bool SoundReachable { get; }
        public bool HasLastKnown { get; }
        public float LastKnownAge { get; }
        public bool HasFreshSighting { get; }
        public float SightConfidence { get; }
    }

    public readonly struct EchoUtilityDecision
    {
        public EchoUtilityDecision(EchoAction action, float patrol, float investigate,
            float search, float chase)
        {
            Action = action;
            Patrol = patrol;
            Investigate = investigate;
            Search = search;
            Chase = chase;
        }
        public EchoAction Action { get; }
        public float Patrol { get; }
        public float Investigate { get; }
        public float Search { get; }
        public float Chase { get; }
    }

    /// <summary>Normalized action scores and a small anti-oscillation retention bonus.</summary>
    public static class EchoUtilityEvaluator
    {
        public static EchoUtilityDecision Evaluate(EchoUtilityInputs input, EchoAction previous)
        {
            float patrol = 0.24f;
            // A quiet cue does not override patrol; useful for illustrating door attenuation.
            float investigate = input.SoundReachable && input.Confidence >= 0.24f
                ? 0.45f * input.Confidence + 0.30f * input.Recency + 0.25f : 0f;
            float search = input.HasLastKnown && input.LastKnownAge <= 12f
                ? 0.34f * (1f - input.LastKnownAge / 12f) : 0f;
            float chase = input.HasFreshSighting
                ? 0.80f + 0.20f * input.SightConfidence : 0f;

            float p = patrol + (previous == EchoAction.Patrol ? 0.06f : 0f);
            float i = investigate > 0f ? investigate + (previous == EchoAction.InvestigateSound ? 0.06f : 0f) : 0f;
            float s = search > 0f ? search + (previous == EchoAction.SearchLastKnown ? 0.06f : 0f) : 0f;
            float c = chase > 0f ? chase + (previous == EchoAction.ChaseVisible ? 0.06f : 0f) : 0f;
            EchoAction selected = EchoAction.Patrol;
            float maximum = p;
            if (i > maximum) { selected = EchoAction.InvestigateSound; maximum = i; }
            if (s > maximum) { selected = EchoAction.SearchLastKnown; maximum = s; }
            if (c > maximum) selected = EchoAction.ChaseVisible;
            return new EchoUtilityDecision(selected, patrol, investigate, search, chase);
        }
    }
}
