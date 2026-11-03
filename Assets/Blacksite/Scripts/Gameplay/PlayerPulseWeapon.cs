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
        private Transform energyCell;
        private Vector3 energyCellRestPosition;
        private Vector3 energyCellRestScale;
        private Quaternion energyCellRestRotation;
        private Transform spareEnergyCell;
        private Transform reloadHand;
        private Renderer[] reloadHandRenderers;
        private Material reloadHandMaterial;
        private Transform chargingHandle;
        private Vector3 chargingHandleRestPosition;

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
                if (viewmodel != null) CacheEnergyCell();
                CreateMuzzleFlash();
                if (viewmodel != null) CreateReloadHand();
                if (viewmodel != null) CreateChargingHandle();
            }
        }

        private void Update()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            if (reloadCompleteAt > 0f && Time.time >= reloadCompleteAt)
            {
                ammo = magazineSize;
                reloadCompleteAt = 0f;
                FinishReloadAnimation();
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
            float reloadProgress = Reloading
                ? Mathf.Clamp01((Time.time - reloadStartedAt) / reloadSeconds) : 1f;
            float reloadAction = Reloading ? ReloadWeaponPose(reloadProgress) : 0f;
            viewmodel.localPosition = Vector3.Lerp(viewmodel.localPosition,
                restingPosition + new Vector3(sideRecoil * .008f + reloadAction * .025f,
                    -reloadAction * .055f - recoil * .025f, -reloadAction * .035f - recoil * .105f),
                Time.deltaTime * (Reloading ? 12f : 22f));
            viewmodel.localRotation = Quaternion.Euler(reloadAction * 7f + recoil * 8f,
                sideRecoil + reloadAction * 2f, -sideRecoil * .55f - reloadAction * 7f);
            UpdateReloadAnimation(reloadProgress);
        }

        private void CacheEnergyCell()
        {
            energyCell = viewmodel.Find("EnergyCell");
            if (energyCell == null) return;
            energyCellRestPosition = energyCell.localPosition;
            energyCellRestScale = energyCell.localScale;
            energyCellRestRotation = energyCell.localRotation;
            GameObject spare = Instantiate(energyCell.gameObject, energyCell.parent);
            spare.name = "EnergyCell_ReloadCopy";
            spareEnergyCell = spare.transform;
            spareEnergyCell.localPosition = energyCellRestPosition;
            spareEnergyCell.localScale = Vector3.zero;
            spareEnergyCell.localRotation = energyCellRestRotation;
            spareEnergyCell.gameObject.SetActive(false);
        }

        private static float ReloadWeaponPose(float progress)
        {
            float lower = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .22f, progress));
            float raise = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.72f, 1f, progress));
            return lower * raise;
        }

        private void AnimateEnergyCell(float progress)
        {
            if (energyCell == null || spareEnergyCell == null) return;
            Vector3 handCellPosition = energyCellRestPosition + new Vector3(-.18f, -.32f, .02f);
            float eject = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.17f, .31f, progress));
            float insert = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.51f, .70f, progress));

            if (progress < .17f)
            {
                energyCell.localPosition = energyCellRestPosition;
                energyCell.localScale = energyCellRestScale;
            }
            else if (progress < .31f)
            {
                energyCell.localPosition = Vector3.Lerp(energyCellRestPosition, handCellPosition, eject);
                energyCell.localScale = energyCellRestScale * Mathf.Lerp(1f, .18f, eject);
            }
            else
            {
                energyCell.localPosition = handCellPosition;
                energyCell.localScale = energyCellRestScale * .001f;
            }
            energyCell.localRotation = energyCellRestRotation * Quaternion.Euler(0f, 0f, eject * 18f);

            if (progress < .49f)
            {
                spareEnergyCell.gameObject.SetActive(false);
            }
            else
            {
                spareEnergyCell.gameObject.SetActive(true);
                spareEnergyCell.localPosition = Vector3.Lerp(handCellPosition, energyCellRestPosition, insert);
                spareEnergyCell.localScale = energyCellRestScale * Mathf.Lerp(.18f, 1f, insert);
                spareEnergyCell.localRotation = energyCellRestRotation * Quaternion.Euler(0f, 0f,
                    Mathf.Lerp(-18f, 0f, insert));
            }
        }

        private void UpdateReloadAnimation(float progress)
        {
            if (!Reloading)
            {
                if (reloadHandRenderers != null)
                    foreach (Renderer handRenderer in reloadHandRenderers) handRenderer.enabled = false;
                if (chargingHandle != null) chargingHandle.localPosition = chargingHandleRestPosition;
                return;
            }

            AnimateEnergyCell(progress);
            if (reloadHand != null)
            {
                reloadHand.gameObject.SetActive(true);
                Vector3 handPosition = EvaluateReloadHandPosition(progress);
                reloadHand.localPosition = handPosition;
                reloadHand.localRotation = Quaternion.Euler(0f, 0f, EvaluateReloadHandTilt(progress));
                if (reloadHandRenderers != null)
                    foreach (Renderer handRenderer in reloadHandRenderers) handRenderer.enabled = true;
            }

            if (chargingHandle != null)
            {
                float pull = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.77f, .82f, progress));
                float release = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.83f, .88f, progress));
                chargingHandle.localPosition = chargingHandleRestPosition + Vector3.back * (.065f * pull * release);
            }
        }

        private Vector3 EvaluateReloadHandPosition(float progress)
        {
            Vector3 rest = new Vector3(-.48f, -.78f, .82f);
            Vector3 grab = new Vector3(.20f, -.35f, .72f);
            Vector3 pull = new Vector3(.18f, -.60f, .70f);
            Vector3 rack = new Vector3(.42f, -.25f, .72f);
            if (progress < .16f) return Vector3.Lerp(rest, grab, SmoothSegment(progress, 0f, .16f));
            if (progress < .31f) return Vector3.Lerp(grab, pull, SmoothSegment(progress, .16f, .31f));
            if (progress < .46f) return Vector3.Lerp(pull, rest, SmoothSegment(progress, .31f, .46f));
            if (progress < .56f) return Vector3.Lerp(rest, grab, SmoothSegment(progress, .46f, .56f));
            if (progress < .74f) return grab;
            if (progress < .81f) return Vector3.Lerp(grab, rack, SmoothSegment(progress, .74f, .81f));
            if (progress < .88f) return Vector3.Lerp(rack, grab, SmoothSegment(progress, .81f, .88f));
            return Vector3.Lerp(grab, rest, SmoothSegment(progress, .88f, 1f));
        }

        private static float EvaluateReloadHandTilt(float progress)
        {
            if (progress < .31f) return Mathf.Lerp(-12f, 25f, SmoothSegment(progress, 0f, .31f));
            if (progress < .46f) return Mathf.Lerp(25f, -12f, SmoothSegment(progress, .31f, .46f));
            if (progress < .74f) return Mathf.Lerp(-12f, 8f, SmoothSegment(progress, .46f, .74f));
            if (progress < .88f) return Mathf.Lerp(8f, -20f, SmoothSegment(progress, .74f, .88f));
            return Mathf.Lerp(-20f, 0f, SmoothSegment(progress, .88f, 1f));
        }

        private static float SmoothSegment(float value, float start, float end)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
        }

        private void CreateReloadHand()
        {
            GameObject handObject = new GameObject("Reload_Hand");
            reloadHand = handObject.transform;
            reloadHand.SetParent(viewCamera.transform, false);
            reloadHand.gameObject.SetActive(false);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            reloadHandMaterial = new Material(shader);
            Color glove = new Color(.055f, .09f, .12f);
            if (reloadHandMaterial.HasProperty("_BaseColor")) reloadHandMaterial.SetColor("_BaseColor", glove);
            reloadHandMaterial.color = glove;
            var trackedMaterials = new System.Collections.Generic.List<Material>(muzzleMaterials ?? new Material[0])
                { reloadHandMaterial };
            muzzleMaterials = trackedMaterials.ToArray();
            var renderers = new System.Collections.Generic.List<Renderer>();
            AddHandPart("Forearm", PrimitiveType.Capsule, new Vector3(0f, -.17f, 0f),
                new Vector3(.095f, .25f, .09f), Quaternion.identity, renderers);
            AddHandPart("Palm", PrimitiveType.Cube, new Vector3(0f, .015f, .015f),
                new Vector3(.16f, .11f, .075f), Quaternion.identity, renderers);
            for (int finger = 0; finger < 4; finger++)
            {
                float x = (finger - 1.5f) * .038f;
                AddHandPart("Finger_" + finger, PrimitiveType.Capsule,
                    new Vector3(x, .105f, .02f), new Vector3(.032f, .085f, .035f),
                    Quaternion.Euler(0f, 0f, (finger - 1.5f) * -5f), renderers);
            }
            AddHandPart("Thumb", PrimitiveType.Capsule, new Vector3(.095f, .015f, .02f),
                new Vector3(.04f, .085f, .04f), Quaternion.Euler(0f, 0f, -40f), renderers);
            reloadHandRenderers = renderers.ToArray();
        }

        private void AddHandPart(string label, PrimitiveType shape, Vector3 position, Vector3 scale,
            Quaternion rotation, System.Collections.Generic.List<Renderer> renderers)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = label;
            part.transform.SetParent(reloadHand, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null) Destroy(partCollider);
            Renderer partRenderer = part.GetComponent<Renderer>();
            partRenderer.sharedMaterial = reloadHandMaterial;
            partRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            partRenderer.receiveShadows = false;
            partRenderer.enabled = false;
            renderers.Add(partRenderer);
        }

        private void CreateChargingHandle()
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Charging_Handle_Animated";
            chargingHandle = handle.transform;
            chargingHandle.SetParent(viewmodel, false);
            chargingHandleRestPosition = new Vector3(.49f, -.19f, .78f);
            chargingHandle.localPosition = chargingHandleRestPosition;
            chargingHandle.localScale = new Vector3(.055f, .035f, .09f);
            Collider handleCollider = handle.GetComponent<Collider>();
            if (handleCollider != null) Destroy(handleCollider);
            Renderer handleRenderer = handle.GetComponent<Renderer>();
            Material handleMaterial = new Material(handleRenderer.sharedMaterial);
            if (handleMaterial.HasProperty("_BaseColor"))
                handleMaterial.SetColor("_BaseColor", new Color(.16f, .24f, .29f));
            handleMaterial.color = new Color(.16f, .24f, .29f);
            handleRenderer.sharedMaterial = handleMaterial;
            handleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            handleRenderer.receiveShadows = false;
            if (muzzleMaterials == null) muzzleMaterials = new Material[0];
            var materials = new System.Collections.Generic.List<Material>(muzzleMaterials) { handleMaterial };
            muzzleMaterials = materials.ToArray();
        }

        private void FinishReloadAnimation()
        {
            if (energyCell == null || spareEnergyCell == null) return;
            Transform spentCell = energyCell;
            energyCell = spareEnergyCell;
            spareEnergyCell = spentCell;
            energyCell.localPosition = energyCellRestPosition;
            energyCell.localScale = energyCellRestScale;
            energyCell.localRotation = energyCellRestRotation;
            energyCell.gameObject.SetActive(true);
            spareEnergyCell.gameObject.SetActive(false);
            spareEnergyCell.localPosition = energyCellRestPosition;
            spareEnergyCell.localScale = Vector3.zero;
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
