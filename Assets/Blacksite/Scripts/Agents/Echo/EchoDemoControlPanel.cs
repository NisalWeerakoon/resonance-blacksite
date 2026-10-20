using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>Click-based demo UI works even when the project disables Legacy Input.</summary>
    public sealed class EchoDemoControlPanel : MonoBehaviour
    {
        [SerializeField] private EchoWorldContext world;
        [SerializeField] private EchoBrain echo;
        private string lastEvent = "Ready: choose a test below";
        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<EchoWorldContext>();
            if (echo == null) echo = FindFirstObjectByType<EchoBrain>();
        }

        private void Emit(string room, float loudness, EchoNoiseCategory type)
        {
            if (world == null) return;
            EchoNoiseEvent sound = EchoNoiseBus.EmitNow(room, world.Spatial.Position(room), loudness, type);
            lastEvent = "Emitted noise " + sound.Id + " in " + room;
        }

        private void OnGUI()
        {
            const int width = 370;
            GUILayout.BeginArea(new Rect(12, 12, width, 420), GUI.skin.box);
            GUILayout.Label("BLACKSITE-7 / ECHO AI DEMONSTRATION");
            GUILayout.Label("Acoustic Dijkstra -> Utility AI -> shared A*");
            if (GUILayout.Button("1  Decoy in HUB")) Emit(EchoRoomIds.Hub, 1f, EchoNoiseCategory.DecoyImpact);
            if (GUILayout.Button("2  Decoy in ACOUSTIC")) Emit(EchoRoomIds.Acoustic, 1f, EchoNoiseCategory.DecoyImpact);
            if (GUILayout.Button("3  Two competing noises"))
            {
                Emit(EchoRoomIds.Security, 0.35f, EchoNoiseCategory.Footstep);
                Emit(EchoRoomIds.Hub, 1f, EchoNoiseCategory.DecoyImpact);
            }
            if (GUILayout.Button("4  Toggle D02 open/closed") && world != null)
            {
                world.TogglePortal("D02");
                world.Acoustic.TryGetPortalState("D02", out EchoDoorAcousticState state);
                lastEvent = "D02 = " + state;
            }
            if (GUILayout.Button("5  Toggle D03 open/closed") && world != null)
            {
                world.TogglePortal("D03");
                lastEvent = "D03 changed";
            }
            if (GUILayout.Button("6  Valid TEST sighting in CORE") && echo != null && world != null)
            {
                echo.Observe(new Blacksite.Shared.WorldObservation(world.Spatial.Position(EchoRoomIds.Core), 1f, Time.timeAsDouble));
                lastEvent = "Injected DEMO sighting (not live tracking)";
            }
            GUILayout.Label(lastEvent);
            if (echo != null)
            {
                GUILayout.Label("Action: " + echo.CurrentAction + " | Sound ID: " + echo.HeardSoundId);
                GUILayout.Label("Confidence: " + echo.HearingConfidence.ToString("F3") +
                                " | Acoustic cost: " + (float.IsInfinity(echo.AcousticCost) ? "Unreachable" : echo.AcousticCost.ToString("F1")));
                GUILayout.Label("Dijkstra expanded: " + echo.AcousticNodesExpanded +
                                " | Movement waypoints: " + echo.WaypointCount);
                EchoUtilityDecision u = echo.Scores;
                GUILayout.Label("Utility P:" + u.Patrol.ToString("F2") + " I:" + u.Investigate.ToString("F2") +
                                " S:" + u.Search.ToString("F2") + " C:" + u.Chase.ToString("F2"));
                GUILayout.Label("Reason: " + echo.LastReason);
            }
            GUILayout.EndArea();
        }
    }
}
