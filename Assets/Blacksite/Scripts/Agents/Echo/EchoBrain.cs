using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// ECHO owner implementation: event perception -> acoustic Dijkstra -> memory/decay
    /// -> Utility scores -> shared GraphAStar -> demo motor.
    /// Production integration should replace the demo motor with the team's Agent Controller.
    /// </summary>
    public sealed class EchoBrain : AgentBrainBase
    {
        [SerializeField] private EchoWorldContext world;
        [SerializeField] private EchoDemoPathFollower demoMotor;
        [SerializeField, Range(0.1f, 1.5f)] private float decisionIntervalSeconds = 0.35f;

        private EchoPerceptionMemory memory = new EchoPerceptionMemory();
        private readonly List<Vector3> path = new List<Vector3>();
        private float sinceTick;
        private float lastReplanTime = -100f;
        private EchoAction action = EchoAction.Patrol;
        private bool needReplan = true;
        private bool hasRememberedTarget;
        private Vector3 rememberedTarget;
        private double rememberedAt;
        private int selectedNoiseId = -1;
        private int patrolIndex;
        private EchoUtilityDecision scores;
        private string lastReason = "Initializing";
        private EchoAcousticResult lastPropagation;
        private float lastConfidence;

        public override AgentKind Kind => AgentKind.Echo;
        public EchoAction CurrentAction => action;
        public EchoUtilityDecision Scores => scores;
        public float HearingConfidence => lastConfidence;
        public float AcousticCost => lastPropagation == null ? float.PositiveInfinity : lastPropagation.Cost;
        public int AcousticNodesExpanded => lastPropagation == null ? 0 : lastPropagation.NodesExpanded;
        public int HeardSoundId => selectedNoiseId;
        public string LastReason => lastReason;
        public int WaypointCount => demoMotor == null ? 0 : demoMotor.WaypointCount;
        public Vector3 RememberedTarget => rememberedTarget;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<EchoWorldContext>();
            if (demoMotor == null) demoMotor = GetComponent<EchoDemoPathFollower>();
        }

        private void OnEnable()
        {
            EchoNoiseBus.NoiseEmitted += OnNoise;
            if (world != null) world.PortalChanged += OnPortalChanged;
        }

        private void OnDisable()
        {
            EchoNoiseBus.NoiseEmitted -= OnNoise;
            if (world != null) world.PortalChanged -= OnPortalChanged;
        }

        private void OnNoise(EchoNoiseEvent noise)
        {
            memory.Remember(noise);
            needReplan = true;
        }

        private void OnPortalChanged(string portalId)
        {
            // A door update may alter hearing AND physical reachability.
            needReplan = true;
            lastReason = "Door changed: " + portalId;
        }

        public override void Observe(WorldObservation observation)
        {
            // Only call this from an actual LOS/sensor source (or the explicitly labeled demo button).
            base.Observe(observation);
            needReplan = true;
        }

        public override void TickDecision(float deltaTime)
        {
            if (world == null || demoMotor == null) return;
            sinceTick += Mathf.Max(0f, deltaTime);
            if (sinceTick < decisionIntervalSeconds && !needReplan) return;
            sinceTick = 0f;
            string room = world.ClosestRoom(transform.position);
            double now = Time.timeAsDouble;
            bool heard = memory.TryBest(world.Acoustic, room, now, out EchoPerceivedNoise cue);
            lastConfidence = heard ? cue.Confidence : 0f;
            lastPropagation = heard ? cue.Propagation : null;

            bool visible = LatestObservation.HasValue &&
                now >= LatestObservation.Value.Time &&
                now - LatestObservation.Value.Time < 1.2;
            float sightConfidence = visible ? LatestObservation.Value.Confidence : 0f;
            float ageKnown = hasRememberedTarget ? (float)(now - rememberedAt) : float.PositiveInfinity;
            float recency = heard ? Mathf.Clamp01(1f - cue.AgeSeconds / 12f) : 0f;
            var inputs = new EchoUtilityInputs(lastConfidence, recency, heard && cue.Propagation.Success,
                hasRememberedTarget, ageKnown, visible, sightConfidence);
            EchoUtilityDecision next = EchoUtilityEvaluator.Evaluate(inputs, action);
            EchoAction newAction = next.Action;
            scores = next;

            Vector3 destination = transform.position;
            bool hasDestination = false;
            if (newAction == EchoAction.ChaseVisible)
            {
                destination = LatestObservation.Value.Position;
                hasDestination = true;
                // A valid observation becomes a memory; do not look at live player position.
                rememberedTarget = destination;
                rememberedAt = now;
                hasRememberedTarget = true;
            }
            else if (newAction == EchoAction.InvestigateSound && heard)
            {
                destination = cue.Noise.SourcePosition;
                hasDestination = true;
                if (selectedNoiseId != cue.Noise.Id)
                {
                    selectedNoiseId = cue.Noise.Id;
                    rememberedTarget = destination;
                    rememberedAt = now;
                    hasRememberedTarget = true;
                    needReplan = true;
                }
            }
            else if (newAction == EchoAction.SearchLastKnown && hasRememberedTarget)
            {
                destination = rememberedTarget;
                hasDestination = true;
            }
            else // patrol using room centers, so the agent doesn't idle forever
            {
                string destinationRoom = patrolIndex % 2 == 0 ? EchoRoomIds.Acoustic : EchoRoomIds.Security;
                destination = world.Spatial.Position(destinationRoom);
                hasDestination = true;
                if (!demoMotor.Moving && !needReplan)
                {
                    patrolIndex++;
                    destinationRoom = patrolIndex % 2 == 0 ? EchoRoomIds.Acoustic : EchoRoomIds.Security;
                    destination = world.Spatial.Position(destinationRoom);
                    needReplan = true;
                }
            }

            bool changedAction = newAction != action;
            action = newAction;
            if (changedAction) needReplan = true;
            if (hasDestination && (needReplan || (!demoMotor.Moving &&
                newAction != EchoAction.SearchLastKnown && Time.time - lastReplanTime > 1.5f)))
            {
                needReplan = false;
                lastReplanTime = Time.time;
                if (world.TrySpatialPath(transform.position, destination, path))
                {
                    // The generated demo follows these waypoints; production uses the shared motor.
                    demoMotor.Follow(path);
                    lastReason = newAction + ": planned " + path.Count + " waypoints";
                }
                else
                {
                    demoMotor.Stop();
                    lastReason = "Unreachable route: stop safely / retry after change";
                }
            }
        }
    }
}
