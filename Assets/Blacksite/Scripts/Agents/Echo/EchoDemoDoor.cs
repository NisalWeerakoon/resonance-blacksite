using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>Visual and physical blocker ONLY for the generated ECHO demo.</summary>
    public sealed class EchoDemoDoor : MonoBehaviour
    {
        [SerializeField] private string portalId;
        [SerializeField] private GameObject blocker;
        [SerializeField] private Renderer indicator;
        public string PortalId => portalId;

        public void Setup(string id, GameObject physicalBlocker, Renderer statusIndicator)
        {
            portalId = id;
            blocker = physicalBlocker;
            indicator = statusIndicator;
        }

        private void OnEnable()
        {
            EchoWorldContext context = FindFirstObjectByType<EchoWorldContext>();
            if (context != null) context.RegisterDoor(this);
        }

        public void SetVisuals(EchoDoorAcousticState state)
        {
            // Closing a demo door blocks direct traversal; it still attenuates sound.
            if (blocker != null) blocker.SetActive(state != EchoDoorAcousticState.Open);
            if (indicator != null)
            {
                Color c = state == EchoDoorAcousticState.Open ? Color.cyan :
                          state == EchoDoorAcousticState.Closed ? new Color(1f, 0.52f, 0.12f) : Color.red;
                indicator.material.color = c;
                if (indicator.material.HasProperty("_EmissionColor"))
                    indicator.material.SetColor("_EmissionColor", c * 1.8f);
            }
        }
    }
}
