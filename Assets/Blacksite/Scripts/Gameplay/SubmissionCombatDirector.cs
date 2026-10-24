using Blacksite.Agents;
using Blacksite.Agents.Bastion;
using Blacksite.Agents.Echo;
using Blacksite.Agents.Rift;
using Blacksite.Agents.Trace;
using Blacksite.Agents.Movement;
using Blacksite.Shared;
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
        private readonly Vector3[] lastKnownPositions = new Vector3[4];
        private readonly float[] lastIntelTimes = new float[4];
        private readonly bool[] hasIntel = new bool[4];
        private readonly bool[] playerVisible = new bool[4];
        private readonly int[] searchPasses = new int[4];
        private static readonly float[] SightRanges = { 18f, 25f, 20f, 23f };
        private const float SightFieldOfView = 150f;
        private const float IntelMemorySeconds = 14f;
        private const float OpeningSearchSeconds = 7f;
        private float nextTactic;
        private float lastTacticAt;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private bool openingSearchStarted;

        public void Configure(PlayerVitals target, SubmissionAgentHealth[] threats, BastionBrain bastionBrain)
        {
            player = target;
            agents = threats;
            bastion = bastionBrain;
        }

        private void OnEnable()
        {
            WorldSignals.NoiseEmitted += OnNoiseEmitted;
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
            WorldSignals.NoiseEmitted -= OnNoiseEmitted;
            if (bastion != null) bastion.AttackRequested -= OnBastionAttack;
        }

        private void Update()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            if (player == null || player.IsDead || agents == null || agents.Length < 4) return;
            if (!openingSearchStarted)
            {
                openingSearchStarted = true;
                for (int i = 0; i < 4; i++)
                {
                    if (agents[i] == null) continue;
                    RememberPlayerPosition(i, player.transform.position);
                    lastIntelTimes[i] = Time.time;
                    tactics[i] = "SEARCHING ENTRY";
                    if (i == 0 && agents[i].TryGetComponent(out EchoBrain echo))
                        echo.Observe(new WorldObservation(player.transform.position, .35f, Time.timeAsDouble));
                    if (i == 1 && bastion != null)
                        bastion.Observe(new WorldObservation(player.transform.position, .35f, Time.timeAsDouble));
                    if ((i == 2 || i == 3) && agents[i].TryGetComponent(out WaypointAgentMotor openingMotor))
                        openingMotor.SetDestination(player.transform.position);
                }
            }
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
            for (int i = 0; i < 4; i++)
            {
                if (agents[i] == null || !agents[i].gameObject.activeInHierarchy) continue;
                playerVisible[i] = CanSeePlayer(i, agents[i].transform);
                if (playerVisible[i]) RememberPlayerPosition(i, player.transform.position);
                bool hasRecentIntel = hasIntel[i] && Time.time - lastIntelTimes[i] <= IntelMemorySeconds;
                if (!hasRecentIntel)
                {
                    tactics[i] = "SEARCHING AREA";
                    continue;
                }

                var motor = agents[i].GetComponent<WaypointAgentMotor>();
                if (motor == null) continue;
                Vector3 target = lastKnownPositions[i];
                switch (i)
                {
                    case 0:
                        // Echo's authored acoustic brain chooses routes to sounds. The
                        // director only supplies the last known point for the opening sweep.
                        if (!playerVisible[i] && Time.time - lastIntelTimes[i] > OpeningSearchSeconds)
                        {
                            if (!motor.HasDestination || motor.HasArrived) SetSearchDestination(i, motor);
                            tactics[i] = "INVESTIGATING SOUND";
                        }
                        else tactics[i] = playerVisible[i] ? "PLAYER SIGHTED" : "SEARCHING ENTRY";
                        break;
                    case 1:
                        // Bastion's behavior tree owns cover selection and last-seen search.
                        tactics[i] = playerVisible[i] ? "SUPPRESSING FROM COVER" : "SEARCHING LAST SIGHTING";
                        break;
                    case 2:
                        if (playerVisible[i])
                        {
                            Vector3 lateral = Vector3.Cross(Vector3.up,
                                (target - agents[i].transform.position).normalized);
                            motor.SetDestination(target + lateral * 3f);
                            tactics[i] = "FLANKING SIDE LANE";
                        }
                        else
                        {
                            if (!motor.HasDestination || motor.HasArrived) SetSearchDestination(i, motor);
                            tactics[i] = "INVESTIGATING LAST SIGHTING";
                        }
                        break;
                    case 3:
                        if (playerVisible[i])
                        {
                            motor.SetDestination(target + Vector3.ClampMagnitude(playerVelocity * 1.5f, 4f));
                            tactics[i] = "INTERCEPTING ROUTE";
                        }
                        else
                        {
                            if (!motor.HasDestination || motor.HasArrived) SetSearchDestination(i, motor);
                            tactics[i] = "SEARCHING LAST SIGHTING";
                        }
                        break;
                }
            }
        }

        private void OnNoiseEmitted(NoiseSignal noise)
        {
            if (!BlacksiteGameFlow.IsPlaying || agents == null) return;
            float hearingRadius = 3f + noise.Loudness * 32f;
            float hearingRadiusSquared = hearingRadius * hearingRadius;
            for (int i = 0; i < Mathf.Min(agents.Length, 4); i++)
            {
                if (agents[i] == null || !agents[i].gameObject.activeInHierarchy ||
                    (agents[i].transform.position - noise.Position).sqrMagnitude > hearingRadiusSquared)
                    continue;
                RememberPlayerPosition(i, noise.Position);
                if (i == 1 && bastion != null)
                    bastion.Observe(new WorldObservation(noise.Position, noise.Loudness, Time.timeAsDouble));
                if ((i == 2 || i == 3) && agents[i].TryGetComponent(out WaypointAgentMotor motor))
                    motor.SetDestination(noise.Position);
                if (i != 0) tactics[i] = "INVESTIGATING SOUND";
            }
        }

        private void RememberPlayerPosition(int index, Vector3 position)
        {
            if (agents == null || index < 0 || index >= agents.Length || agents[index] == null) return;
            lastKnownPositions[index] = position;
            lastIntelTimes[index] = Time.time;
            hasIntel[index] = true;
            searchPasses[index] = 0;
            var brain = agents[index].GetComponent<AgentBrainBase>();
            if (brain != null && playerVisible[index])
                brain.Observe(new WorldObservation(position, 1f, Time.timeAsDouble));
        }

        private bool CanSeePlayer(int index, Transform observer)
        {
            Vector3 origin = observer.position + Vector3.up * 1.55f;
            Collider playerCollider = player.GetComponent<Collider>();
            Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.transform.position + Vector3.up;
            Vector3 delta = target - origin;
            float distance = delta.magnitude;
            if (distance > SightRanges[index] || distance < .01f ||
                Vector3.Angle(observer.forward, delta) > SightFieldOfView * .5f) return false;

            float closest = float.PositiveInfinity;
            Transform closestTransform = null;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, delta / distance, distance,
                         ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == observer || hit.transform.IsChildOf(observer)) continue;
                if (hit.distance >= closest) continue;
                closest = hit.distance;
                closestTransform = hit.transform;
            }
            return closestTransform == player.transform ||
                closestTransform != null && closestTransform.IsChildOf(player.transform);
        }

        private void SetSearchDestination(int index, WaypointAgentMotor motor)
        {
            Vector3 center = lastKnownPositions[index];
            int pass = searchPasses[index]++;
            float angle = (pass * 90f + index * 90f) * Mathf.Deg2Rad;
            float radius = pass == 0 ? 0f : 2.5f + (pass % 2) * 1.5f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            motor.SetDestination(center + offset);
        }

        private void RangedAttack(int index, float range, int damage, float cooldown,
            Color color)
        {
            if (agents[index] == null || !agents[index].gameObject.activeInHierarchy ||
                !playerVisible[index] || Time.time < nextAttack[index]) return;
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
                !bastion.gameObject.activeInHierarchy || !playerVisible[1] || Time.time < nextAttack[1]) return;
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
