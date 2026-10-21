using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SubmissionRelay : MonoBehaviour
    {
        [SerializeField] private string relayName;
        [SerializeField] private Renderer indicator;
        public bool Activated { get; private set; }
        public string RelayName => relayName;
        private Transform player;

        private void Start()
        {
            var operative = GameObject.Find("DemoPlayer_FPS");
            if (operative != null) player = operative.transform;
        }

        public void Configure(string label, Renderer visual)
        {
            relayName = label;
            indicator = visual;
        }

        public void Activate()
        {
            if (Activated) return;
            Activated = true;
            if (indicator != null)
            {
                var block = new MaterialPropertyBlock();
                indicator.GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.green);
                indicator.SetPropertyBlock(block);
            }
            Debug.Log("BLACKSITE_RELAY activated=" + relayName);
        }

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying || Activated || player == null ||
                Vector3.Distance(player.position, transform.position) > 2.5f) return;
            GUI.Box(new Rect(Screen.width * .5f - 150f, Screen.height - 160f, 300f, 43f),
                "PRESS E  •  ACTIVATE " + relayName + " RELAY");
        }
    }
}
