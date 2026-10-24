using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SubmissionAgentHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int hitsToDisable = 3;
        [SerializeField, Min(0f)] private float recoveryDelay = 4f;
        [SerializeField, Min(.1f)] private float secondsPerRecoveredHit = 2.5f;
        private int hits;
        private float hitFlashUntil;
        private float lastHitAt = -100f;
        private float recoveryProgress;
        public int HitsRemaining => Mathf.Max(0, hitsToDisable - hits);
        public float HealthFraction => hitsToDisable <= 0 ? 0f : HitsRemaining / (float)hitsToDisable;
        public bool ShouldRetreat => HitsRemaining > 0 && HitsRemaining < hitsToDisable;

        public void TakeHit()
        {
            hits++;
            lastHitAt = Time.time;
            recoveryProgress = 0f;
            hitFlashUntil = Time.time + .5f;
            Debug.Log($"BLACKSITE_HIT agent={name} remaining={HitsRemaining}");
            if (hits < hitsToDisable) return;
            Debug.Log($"BLACKSITE {name} disabled after {hits} pulse hits", this);
            gameObject.SetActive(false);
        }

        public void RecoverWhileHidden(bool isHidden, float deltaTime)
        {
            if (!isHidden || HitsRemaining >= hitsToDisable)
            {
                recoveryProgress = 0f;
                return;
            }
            if (Time.time - lastHitAt < recoveryDelay) return;

            recoveryProgress += Mathf.Max(0f, deltaTime);
            if (recoveryProgress < secondsPerRecoveredHit) return;
            recoveryProgress -= secondsPerRecoveredHit;
            hits = Mathf.Max(0, hits - 1);
            Debug.Log($"BLACKSITE_RECOVERY agent={name} remaining={HitsRemaining}");
        }

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying || Camera.main == null) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.6f);
            if (screen.z <= 0f) return;
            float x = screen.x - 37f;
            float y = Screen.height - screen.y;
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(x, y, 74f * HealthFraction, 4f), Texture2D.whiteTexture);
            if (Time.time < hitFlashUntil)
                GUI.Label(new Rect(x, y - 27f, 90f, 24f), "HIT -1");
            GUI.color = Color.white;
        }
    }
}
