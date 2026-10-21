using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class BlacksiteMission : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameObject coreVisual;
        [SerializeField] private Transform extractionPoint;
        [SerializeField] private SubmissionAgentHealth[] enemies;
        [SerializeField] private PlayerVitals vitals;
        [SerializeField] private SubmissionRelay[] relays;
        [SerializeField, Min(.5f)] private float pickupRange = 2.2f;
        [SerializeField, Min(.5f)] private float extractionRange = 3f;
        [SerializeField, Min(.1f)] private float captureRange = 1.25f;
        private bool hasCore;
        private bool won;
        private bool lost;

        public bool HasCore => hasCore;
        public bool Won => won;
        public bool Lost => lost;
        public int RelaysActive
        {
            get
            {
                int count = 0;
                if (relays != null)
                    foreach (SubmissionRelay relay in relays)
                        if (relay != null && relay.Activated) count++;
                return count;
            }
        }
        public int RelayCount => relays == null ? 0 : relays.Length;

        public void Configure(Transform playerTransform, GameObject core,
            Transform extraction, SubmissionAgentHealth[] agents, SubmissionRelay[] requiredRelays = null)
        {
            player = playerTransform;
            coreVisual = core;
            extractionPoint = extraction;
            enemies = agents;
            vitals = player != null ? player.GetComponent<PlayerVitals>() : null;
            relays = requiredRelays;
        }

        private void Update()
        {
            if (!BlacksiteGameFlow.IsPlaying && !won && !lost) return;
            if (won || lost || player == null) return;
            if (vitals != null && vitals.IsDead)
            {
                lost = true;
                UnlockCursor();
                Debug.Log("BLACKSITE_MISSION operative down");
                return;
            }
            if (BlacksiteInput.Pressed(KeyCode.E) && relays != null)
                foreach (SubmissionRelay relay in relays)
                    if (relay != null && !relay.Activated &&
                        Vector3.Distance(player.position, relay.transform.position) <= 2.5f)
                        relay.Activate();

            if (!hasCore && coreVisual != null &&
                Vector3.Distance(player.position, coreVisual.transform.position) <= pickupRange &&
                BlacksiteInput.Pressed(KeyCode.E))
                TryCollectCore();
            if (hasCore && extractionPoint != null &&
                Vector3.Distance(player.position, extractionPoint.position) <= extractionRange)
            {
                won = true;
                UnlockCursor();
                Debug.Log("BLACKSITE_MISSION extraction complete");
                return;
            }

        }

        public bool TryCollectCore()
        {
            if (hasCore || RelaysActive < RelayCount || player == null || coreVisual == null ||
                Vector3.Distance(player.position, coreVisual.transform.position) > pickupRange)
                return false;
            hasCore = true;
            coreVisual.SetActive(false);
            Debug.Log("BLACKSITE_MISSION core collected");
            return true;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            GUI.Box(new Rect(12, 12, 570, 148), "RESONANCE // BLACKSITE — Recovery Operation");
            string objective = won ? "EXTRACTION COMPLETE — press R to play again" :
                lost ? "OPERATIVE DOWN — press R to retry" :
                hasCore ? "CORE SECURED — return to ENTRY for extraction" :
                RelaysActive < RelayCount ?
                    $"ACTIVATE RELAYS ({RelaysActive}/{RelayCount}) in ACOUSTIC, SECURITY, MAINT — press E" :
                    "FIND THE RESONANCE CORE in CORE VAULT; press E to collect";
            GUI.Label(new Rect(24, 40, 540, 25), objective);
            GUI.Label(new Rect(24, 66, 540, 22), "WASD move • mouse aim • left click pulse • R reload • E interact • G decoy");
            GUI.Label(new Rect(24, 91, 540, 22), "Four distinct threats: use decoys, cover and the carbine; evade or disable them.");
            if (vitals != null)
            {
                GUI.color = vitals.Health < 30 ? Color.red : Color.green;
                GUI.DrawTexture(new Rect(24, 124, 5.2f * vitals.Health, 14f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(24, 123, 530, 20), $"OPERATIVE HEALTH {vitals.Health}/{vitals.MaxHealth}");
                if (vitals.DamageFlash > 0f)
                {
                    GUI.color = new Color(1f, .1f, .1f, vitals.DamageFlash * .3f);
                    GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }
    }
}
