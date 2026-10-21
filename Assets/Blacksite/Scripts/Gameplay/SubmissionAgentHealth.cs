using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SubmissionAgentHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int hitsToDisable = 3;
        private int hits;
        private float hitFlashUntil;
        public int HitsRemaining => Mathf.Max(0, hitsToDisable - hits);

        public void TakeHit()
        {
            hits++;
            hitFlashUntil = Time.time + .5f;
            Debug.Log($"BLACKSITE_HIT agent={name} remaining={HitsRemaining}");
            if (hits < hitsToDisable) return;
            Debug.Log($"BLACKSITE {name} disabled after {hits} pulse hits", this);
            gameObject.SetActive(false);
        }

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying || Camera.main == null) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.6f);
            if (screen.z <= 0f) return;
            float x = screen.x - 37f;
            float y = Screen.height - screen.y;
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(x, y, 74f * HitsRemaining / 3f, 4f), Texture2D.whiteTexture);
            if (Time.time < hitFlashUntil)
                GUI.Label(new Rect(x, y - 27f, 90f, 24f), "HIT -1");
            GUI.color = Color.white;
        }
    }
}
