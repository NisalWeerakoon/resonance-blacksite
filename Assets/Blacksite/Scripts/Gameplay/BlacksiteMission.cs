using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksite.Gameplay
{
    public sealed class BlacksiteMission : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameObject coreVisual;
        [SerializeField] private Transform extractionPoint;
        [SerializeField] private SubmissionAgentHealth[] enemies;
        [SerializeField, Min(.5f)] private float pickupRange = 2.2f;
        [SerializeField, Min(.5f)] private float extractionRange = 3f;
        [SerializeField, Min(.1f)] private float captureRange = 1.25f;
        private float captureTime;
        private bool hasCore;
        private bool won;
        private bool lost;

        public bool HasCore => hasCore;
        public bool Won => won;
        public bool Lost => lost;

        public void Configure(Transform playerTransform, GameObject core,
            Transform extraction, SubmissionAgentHealth[] agents)
        {
            player = playerTransform;
            coreVisual = core;
            extractionPoint = extraction;
            enemies = agents;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R) && (won || lost))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (won || lost || player == null) return;

            if (!hasCore && coreVisual != null &&
                Vector3.Distance(player.position, coreVisual.transform.position) <= pickupRange &&
                Input.GetKeyDown(KeyCode.E))
                TryCollectCore();
            if (hasCore && extractionPoint != null &&
                Vector3.Distance(player.position, extractionPoint.position) <= extractionRange)
            {
                won = true;
                UnlockCursor();
                Debug.Log("BLACKSITE_MISSION extraction complete");
                return;
            }

            bool threatened = false;
            if (enemies != null)
                foreach (SubmissionAgentHealth enemy in enemies)
                    if (enemy != null && enemy.gameObject.activeInHierarchy &&
                        Vector3.Distance(player.position, enemy.transform.position) < captureRange)
                        threatened = true;
            captureTime = threatened ? captureTime + Time.deltaTime : Mathf.Max(0f, captureTime - 2f * Time.deltaTime);
            if (captureTime < 1.25f) return;
            lost = true;
            UnlockCursor();
            Debug.Log("BLACKSITE_MISSION operative captured");
        }

        public bool TryCollectCore()
        {
            if (hasCore || player == null || coreVisual == null ||
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
            GUI.Box(new Rect(12, 12, 570, 120), "RESONANCE // BLACKSITE — Recovery Operation");
            string objective = won ? "EXTRACTION COMPLETE — press R to play again" :
                lost ? "OPERATIVE CAPTURED — press R to retry" :
                hasCore ? "CORE SECURED — return to ENTRY for extraction" :
                "FIND THE RESONANCE CORE in CORE VAULT; press E to collect";
            GUI.Label(new Rect(24, 40, 540, 25), objective);
            GUI.Label(new Rect(24, 66, 540, 22), "WASD move • mouse look • left click pulse • E interact • G decoy • Esc unlock");
            if (!won && !lost && captureTime > 0f)
                GUI.Label(new Rect(24, 91, 540, 22), "WARNING — enemy contact! Move away or fire.");
        }
    }
}
