using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SystemsSandboxTelemetry : MonoBehaviour
    {
        [SerializeField] private DoorPortal door;
        [SerializeField] private MovableBarricade barricade;
        private string lastSignal = "No signal yet";

        public void ConfigureSandbox(DoorPortal portal, MovableBarricade block)
        {
            door = portal;
            barricade = block;
        }
        private void OnEnable()
        {
            WorldSignals.NoiseEmitted += OnNoise;
            WorldSignals.DoorStateChanged += OnDoor;
            WorldSignals.BarricadeMoved += OnBarricade;
        }
        private void OnDisable()
        {
            WorldSignals.NoiseEmitted -= OnNoise;
            WorldSignals.DoorStateChanged -= OnDoor;
            WorldSignals.BarricadeMoved -= OnBarricade;
        }
        private void OnNoise(NoiseSignal signal) =>
            lastSignal = $"Decoy noise {signal.Loudness:F1} at {signal.Position}";
        private void OnDoor(string id, bool open) =>
            lastSignal = $"Door {id} {(open ? "open" : "closed")}";
        private void OnBarricade(Bounds bounds) =>
            lastSignal = $"Barricade moved at {bounds.center}";
        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 450, 144), "BLACKSITE Systems Engineer sandbox");
            GUI.Label(new Rect(24, 38, 430, 22), "Click Game to lock mouse; WASD move; E interact; G throw; Esc unlock");
            GUI.Label(new Rect(24, 62, 430, 22),
                $"Door: {(door != null && door.IsTraversable ? "OPEN" : "CLOSED")}   Barricade: {(barricade != null && barricade.IsTraversable ? "CLEAR" : "BLOCKED")}");
            GUI.Label(new Rect(24, 86, 430, 22), "BASTION decisions and cover scores are logged in Console");
            GUI.Label(new Rect(24, 110, 430, 22), lastSignal);
        }
    }
}
