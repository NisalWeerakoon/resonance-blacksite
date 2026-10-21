using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SubmissionAgentHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int hitsToDisable = 3;
        private int hits;

        public void TakeHit()
        {
            hits++;
            if (hits < hitsToDisable) return;
            Debug.Log($"BLACKSITE {name} disabled after {hits} pulse hits", this);
            gameObject.SetActive(false);
        }
    }
}
