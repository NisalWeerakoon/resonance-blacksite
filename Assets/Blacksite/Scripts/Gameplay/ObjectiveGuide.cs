using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class ObjectiveGuide : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private BlacksiteMission mission;
        [SerializeField] private SubmissionRelay[] relays;
        [SerializeField] private Transform core;
        [SerializeField] private Transform extraction;
        private GUIStyle arrowStyle;

        public void Configure(Transform target, Camera camera, BlacksiteMission currentMission,
            SubmissionRelay[] objectives, Transform coreTarget, Transform exit)
        {
            player = target;
            viewCamera = camera;
            mission = currentMission;
            relays = objectives;
            core = coreTarget;
            extraction = exit;
        }

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying || player == null || mission == null) return;
            Transform target = null;
            string label = "";
            if (relays != null)
                foreach (SubmissionRelay relay in relays)
                    if (relay != null && !relay.Activated)
                    {
                        target = relay.transform;
                        label = "ACTIVATE " + relay.name.Replace("Relay_", "") + " RELAY [E]";
                        break;
                    }
            if (target == null && !mission.HasCore && core != null)
            {
                target = core;
                label = "RECOVER CORE [E]";
            }
            if (target == null && mission.HasCore)
            {
                target = extraction;
                label = "RETURN TO ENTRY — EXTRACT";
            }
            if (target == null) return;
            Vector3 direction = target.position - player.position;
            direction.y = 0f;
            Vector3 forward = viewCamera != null ? viewCamera.transform.forward : player.forward;
            forward.y = 0f;
            float angle = Vector3.SignedAngle(forward, direction, Vector3.up);
            float cx = Screen.width * .5f;
            GUI.Box(new Rect(cx - 180f, 12f, 360f, 72f), "OBJECTIVE GUIDE");
            GUI.Label(new Rect(cx - 130f, 39f, 310f, 28f), $"{label}  •  {direction.magnitude:0}m");
            if (arrowStyle == null)
                arrowStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = Color.cyan;
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(cx - 150f, 53f));
            GUI.Label(new Rect(cx - 175f, 28f, 50f, 50f), "↑", arrowStyle);
            GUI.matrix = old;
            GUI.color = Color.white;
        }
    }
}
