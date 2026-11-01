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
        private float sideRecoil;
        private float reloadStartedAt;
        private Transform muzzleFlash;
        private Light muzzleLight;
        private Renderer[] muzzleRenderers;
        private Material[] muzzleMaterials;
        private float muzzleFlashRemaining;
        private const float MuzzleFlashDuration = .095f;

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
                CreateMuzzleFlash();
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
            recoil = 1f;
            sideRecoil = Random.Range(-1.6f, 1.6f);
            muzzleFlashRemaining = MuzzleFlashDuration;
            if (muzzleLight != null) muzzleLight.enabled = true;
            if (muzzleRenderers != null)
                foreach (Renderer flashRenderer in muzzleRenderers) flashRenderer.enabled = true;
            WorldSignals.EmitNoise(viewCamera.transform.position, 1f);
            Vector3 muzzle = viewCamera.transform.TransformPoint(new Vector3(.37f, -.28f, 1.33f));
            if (PulseProjectile.FirePlayer(viewCamera, transform, muzzle, range))
                hitConfirmUntil = Time.time + .25f;
        }

        private void LateUpdate()
        {
            UpdateMuzzleFlash();
            if (viewmodel == null) return;
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 5.5f);
            sideRecoil = Mathf.MoveTowards(sideRecoil, 0f, Time.deltaTime * 8f);
            float reloadDip = Reloading ?
                Mathf.Sin(Mathf.PI * (Time.time - reloadStartedAt) / reloadSeconds) * .34f : 0f;
            viewmodel.localPosition = Vector3.Lerp(viewmodel.localPosition,
                restingPosition + new Vector3(sideRecoil * .008f, -reloadDip - recoil * .025f,
                    -recoil * .105f), Time.deltaTime * 22f);
            viewmodel.localRotation = Quaternion.Euler(Reloading ? reloadDip * 60f : recoil * 8f,
                sideRecoil, -sideRecoil * .55f);
        }

        private void CreateMuzzleFlash()
        {
            var flashObject = new GameObject("Pulse_Muzzle_Flash");
            muzzleFlash = flashObject.transform;
            muzzleFlash.SetParent(viewCamera.transform, false);
            muzzleFlash.localPosition = new Vector3(.37f, -.28f, 1.33f);
            muzzleFlash.localRotation = Quaternion.identity;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return;
            Material cyan = CreateFlashMaterial(shader, new Color(.08f, .8f, 1f));
            Material white = CreateFlashMaterial(shader, new Color(.75f, 1f, 1f));
            var renderers = new System.Collections.Generic.List<Renderer>();
            var materials = new System.Collections.Generic.List<Material> { cyan, white };
            AddFlashOrb("OuterPulse", Vector3.zero, new Vector3(.105f, .105f, .29f), cyan, renderers);
            AddFlashOrb("BrightCore", new Vector3(0f, 0f, .025f),
                new Vector3(.055f, .055f, .15f), white, renderers);
            AddFlashOrb("UpperArc", new Vector3(0f, .075f, 0f),
                new Vector3(.025f, .12f, .07f), cyan, renderers);
            AddFlashOrb("LowerArc", new Vector3(0f, -.075f, 0f),
                new Vector3(.025f, .12f, .07f), cyan, renderers);
            AddFlashOrb("SideArc", new Vector3(.07f, 0f, 0f),
                new Vector3(.12f, .025f, .07f), cyan, renderers);
            muzzleRenderers = renderers.ToArray();
            muzzleMaterials = materials.ToArray();

            muzzleLight = flashObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(.12f, .72f, 1f);
            muzzleLight.range = 4.5f;
            muzzleLight.intensity = 5f;
            muzzleLight.enabled = false;
        }

        private static Material CreateFlashMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 3f);
            }
            return material;
        }

        private void AddFlashOrb(string label, Vector3 position, Vector3 scale, Material material,
            System.Collections.Generic.List<Renderer> renderers)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = label;
            orb.transform.SetParent(muzzleFlash, false);
            orb.transform.localPosition = position;
            orb.transform.localScale = scale;
            Collider orbCollider = orb.GetComponent<Collider>();
            if (orbCollider != null) Destroy(orbCollider);
            Renderer orbRenderer = orb.GetComponent<Renderer>();
            orbRenderer.sharedMaterial = material;
            orbRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            orbRenderer.receiveShadows = false;
            orbRenderer.enabled = false;
            renderers.Add(orbRenderer);
        }

        private void UpdateMuzzleFlash()
        {
            if (muzzleFlashRemaining <= 0f) return;
            muzzleFlashRemaining = Mathf.Max(0f, muzzleFlashRemaining - Time.deltaTime);
            float progress = 1f - muzzleFlashRemaining / MuzzleFlashDuration;
            float pulse = Mathf.Sin(progress * Mathf.PI);
            muzzleFlash.localScale = Vector3.one * (.72f + pulse * .65f);
            if (muzzleLight != null)
            {
                muzzleLight.intensity = pulse * 5f;
                muzzleLight.enabled = muzzleFlashRemaining > 0f;
            }
            if (muzzleFlashRemaining <= 0f && muzzleRenderers != null)
                foreach (Renderer flashRenderer in muzzleRenderers) flashRenderer.enabled = false;
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

        private void OnDestroy()
        {
            if (muzzleMaterials == null) return;
            foreach (Material material in muzzleMaterials)
                if (material != null) Destroy(material);
        }
    }
}
