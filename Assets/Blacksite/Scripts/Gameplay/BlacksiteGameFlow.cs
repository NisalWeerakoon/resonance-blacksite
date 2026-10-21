using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Blacksite.Gameplay
{
    public sealed class BlacksiteGameFlow : MonoBehaviour
    {
        [SerializeField] private BlacksiteMission mission;
        private bool started;
        public static bool IsPlaying { get; private set; }

        public void Configure(BlacksiteMission target) => mission = target;

        private void Awake()
        {
            started = Application.isBatchMode ||
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-skipTitle") >= 0;
            IsPlaying = started;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (!started)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame ||
                                         keyboard.spaceKey.wasPressedThisFrame)) StartMission();
                return;
            }
            if (mission == null || !started) return;
            if (mission.Won || mission.Lost)
            {
                IsPlaying = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.rKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                    RetryMission();
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            IsPlaying = true;
        }

        private void OnGUI()
        {
            if (mission == null || started && !mission.Won && !mission.Lost) return;
            GUI.depth = -1000;
            float width = Mathf.Min(600f, Screen.width - 30f);
            float left = (Screen.width - width) * .5f;
            float top = (Screen.height - 350f) * .5f;
            GUI.color = new Color(.02f, .04f, .08f, .82f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Box(new Rect(left, top, width, 350f), "RESONANCE // BLACKSITE — COMBAT FIX 2");
            string heading = !started ? "RECOVERY OPERATION" : mission.Won ?
                "EXTRACTION COMPLETE" : "OPERATIVE DOWN";
            GUI.Label(new Rect(left + 30f, top + 55f, width - 60f, 35f), heading);
            string instructions = !started ?
                "Activate three room relays, secure the Core Vault's Resonance Core, then return to ENTRY.\n" +
                "ECHO hunts noise • BASTION uses cover and fires • RIFT flanks • TRACE intercepts.\n" +
                "Survive with your pulse carbine. Disable threats or evade them." :
                mission.Won ? "Core recovered. The operation is complete." :
                "Your health reached zero. Retry and use cover, decoys and your pulse carbine.";
            GUI.Label(new Rect(left + 30f, top + 105f, width - 60f, 100f), instructions);
            GUI.Label(new Rect(left + 30f, top + 218f, width - 60f, 32f),
                "WASD move  •  mouse aim  •  left click fire  •  R reload  •  G decoy  •  E interact");
            if (GUI.Button(new Rect(left + 30f, top + 275f, width - 60f, 48f),
                    !started ? "START MISSION  [ENTER]" : "RETRY MISSION"))
            {
                if (!started) StartMission();
                else RetryMission();
            }
        }

        private void StartMission()
        {
            started = true;
            IsPlaying = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void RetryMission()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
