using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SubmissionAgentHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int hitsToDisable = 8;
        [SerializeField, Min(0f)] private float recoveryDelay = 2f;
        [SerializeField, Min(.1f)] private float secondsPerRecoveredHit = 1f;
        private int hits;
        private float hitFlashUntil;
        private float lastHitAt = -100f;
        private float recoveryProgress;
        private Transform visualRoot;
        private Vector3 visualRestPosition;
        private Quaternion visualRestRotation;
        private float hitReactionUntil;
        private float hitReactionSide;
        private Vector3 hitReactionDirection;
        private const float HitReactionDuration = .34f;
        public int HitsRemaining => Mathf.Max(0, hitsToDisable - hits);
        public int MaxHits => hitsToDisable;
        public float HealthFraction => hitsToDisable <= 0 ? 0f : HitsRemaining / (float)hitsToDisable;
        public bool ShouldRetreat => HitsRemaining > 0 && HitsRemaining <= Mathf.CeilToInt(hitsToDisable * .375f);

        private void Awake()
        {
            Renderer bodyRenderer = GetComponentInChildren<Renderer>(true);
            if (bodyRenderer == null) return;
            visualRoot = bodyRenderer.transform;
            while (visualRoot.parent != null && visualRoot.parent != transform)
                visualRoot = visualRoot.parent;
            visualRestPosition = visualRoot.localPosition;
            visualRestRotation = visualRoot.localRotation;
        }

        private void Update()
        {
            if (visualRoot == null) return;
            float progress = Mathf.Clamp01(1f - (hitReactionUntil - Time.time) / HitReactionDuration);
            float kick = hitReactionUntil > Time.time
                ? Mathf.Sin((1f - progress) * Mathf.PI * .5f) : 0f;
            Vector3 offset = hitReactionDirection * (.075f * kick) + Vector3.up * (.035f * kick);
            visualRoot.localPosition = visualRestPosition + offset;
            visualRoot.localRotation = visualRestRotation * Quaternion.Euler(
                -8f * kick, 0f, -hitReactionSide * 11f * kick);
        }

        public void TakeHit()
        {
            TakeHit(transform.position + Vector3.up, transform.forward);
        }

        public void TakeHit(Vector3 impactPoint, Vector3 shotDirection)
        {
            hits++;
            lastHitAt = Time.time;
            recoveryProgress = 0f;
            hitFlashUntil = Time.time + .5f;
            Vector3 localShotDirection = transform.InverseTransformDirection(shotDirection.normalized);
            hitReactionDirection = -new Vector3(localShotDirection.x, 0f, localShotDirection.z).normalized;
            if (hitReactionDirection.sqrMagnitude < .01f) hitReactionDirection = Vector3.back;
            hitReactionSide = Mathf.Abs(localShotDirection.x) > .1f
                ? Mathf.Sign(localShotDirection.x) : (impactPoint.x >= transform.position.x ? 1f : -1f);
            hitReactionUntil = Time.time + HitReactionDuration;
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
