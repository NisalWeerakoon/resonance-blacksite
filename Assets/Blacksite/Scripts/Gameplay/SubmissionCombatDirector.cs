using Blacksite.Agents.Bastion;
using Blacksite.Agents.Echo;
using Blacksite.Agents.Rift;
using Blacksite.Agents.Trace;
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
        private readonly string[] roles = { "sound hunter", "cover marksman", "flank pursuer", "route interceptor" };

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
            // Brains handle pursuit, cover, flanking and interception. The attack
            // layer gives each visible opponent a distinct, dodgeable ranged shot.
            RangedAttack(0, 14f, 8, 2.1f, 10f, Color.cyan);
            RangedAttack(1, 23f, 11, 1.25f, 23f, new Color(1f, .5f, .08f));
            RangedAttack(2, 11f, 14, 1.55f, 19f, Color.red);
            RangedAttack(3, 20f, 10, 1.7f, 32f, Color.magenta);
        }

        private void RangedAttack(int index, float range, int damage, float cooldown,
            float speed, Color color)
        {
            if (agents[index] == null || !agents[index].gameObject.activeInHierarchy ||
                Time.time < nextAttack[index]) return;
            Vector3 origin = agents[index].transform.position + Vector3.up * 1.45f;
            Vector3 destination = player.transform.position + Vector3.up;
            float distance = Vector3.Distance(origin, destination);
            if (distance > range ||
                (distance > 3f && !ClearShot(origin, destination, agents[index].transform))) return;
            nextAttack[index] = Time.time + cooldown;
            Debug.Log($"BLACKSITE_BOLT fire={names[index]} distance={distance:0.0}");
            PulseProjectile.Launch(origin, destination - origin, agents[index].transform,
                false, speed, range, damage, color, names[index]);
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
            PulseProjectile.Launch(origin, destination - origin, bastion.transform,
                false, 23f, 24f, 11, new Color(1f, .5f, .08f), "BASTION");
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
            float width = 285f;
            float left = Screen.width - width - 14f;
            GUI.Box(new Rect(left, 12f, width, 148f), "FOUR ACTIVE THREATS");
            for (int i = 0; i < Mathf.Min(agents.Length, 4); i++)
            {
                bool active = agents[i] != null && agents[i].gameObject.activeInHierarchy;
                string distance = active ?
                    $"{Vector3.Distance(player.transform.position, agents[i].transform.position):0}m • {agents[i].HitsRemaining} hits" :
                    "DISABLED";
                GUI.Label(new Rect(left + 12f, 38f + i * 28f, width - 20f, 25f),
                    $"{names[i]} — {roles[i]} — {distance}");
            }
        }
    }
}
