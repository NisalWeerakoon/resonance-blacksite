using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// Assign this bridge to real player/door interaction events in the integration scene.
    /// It doesn't monitor or fetch the player's transform continuously for AI decisions.
    /// </summary>
    public sealed class EchoGameplayEventRelay : MonoBehaviour
    {
        [SerializeField] private EchoWorldContext world;
        [SerializeField, Range(0f, 1f)] private float footstepLoudness = 0.23f;
        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<EchoWorldContext>();
        }

        private void Publish(Vector3 location, float loudness, EchoNoiseCategory category)
        {
            if (world == null)
            {
                Debug.LogWarning("ECHO relay requires an EchoWorldContext.", this);
                return;
            }
            EchoNoiseBus.EmitNow(world.ClosestRoom(location), location, loudness, category);
        }

        public void FootstepAt(Vector3 position) => Publish(position, footstepLoudness, EchoNoiseCategory.Footstep);
        public void GunshotAt(Vector3 position) => Publish(position, 1f, EchoNoiseCategory.Gunshot);
        public void DecoyImpactAt(Vector3 position) => Publish(position, 0.9f, EchoNoiseCategory.DecoyImpact);
        public void DoorImpactAt(Vector3 position) => Publish(position, 0.35f, EchoNoiseCategory.DoorImpact);

        public void SetDoorOpen(string portalId) => SetDoor(portalId, EchoDoorAcousticState.Open);
        public void SetDoorClosed(string portalId) => SetDoor(portalId, EchoDoorAcousticState.Closed);
        public void SetDoorSealed(string portalId) => SetDoor(portalId, EchoDoorAcousticState.Sealed);

        private void SetDoor(string portalId, EchoDoorAcousticState state)
        {
            if (world != null) world.SetPortal(portalId, state);
        }
    }
}
