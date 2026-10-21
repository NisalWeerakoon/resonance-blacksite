using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class PlayerPulseWeapon : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(.1f)] private float range = 30f;
        [SerializeField, Min(.05f)] private float fireInterval = .35f;
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(.1f)] private float reloadSeconds = 1.6f;
        private float nextFire;
        private float reloadCompleteAt;
        private float flashUntil;
        private float hitConfirmUntil;
        private int ammo;
        private BlacksiteMission mission;

        public int Ammo => ammo;
        public bool Reloading => reloadCompleteAt > Time.time;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            ammo = magazineSize;
            mission = FindFirstObjectByType<BlacksiteMission>();
        }

        private void Start()
        {
            if (mission == null) mission = FindFirstObjectByType<BlacksiteMission>();
        }

        private void Update()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            if (reloadCompleteAt > 0f && Time.time >= reloadCompleteAt)
            {
                ammo = magazineSize;
                reloadCompleteAt = 0f;
            }
            if (BlacksiteInput.Pressed(KeyCode.R) && ammo < magazineSize && !Reloading &&
                (mission == null || !mission.Won && !mission.Lost))
                StartReload();
            if (mission != null && (mission.Won || mission.Lost)) return;
            if (!BlacksiteInput.FirePressed() || Time.time < nextFire || viewCamera == null) return;
            if (Reloading) return;
            if (ammo <= 0) { StartReload(); return; }
            ammo--;
            nextFire = Time.time + fireInterval;
            flashUntil = Time.time + .12f;
            WorldSignals.EmitNoise(viewCamera.transform.position, 1f);
            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                    out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore)) return;
            SubmissionAgentHealth health = hit.collider.GetComponentInParent<SubmissionAgentHealth>();
            if (health != null)
            {
                health.TakeHit();
                hitConfirmUntil = Time.time + .22f;
            }
        }

        private void StartReload() => reloadCompleteAt = Time.time + reloadSeconds;

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            float cx = Screen.width * .5f;
            float cy = Screen.height * .5f;
            GUI.color = Time.time < hitConfirmUntil ? Color.red :
                Time.time < flashUntil ? Color.yellow : Color.cyan;
            GUI.DrawTexture(new Rect(cx - 12f, cy - 1f, 8f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 4f, cy - 1f, 8f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 12f, 2f, 8f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy + 4f, 2f, 8f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (Time.time < hitConfirmUntil)
                GUI.Label(new Rect(cx - 24f, cy + 18f, 75f, 22f), "HIT");
            GUI.Box(new Rect(12f, Screen.height - 80f, 230f, 66f), "PULSE CARBINE");
            GUI.Label(new Rect(25f, Screen.height - 53f, 210f, 24f),
                Reloading ? "RELOADING..." : $"CHARGE {ammo}/{magazineSize}  •  R reload");
        }
    }
}
