using Blacksite.Agents.Bastion;
using Blacksite.Agents.Echo;
using Blacksite.Agents.Rift;
using Blacksite.Agents.Trace;
using Blacksite.Agents.Movement;
using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class SubmissionCombatDirector : MonoBehaviour
    {
        [SerializeField] private PlayerVitals player;
        [SerializeField] private SubmissionAgentHealth[] agents;
        [SerializeField] private BastionBrain bastion;
        private readonly float[] nextAttack = new float[4];
        private readonly string[] names = { "ECHO", "BASTION", "RIFT", "TRACE" };
        private readonly string[] rooms = { "ACOUSTIC", "SECURITY", "MAINT", "CORE" };
        private readonly string[] tactics = { "LISTENING", "HOLDING COVER", "FLANKING", "PREDICTING" };
        private float nextTactic;
        private float lastTacticAt;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;

        public void Configure(PlayerVitals target, SubmissionAgentHealth[] threats, BastionBrain bastionBrain)
        {
            player = target;
            agents = threats;
            bastion = bastionBrain;
        }

        private void OnEnable()
        {
            if (bastion != null) bastion.AttackRequested += OnBastionAttack;
        }

        private void Start()
        {
            // Builder sets serialized references after AddComponent/OnEnable.
            if (bastion != null) bastion.AttackRequested -= OnBastionAttack;
            if (bastion != null) bastion.AttackRequested += OnBastionAttack;
        }

        private void OnDisable()
        {
            if (bastion != null) bastion.AttackRequested -= OnBastionAttack;
        }

        private void Update()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            if (player == null || player.IsDead || agents == null || agents.Length < 4) return;
            if (Time.time >= nextTactic)
            {
                playerVelocity = lastTacticAt > 0f ?
                    (player.transform.position - lastPlayerPosition) /
                    Mathf.Max(.1f, Time.time - lastTacticAt) : Vector3.zero;
                lastPlayerPosition = player.transform.position;
                lastTacticAt = Time.time;
                nextTactic = Time.time + 1f;
                UpdateTactics();
            }
            // Brains handle pursuit, cover, flanking and interception. The attack
            // layer gives each visible opponent a distinct, dodgeable ranged shot.
            RangedAttack(0, 15f, 8, 1.9f, Color.cyan);
            RangedAttack(1, 24f, 12, 1.35f, new Color(1f, .5f, .08f));
            RangedAttack(2, 12f, 16, 1.6f, Color.red);
            RangedAttack(3, 23f, 14, 1.8f, Color.magenta);
        }

        private void UpdateTactics()
        {
            // The authored teammate brains remain active. This makes their combat
            // purpose visible when a player enters an encounter and a sensor misses.
            for (int i = 0; i < 4; i++)
            {
                if (agents[i] == null || !agents[i].gameObject.activeInHierarchy) continue;
                float distance = Vector3.Distance(agents[i].transform.position, player.transform.position);
                if (distance > 25f) continue;
                var motor = agents[i].GetComponent<WaypointAgentMotor>();
                if (motor == null) continue;
                Vector3 target = player.transform.position;
                switch (i)
                {
                    case 0: // ECHO closes in on sound and pressure.
                        if (distance < 14f) { motor.SetDestination(target); tactics[i] = "HUNTING SOUND"; }
                        break;
                    case 1: // BASTION preserves the cover route selected by its brain.
                        tactics[i] = "SUPPRESSING FROM COVER";
                        break;
                    case 2: // RIFT takes a side lane instead of tailing the player.
                        Vector3 lateral = Vector3.Cross(Vector3.up,
                            (target - agents[i].transform.position).normalized);
                        motor.SetDestination(target + lateral * 3f);
                        tactics[i] = "FLANKING SIDE LANE";
                        break;
                    case 3: // TRACE aims for the player's projected route.
                        motor.SetDestination(target + Vector3.ClampMagnitude(playerVelocity * 1.5f, 4f));
                        tactics[i] = "INTERCEPTING ROUTE";
                        break;
                }
            }
        }

        private void RangedAttack(int index, float range, int damage, float cooldown,
            Color color)
        {
            if (agents[index] == null || !agents[index].gameObject.activeInHierarchy ||
                Time.time < nextAttack[index]) return;
            Vector3 origin = agents[index].transform.position + Vector3.up * 1.45f;
            Vector3 destination = player.transform.position + Vector3.up;
            float distance = Vector3.Distance(origin, destination);
            if (distance > range ||
                (distance > 3f && !ClearShot(origin, destination, agents[index].transform))) return;
            nextAttack[index] = Time.time + cooldown;
            PulseProjectile.FireEnemy(origin, destination, agents[index].transform,
                player, damage, color, names[index]);
        }

        private void OnBastionAttack(Transform target)
        {
            if (player == null || player.IsDead || bastion == null ||
                !bastion.gameObject.activeInHierarchy || Time.time < nextAttack[1]) return;
            Vector3 origin = bastion.transform.position + Vector3.up * 1.7f;
            Vector3 destination = player.transform.position + Vector3.up;
            if (Vector3.Distance(origin, destination) > 24f ||
                !ClearShot(origin, destination, bastion.transform)) return;
            nextAttack[1] = Time.time + 1.2f;
            PulseProjectile.FireEnemy(origin, destination, bastion.transform,
                player, 12, new Color(1f, .5f, .08f), "BASTION");
        }

        private bool ClearShot(Vector3 from, Vector3 to, Transform attacker)
        {
            Vector3 delta = to - from;
            float closest = delta.magnitude + .001f;
            Collider nearest = null;
            foreach (RaycastHit hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude,
                         ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(attacker) || hit.distance >= closest) continue;
                closest = hit.distance;
                nearest = hit.collider;
            }
            return nearest == null || nearest.transform.IsChildOf(player.transform);
        }

        private void OnGUI()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            if (player == null || agents == null) return;
            float width = 380f;
            float left = Screen.width - width - 14f;
            int activeCount = 0;
            foreach (SubmissionAgentHealth threat in agents)
                if (threat != null && threat.gameObject.activeInHierarchy) activeCount++;
            GUI.Box(new Rect(left, 12f, width, 178f), $"THREAT RADAR — {activeCount}/4 ACTIVE");
            for (int i = 0; i < Mathf.Min(agents.Length, 4); i++)
            {
                bool active = agents[i] != null && agents[i].gameObject.activeInHierarchy;
                string distance = active ?
                    $"{Vector3.Distance(player.transform.position, agents[i].transform.position):0}m • {agents[i].HitsRemaining}/3 armor" :
                    "DISABLED";
                GUI.Label(new Rect(left + 12f, 36f + i * 36f, width - 20f, 20f),
                    $"{names[i]}  [{rooms[i]}]  {distance}");
                GUI.Label(new Rect(left + 24f, 54f + i * 36f, width - 30f, 17f),
                    active ? tactics[i] : "NEUTRALIZED");
            }
        }
    }
}
