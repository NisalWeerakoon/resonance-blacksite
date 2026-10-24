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
        private readonly bool[] retreating = new bool[4];
        private readonly bool[] postRetreatSearch = new bool[4];
        private readonly bool[] brainWasEnabled = new bool[4];
        private readonly CoverPoint[] retreatCovers = new CoverPoint[4];
        private readonly int[] searchPasses = new int[4];
        private CoverPoint[] coverPoints = System.Array.Empty<CoverPoint>();
        private static readonly float[] SightRanges = { 18f, 25f, 40f, 23f };
        private const float SightFieldOfView = 220f;
        private const float IntelMemorySeconds = 45f;
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
            coverPoints = FindObjectsByType<CoverPoint>(FindObjectsSortMode.None);
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
                    postRetreatSearch[i] = true;
                    if (i == 1 && bastion != null) bastion.SetSearchMode(true);
                    if (i == 1 && bastion != null)
                        bastion.Observe(new WorldObservation(player.transform.position, .35f, Time.timeAsDouble));
                    else
                    {
                        AgentBrainBase brain = agents[i].GetComponent<AgentBrainBase>();
                        if (brain != null)
                        {
                            brainWasEnabled[i] = brain.enabled;
                            brain.enabled = false;
                        }
                        if (agents[i].TryGetComponent(out WaypointAgentMotor openingMotor))
                        {
                            openingMotor.ClearDestination();
                            openingMotor.SetDestination(player.transform.position);
                        }
                    }
                }
            }
            UpdateRetreats(Time.deltaTime);
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
                if (retreating[i])
                {
                    tactics[i] = IsAtRetreatCover(i) ? "RECOVERING IN COVER" : "RETREATING TO COVER";
                    continue;
                }

                if (postRetreatSearch[i])
                {
                    if (playerVisible[i])
                    {
                        postRetreatSearch[i] = false;
                        if (i == 1 && bastion != null) bastion.SetSearchMode(false);
                        if (i != 1)
                        {
                            AgentBrainBase brain = agents[i].GetComponent<AgentBrainBase>();
                            if (brain != null && brainWasEnabled[i])
                            {
                                brain.enabled = true;
                                brain.Observe(new WorldObservation(player.transform.position, 1f, Time.timeAsDouble));
                            }
                        }
                    }
                    else
                    {
                        if (i == 1 && bastion != null)
                            bastion.Observe(new WorldObservation(lastKnownPositions[i], .35f, Time.timeAsDouble));
                        else if (agents[i].TryGetComponent(out WaypointAgentMotor searchMotor) &&
                                 (!searchMotor.HasDestination || searchMotor.HasArrived || searchMotor.IsBlocked))
                            SetSearchDestination(i, searchMotor);
                        tactics[i] = "HUNTING LAST POSITION";
                        continue;
                    }
                }

                bool hasRecentIntel = hasIntel[i] && Time.time - lastIntelTimes[i] <= IntelMemorySeconds;
                if (!hasRecentIntel)
                {
                    tactics[i] = "SEARCHING AREA";
                    continue;
                }

                switch (i)
                {
                    case 0:
                        tactics[i] = playerVisible[i] ? "PLAYER SIGHTED" : "HUNTING SOUND";
                        break;
                    case 1:
                        // Bastion's behavior tree owns cover selection and last-seen search.
                        tactics[i] = playerVisible[i] ? "SUPPRESSING FROM COVER" : "SEARCHING LAST SIGHTING";
                        break;
                    case 2:
                        tactics[i] = playerVisible[i] ? "FLANKING SIDE LANE" : "INVESTIGATING LAST SIGHTING";
                        break;
                    case 3:
                        tactics[i] = playerVisible[i] ? "INTERCEPTING ROUTE" : "SEARCHING LAST SIGHTING";
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
                if (!retreating[i] && !postRetreatSearch[i] && i != 0)
                {
                    AgentBrainBase brain = agents[i].GetComponent<AgentBrainBase>();
                    if (brain != null)
                        brain.Observe(new WorldObservation(noise.Position, noise.Loudness, Time.timeAsDouble));
                }
                if (i == 1 && bastion != null)
                    bastion.Observe(new WorldObservation(noise.Position, noise.Loudness, Time.timeAsDouble));
                if (retreating[i]) continue;
                if ((i == 2 || i == 3 || postRetreatSearch[i]) &&
                    agents[i].TryGetComponent(out WaypointAgentMotor motor))
                    motor.SetDestination(noise.Position);
                if (i != 0) tactics[i] = "INVESTIGATING SOUND";
            }
        }

        private void UpdateRetreats(float deltaTime)
        {
            for (int i = 0; i < 4; i++)
            {
                SubmissionAgentHealth health = agents[i];
                if (health == null) continue;
                if (!health.gameObject.activeInHierarchy)
                {
                    ReleaseRetreatCover(i);
                    retreating[i] = false;
                    continue;
                }

                if (i == 1 && bastion != null) bastion.SetHealthFraction(health.HealthFraction);
                if (!retreating[i] && health.ShouldRetreat) BeginRetreat(i);
                if (!retreating[i]) continue;

                bool hidden = IsAtRetreatCover(i) &&
                    (!playerVisible[i] || IsHiddenFromPlayer(i));
                health.RecoverWhileHidden(hidden, deltaTime);
                if (!health.ShouldRetreat)
                {
                    retreating[i] = false;
                    ReleaseRetreatCover(i);
                    hasIntel[i] = true;
                    lastIntelTimes[i] = Time.time;
                    searchPasses[i] = 0;
                    postRetreatSearch[i] = true;
                    if (i == 1 && bastion != null) bastion.SetSearchMode(true);
                    if (i != 1)
                    {
                        AgentBrainBase brain = agents[i].GetComponent<AgentBrainBase>();
                        if (brain != null)
                        {
                            if (brain.enabled) brainWasEnabled[i] = true;
                            brain.enabled = false;
                        }
                    }
                    if (i == 1 && bastion != null)
                    {
                        bastion.SetHealthFraction(health.HealthFraction);
                        bastion.Observe(new WorldObservation(lastKnownPositions[i], .35f, Time.timeAsDouble));
                    }
                    var motor = agents[i].GetComponent<WaypointAgentMotor>();
                    if (i != 1 && motor != null && hasIntel[i]) motor.SetDestination(lastKnownPositions[i]);
                }
            }
        }

        private void BeginRetreat(int index)
        {
            retreating[index] = true;
            playerVisible[index] = false;
            tactics[index] = "RETREATING TO COVER";
            if (index == 1) return; // BASTION's behavior tree selects its own protected cover.

            AgentBrainBase brain = agents[index].GetComponent<AgentBrainBase>();
            if (brain != null)
            {
                brainWasEnabled[index] = brain.enabled;
                brain.enabled = false;
            }

            WaypointAgentMotor motor = agents[index].GetComponent<WaypointAgentMotor>();
            if (motor == null) return;
            CoverPoint cover = FindRetreatCover(index);
            if (cover != null)
            {
                retreatCovers[index] = cover;
                cover.Occupied = true;
                motor.SetDestination(cover.transform.position);
                return;
            }

            Vector3 away = agents[index].transform.position - player.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < .01f) away = -agents[index].transform.forward;
            motor.SetDestination(agents[index].transform.position + away.normalized * 7f);
        }

        private CoverPoint FindRetreatCover(int index)
        {
            CoverPoint best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < coverPoints.Length; i++)
            {
                CoverPoint point = coverPoints[i];
                if (point == null || point.Occupied ||
                    (retreating[1] && bastion != null && point == bastion.SelectedCover)) continue;
                Vector3 coverBody = point.transform.position + Vector3.up;
                bool blocksSight = IsPositionOccluded(index, coverBody);
                float distance = Vector3.Distance(agents[index].transform.position, point.transform.position);
                float score = (blocksSight ? 100f : 0f) + point.StructuralSafety * 10f - distance;
                if (score <= bestScore) continue;
                bestScore = score;
                best = point;
            }
            return best;
        }

        private bool IsHiddenFromPlayer(int index)
        {
            return IsPositionOccluded(index, agents[index].transform.position + Vector3.up * 1.5f);
        }

        private bool IsPositionOccluded(int index, Vector3 target)
        {
            Vector3 origin = player.transform.position + Vector3.up;
            Vector3 delta = target - origin;
            float distance = delta.magnitude;
            if (distance < .05f) return false;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, delta / distance, distance,
                         ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(player.transform) ||
                    hit.transform == agents[index].transform ||
                    hit.transform.IsChildOf(agents[index].transform)) continue;
                return true;
            }
            return false;
        }

        private bool IsAtRetreatCover(int index)
        {
            Vector3 coverPosition;
            if (index == 1 && bastion != null && bastion.SelectedCover != null)
                coverPosition = bastion.SelectedCover.transform.position;
            else if (retreatCovers[index] != null)
                coverPosition = retreatCovers[index].transform.position;
            else return false;
            return Vector3.Distance(agents[index].transform.position, coverPosition) <= 1.4f;
        }

        private void ReleaseRetreatCover(int index)
        {
            if (retreatCovers[index] != null)
                retreatCovers[index].Occupied = false;
            retreatCovers[index] = null;
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
            float fieldOfView = postRetreatSearch[index] ? 360f : SightFieldOfView;
            if (distance > SightRanges[index] || distance < .01f ||
                Vector3.Angle(observer.forward, delta) > fieldOfView * .5f) return false;

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
            int ring = pass == 0 ? 0 : (pass - 1) / 4 + 1;
            float radius = Mathf.Min(ring * 3f + 2f, 14f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            motor.SetDestination(center + offset);
        }

        private void RangedAttack(int index, float range, int damage, float cooldown,
            Color color)
        {
            if (agents[index] == null || !agents[index].gameObject.activeInHierarchy ||
                retreating[index] || !playerVisible[index] || Time.time < nextAttack[index]) return;
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
                !bastion.gameObject.activeInHierarchy || retreating[1] || !playerVisible[1] ||
                Time.time < nextAttack[1]) return;
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
                    $"{Vector3.Distance(player.transform.position, agents[i].transform.position):0}m • {agents[i].HitsRemaining}/{agents[i].MaxHits} armor" :
                    "DISABLED";
                GUI.Label(new Rect(left + 12f, 36f + i * 36f, width - 20f, 20f),
                    $"{names[i]}  [{rooms[i]}]  {distance}");
                GUI.Label(new Rect(left + 24f, 54f + i * 36f, width - 30f, 17f),
                    active ? tactics[i] : "NEUTRALIZED");
            }
        }
    }
}
