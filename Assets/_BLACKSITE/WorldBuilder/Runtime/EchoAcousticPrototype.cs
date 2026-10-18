using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Blacksite7.WorldBuilder
{
    /// <summary>
    /// ECHO proof of concept: room-portal Dijkstra + simple Utility AI choice.
    /// No enemy movement here: the Agent Controller should implement path following.
    /// N emits test noise in the player's room; T toggles D02 in the acoustic graph.
    /// F1 toggles debug HUD. This is NOT the finished IS agent.
    /// </summary>
    public class EchoAcousticPrototype : MonoBehaviour
    {
        public string echoRoomId = "ACOUSTIC";
        public Transform demoPlayer;
        public bool showDebug = true;
        public float noiseLoudness = 1f;
        public float attenuationFactor = 0.4f;
        public float ageDecayRate = 0.08f;
        [Range(0, 1)] public float investigateThreshold = 0.3f;

        private DoorPortalData[] doors;
        private RoomVolumeData[] rooms;
        private string lastNoiseRoom = "(none)";
        private float lastCost = float.PositiveInfinity;
        private float lastNoiseTime = -10000f;
        private float originalConfidence;
        private string decision = "Patrol";

        private void Start()
        {
            RefreshWorld();
        }

        public void RefreshWorld()
        {
            doors = FindObjectsByType<DoorPortalData>(FindObjectsSortMode.None);
            rooms = FindObjectsByType<RoomVolumeData>(FindObjectsSortMode.None);
        }

        private bool PressedN()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.N);
#else
            return false;
#endif
        }
        private bool PressedT()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.T);
#else
            return false;
#endif
        }
        private bool PressedF1()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F1);
#else
            return false;
#endif
        }

        private void Update()
        {
            if (PressedF1()) showDebug = !showDebug;
            if (PressedT())
            {
                foreach (var door in doors)
                    if (door != null && door.doorId == "D02")
                    {
                        door.isOpen = !door.isOpen;
                        Debug.Log("BLACKSITE ECHO: D02 " + (door.isOpen ? "open" : "closed") + " (acoustic data only)");
                        break;
                    }
            }
            if (PressedN())
            {
                string room = FindRoom(demoPlayer ? demoPlayer.position : transform.position);
                EmitDemoNoise(room, noiseLoudness);
            }
            float confidence = originalConfidence * Mathf.Exp(-ageDecayRate * Mathf.Max(0f, Time.time - lastNoiseTime));
            // A simple normalized pair of utilities; replace with full action scoring later.
            float investigateUtility = confidence;
            float patrolUtility = investigateThreshold;
            decision = investigateUtility > patrolUtility ? "InvestigateSound" : "Patrol";
        }

        public void EmitDemoNoise(string sourceRoom, float loudness)
        {
            if (doors == null || rooms == null) RefreshWorld();
            lastNoiseRoom = sourceRoom;
            lastCost = SolveDijkstra(sourceRoom, echoRoomId);
            lastNoiseTime = Time.time;
            originalConfidence = float.IsInfinity(lastCost) ? 0 : Mathf.Clamp01(loudness * Mathf.Exp(-attenuationFactor * lastCost));
            Debug.Log($"ECHO heard test noise: room={sourceRoom}, acousticCost={lastCost:0.0}, confidence={originalConfidence:0.00}");
        }

        // Pure Dijkstra over weighted, nonnegative door portal costs.
        public float SolveDijkstra(string sourceRoom, string destinationRoom)
        {
            if (rooms == null || doors == null) RefreshWorld();
            var dist = new Dictionary<string, float>();
            var visited = new HashSet<string>();
            foreach (var room in rooms) if (room != null && !dist.ContainsKey(room.roomId)) dist[room.roomId] = float.PositiveInfinity;
            if (!dist.ContainsKey(sourceRoom) || !dist.ContainsKey(destinationRoom)) return float.PositiveInfinity;
            dist[sourceRoom] = 0f;
            while (visited.Count < dist.Count)
            {
                string current = null;
                float lowest = float.PositiveInfinity;
                foreach (var pair in dist)
                    if (!visited.Contains(pair.Key) && pair.Value < lowest) { current = pair.Key; lowest = pair.Value; }
                if (current == null || current == destinationRoom) break;
                visited.Add(current);
                foreach (var door in doors)
                {
                    if (door == null) continue;
                    string next = door.roomA == current ? door.roomB : (door.roomB == current ? door.roomA : null);
                    if (next == null || !dist.ContainsKey(next)) continue;
                    float candidate = lowest + Mathf.Max(0f, door.AcousticCost);
                    if (candidate < dist[next]) dist[next] = candidate;
                }
            }
            return dist[destinationRoom];
        }

        private string FindRoom(Vector3 position)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;
                var box = room.GetComponent<BoxCollider>();
                if (box != null && box.bounds.Contains(position)) return room.roomId;
            }
            // Corridor sound source maps to the nearest room; acceptable for a prototype.
            string nearest = echoRoomId;
            float best = float.PositiveInfinity;
            foreach (var room in rooms)
            {
                if (room == null) continue;
                float d = (room.transform.position - position).sqrMagnitude;
                if (d < best) { best = d; nearest = room.roomId; }
            }
            return nearest;
        }

        private void OnGUI()
        {
            if (!showDebug) return;
            float confidence = originalConfidence * Mathf.Exp(-ageDecayRate * Mathf.Max(0f, Time.time - lastNoiseTime));
            string d02 = "?";
            if (doors != null) foreach (var d in doors) if (d != null && d.doorId == "D02") d02 = d.isOpen ? "OPEN" : "CLOSED";
            GUI.Box(new Rect(12, 12, 420, 162),
                "BLACKSITE-7 | WORLD BUILDER + ECHO PROTOTYPE\n" +
                "WASD move | mouse look (click game first) | Esc unlock\n" +
                "N noise in player room | T toggle D02 | F1 HUD\n" +
                "D02: " + d02 + "   Noise room: " + lastNoiseRoom + "\n" +
                "Dijkstra acoustic cost: " + (float.IsInfinity(lastCost) ? "Unreachable" : lastCost.ToString("0.0")) +
                "   Confidence: " + confidence.ToString("0.00") + "\n" +
                "Utility choice: " + decision + " (prototype only)");
        }
    }
}
