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
        private Transform viewmodel;
        private Vector3 restingPosition;
        private float recoil;
        private float reloadStartedAt;

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
            if (viewCamera != null)
            {
                viewmodel = viewCamera.transform.Find("Pulse_Carbine_Viewmodel");
                if (viewmodel != null) restingPosition = viewmodel.localPosition;
            }
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
            recoil = .16f;
            WorldSignals.EmitNoise(viewCamera.transform.position, 1f);
            Vector3 muzzle = viewCamera.transform.TransformPoint(new Vector3(.37f, -.28f, 1.33f));
            if (PulseProjectile.FirePlayer(viewCamera, transform, muzzle, range))
                hitConfirmUntil = Time.time + .25f;
        }

        private void LateUpdate()
        {
            if (viewmodel == null) return;
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * .7f);
            float reloadDip = Reloading ?
                Mathf.Sin(Mathf.PI * (Time.time - reloadStartedAt) / reloadSeconds) * .34f : 0f;
            viewmodel.localPosition = Vector3.Lerp(viewmodel.localPosition,
                restingPosition + new Vector3(0f, -reloadDip, -recoil), Time.deltaTime * 16f);
            viewmodel.localRotation = Quaternion.Euler(Reloading ? reloadDip * 60f : recoil * 35f, 0f, 0f);
        }

        private void StartReload()
        {
            reloadStartedAt = Time.time;
            reloadCompleteAt = Time.time + reloadSeconds;
        }

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
