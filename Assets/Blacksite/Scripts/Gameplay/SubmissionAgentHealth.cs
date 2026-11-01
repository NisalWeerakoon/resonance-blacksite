using UnityEngine;
using Blacksite.Agents;
using Blacksite.Agents.Movement;
using Blacksite.Agents.Rift;

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
        private RiftProceduralAnimation riggedAnimation;
        private Renderer[] visualRenderers;
        private MaterialPropertyBlock[] restPropertyBlocks;
        private MaterialPropertyBlock[] flashPropertyBlocks;
        private float hitReactionUntil;
        private float visualFlashUntil;
        private float hitReactionSide;
        private Vector3 hitReactionDirection;
        private bool hitFlashApplied;
        private bool isDead;
        private float deathStartedAt;
        private float deathSide = 1f;
        private Vector3 deathDirection;
        private const float HitReactionDuration = .38f;
        private const float HitFlashDuration = .11f;
        private const float DeathAnimationDuration = 1.15f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        public int HitsRemaining => Mathf.Max(0, hitsToDisable - hits);
        public int MaxHits => hitsToDisable;
        public bool IsDead => isDead;
        public float HealthFraction => hitsToDisable <= 0 ? 0f : HitsRemaining / (float)hitsToDisable;
        public bool ShouldRetreat => HitsRemaining > 0 && HitsRemaining <= Mathf.CeilToInt(hitsToDisable * .375f);

        private void Awake()
        {
            visualRenderers = GetComponentsInChildren<Renderer>(true);
            Renderer bodyRenderer = visualRenderers.Length > 0 ? visualRenderers[0] : null;
            if (bodyRenderer == null) return;
            visualRoot = bodyRenderer.transform;
            while (visualRoot.parent != null && visualRoot.parent != transform)
                visualRoot = visualRoot.parent;
            visualRestPosition = visualRoot.localPosition;
            visualRestRotation = visualRoot.localRotation;
            riggedAnimation = GetComponent<RiftProceduralAnimation>();
            restPropertyBlocks = new MaterialPropertyBlock[visualRenderers.Length];
            flashPropertyBlocks = new MaterialPropertyBlock[visualRenderers.Length];
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                restPropertyBlocks[i] = new MaterialPropertyBlock();
                flashPropertyBlocks[i] = new MaterialPropertyBlock();
                visualRenderers[i].GetPropertyBlock(restPropertyBlocks[i]);
                visualRenderers[i].GetPropertyBlock(flashPropertyBlocks[i]);
                flashPropertyBlocks[i].SetColor(BaseColorId, new Color(1.2f, 2.1f, 2.5f));
                flashPropertyBlocks[i].SetColor(LegacyColorId, new Color(1.2f, 2.1f, 2.5f));
            }
        }

        private void Update()
        {
            if (hitFlashApplied && Time.time >= visualFlashUntil)
                SetHitFlash(false);
            if (isDead)
            {
                UpdateDeathAnimation();
                return;
            }
            if (visualRoot == null) return;
            float elapsed = HitReactionDuration - (hitReactionUntil - Time.time);
            float progress = Mathf.Clamp01(elapsed / HitReactionDuration);
            float kick = hitReactionUntil > Time.time
                ? Mathf.Pow(1f - progress, 2f) * (1f + .2f * Mathf.Sin(progress * Mathf.PI * 3f)) : 0f;
            Vector3 offset = hitReactionDirection * (.11f * kick) + Vector3.up * (.025f * kick);
            visualRoot.localPosition = visualRestPosition + offset + Vector3.right * (hitReactionSide * .025f * kick);
        }

        public void TakeHit()
        {
            TakeHit(transform.position + Vector3.up, transform.forward);
        }

        public void TakeHit(Vector3 impactPoint, Vector3 shotDirection)
        {
            if (isDead) return;
            hits++;
            lastHitAt = Time.time;
            recoveryProgress = 0f;
            hitFlashUntil = Time.time + .5f;
            Vector3 localShotDirection = transform.InverseTransformDirection(shotDirection.normalized);
            Vector3 localImpactPoint = transform.InverseTransformPoint(impactPoint);
            hitReactionDirection = -new Vector3(localShotDirection.x, 0f, localShotDirection.z).normalized;
            if (hitReactionDirection.sqrMagnitude < .01f) hitReactionDirection = Vector3.back;
            hitReactionSide = Mathf.Abs(localShotDirection.x) > .1f
                ? Mathf.Sign(localShotDirection.x) : Mathf.Abs(localImpactPoint.x) > .08f
                    ? -Mathf.Sign(localImpactPoint.x) : (Random.value < .5f ? -1f : 1f);
            deathSide = hitReactionSide;
            deathDirection = hitReactionDirection;
            hitReactionUntil = Time.time + HitReactionDuration;
            if (riggedAnimation == null) riggedAnimation = GetComponent<RiftProceduralAnimation>();
            if (riggedAnimation != null) riggedAnimation.TriggerHitReaction(hitReactionSide);
            visualFlashUntil = Time.time + HitFlashDuration;
            SetHitFlash(true);
            Debug.Log($"BLACKSITE_HIT agent={name} remaining={HitsRemaining}");
            if (hits < hitsToDisable) return;
            Debug.Log($"BLACKSITE {name} disabled after {hits} pulse hits", this);
            BeginDeathAnimation();
        }

        private void BeginDeathAnimation()
        {
            isDead = true;
            deathStartedAt = Time.time;
            AgentBrainBase brain = GetComponent<AgentBrainBase>();
            if (brain != null) brain.enabled = false;
            WaypointAgentMotor motor = GetComponent<WaypointAgentMotor>();
            if (motor != null) motor.enabled = false;
            if (riggedAnimation != null) riggedAnimation.SetDeathPose(deathSide);
        }

        private void UpdateDeathAnimation()
        {
            float progress = Mathf.Clamp01((Time.time - deathStartedAt) / DeathAnimationDuration);
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.05f, .72f, progress));
            float settle = progress > .72f
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.72f, 1f, progress)) : 0f;
            if (visualRoot != null)
            {
                float roll = deathSide * 88f * fall;
                float twitch = Mathf.Sin(progress * Mathf.PI * 9f) * settle * 4f;
                visualRoot.localRotation = visualRestRotation * Quaternion.Euler(0f, twitch, roll);
                visualRoot.localPosition = visualRestPosition + deathDirection * (.08f * fall) +
                    Vector3.up * (Mathf.Sin(progress * Mathf.PI) * .04f);
            }
            if (progress >= 1f) gameObject.SetActive(false);
        }

        private void SetHitFlash(bool enabled)
        {
            if (visualRenderers == null || restPropertyBlocks == null || flashPropertyBlocks == null) return;
            for (int i = 0; i < visualRenderers.Length; i++)
                if (visualRenderers[i] != null)
                    visualRenderers[i].SetPropertyBlock(enabled ? flashPropertyBlocks[i] : restPropertyBlocks[i]);
            hitFlashApplied = enabled;
        }

        public void RecoverWhileHidden(bool isHidden, float deltaTime)
        {
            if (isDead || !isHidden || HitsRemaining >= hitsToDisable)
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
            if (isDead || !BlacksiteGameFlow.IsPlaying || Camera.main == null) return;
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
