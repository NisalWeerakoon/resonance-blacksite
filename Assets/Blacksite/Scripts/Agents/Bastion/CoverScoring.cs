using System;
using UnityEngine;

namespace Blacksite.Agents.Bastion
{
    public readonly struct CoverFactors
    {
        public CoverFactors(float protection, float distance, float firingAngle,
            float safety, float pathCost)
        {
            Protection = Mathf.Clamp01(protection);
            Distance = Mathf.Clamp01(distance);
            FiringAngle = Mathf.Clamp01(firingAngle);
            Safety = Mathf.Clamp01(safety);
            PathCost = Mathf.Clamp01(pathCost);
        }

        public float Protection { get; }
        public float Distance { get; }
        public float FiringAngle { get; }
        public float Safety { get; }
        public float PathCost { get; }
    }

    public readonly struct CoverWeights
    {
        public CoverWeights(float protection, float distance, float firingAngle,
            float safety, float pathCost)
        {
            Protection = protection;
            Distance = distance;
            FiringAngle = firingAngle;
            Safety = safety;
            PathCost = pathCost;
        }

        public float Protection { get; }
        public float Distance { get; }
        public float FiringAngle { get; }
        public float Safety { get; }
        public float PathCost { get; }
    }

    public static class CoverScoring
    {
        // The brief's illustrative coefficients, not claimed to be optimal.
        public static readonly CoverWeights Default = new CoverWeights(.30f, .20f, .20f, .20f, .10f);

        public static float Score(CoverFactors f, CoverWeights w)
        {
            return w.Protection * f.Protection + w.Distance * f.Distance +
                   w.FiringAngle * f.FiringAngle + w.Safety * f.Safety -
                   w.PathCost * f.PathCost;
        }

        public static int BestIndex(CoverFactors[] factors, bool[] valid, CoverWeights weights)
        {
            if (factors == null || valid == null || factors.Length != valid.Length)
                throw new ArgumentException("Candidate and validity arrays must match");
            int best = -1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < factors.Length; i++)
            {
                if (!valid[i]) continue;
                float score = Score(factors[i], weights);
                if (score > bestScore) { best = i; bestScore = score; }
            }
            return best;
        }
    }
}
