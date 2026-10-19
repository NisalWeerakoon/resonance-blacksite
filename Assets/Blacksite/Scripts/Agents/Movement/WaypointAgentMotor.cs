using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Movement
{
    /// <summary>Shared graph path follower. It owns motion and presentation, never agent decisions.</summary>
    public sealed class WaypointAgentMotor : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private MonoBehaviour graphComponent;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private CapsuleCollider capsuleCollider;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0f)] private float collisionSkin = 0.03f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 360f;
        [SerializeField, Min(0.05f)] private float arrivalDistance = 0.3f;
        [SerializeField, Min(0.1f)] private float repathInterval = 0.5f;
        [SerializeField] private bool faceMovementDirection = true;

        private readonly HashSet<string> blockedPortals = new HashSet<string>();
        private readonly List<string> nodePath = new List<string>();
        private readonly List<Vector3> route = new List<Vector3>();
        private readonly RaycastHit[] collisionHits = new RaycastHit[16];
        private Vector3 destination;
        private bool hasDestination;
        private int waypointIndex;
        private float repathTimer;
        private float verticalVelocity;
        private Vector3 previousForward;
        private IWaypointGraph graph;
        private IWaypointNodeProvider nodeProvider;

        public bool HasDestination => hasDestination;
        public bool IsMoving { get; private set; }
        public bool IsBlocked { get; private set; }
        public bool HasArrived => hasDestination && !IsBlocked && waypointIndex >= route.Count;
        public Vector3 Destination => destination;
        public IReadOnlyList<Vector3> CurrentRoute => route;

        private void Awake()
        {
            if (graphComponent != null)
            {
                graph = graphComponent as IWaypointGraph;
                nodeProvider = graphComponent as IWaypointNodeProvider;
            }
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();
            previousForward = transform.forward;
        }

        private void Update()
        {
            if (hasDestination && !HasArrived && (route.Count == 0 || IsBlocked))
            {
                repathTimer += Time.deltaTime;
                if (repathTimer >= repathInterval) RebuildRoute();
            }

            Vector3 planarVelocity = Vector3.zero;
            if (hasDestination && !IsBlocked && waypointIndex < route.Count)
            {
                Vector3 target = route[waypointIndex];
                Vector3 offset = target - transform.position;
                offset.y = 0f;
                if (offset.magnitude <= arrivalDistance)
                {
                    waypointIndex++;
                    if (waypointIndex >= route.Count) FinishRoute();
                }
                else
                {
                    Vector3 direction = offset.normalized;
                    planarVelocity = direction * moveSpeed;
                    if (faceMovementDirection)
                    {
                        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, facing,
                            turnSpeed * Time.deltaTime);
                    }
                }
            }

            if (characterController != null)
            {
                if (characterController.isGrounded) verticalVelocity = -1f;
                else verticalVelocity -= 9.81f * Time.deltaTime;
                characterController.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            }
            else MoveWithCollision(planarVelocity * Time.deltaTime);

            IsMoving = planarVelocity.sqrMagnitude > 0.01f;
            UpdateAnimator(planarVelocity);
        }

        private void MoveWithCollision(Vector3 displacement)
        {
            if (displacement.sqrMagnitude <= 0.000001f) return;
            if (capsuleCollider == null)
            {
                transform.position += displacement;
                return;
            }

            Vector3 remaining = displacement;
            for (int slide = 0; slide < 2 && remaining.sqrMagnitude > 0.000001f; slide++)
            {
                float distance = remaining.magnitude;
                Vector3 direction = remaining / distance;
                if (!TryGetBlockingHit(direction, distance + collisionSkin, out RaycastHit hit))
                {
                    transform.position += remaining;
                    return;
                }

                float safeDistance = Mathf.Max(0f, hit.distance - collisionSkin);
                Vector3 safeMove = direction * safeDistance;
                transform.position += safeMove;
                remaining -= safeMove;
                remaining = Vector3.ProjectOnPlane(remaining, hit.normal);
            }
        }

        private bool TryGetBlockingHit(Vector3 direction, float distance, out RaycastHit nearestHit)
        {
            Bounds bounds = capsuleCollider.bounds;
            float radius = Mathf.Min(Mathf.Max(bounds.extents.x, bounds.extents.z), bounds.extents.y);
            float segment = Mathf.Max(0f, bounds.extents.y - radius);
            Vector3 center = bounds.center;
            Vector3 bottom = center - Vector3.up * segment;
            Vector3 top = center + Vector3.up * segment;
            int hitCount = Physics.CapsuleCastNonAlloc(bottom, top, radius, direction, collisionHits,
                distance, obstacleLayers, QueryTriggerInteraction.Ignore);

            nearestHit = default;
            float nearestDistance = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = collisionHits[i].collider;
                if (hitCollider == null || hitCollider.transform == transform ||
                    hitCollider.transform.IsChildOf(transform)) continue;
                if (collisionHits[i].distance >= nearestDistance) continue;

                nearestHit = collisionHits[i];
                nearestDistance = collisionHits[i].distance;
                found = true;
            }

            return found;
        }

        public bool SetDestination(Vector3 worldPosition)
        {
            destination = worldPosition;
            hasDestination = true;
            waypointIndex = 0;
            return RebuildRoute();
        }

        /// <summary>Follows a route already selected by an agent brain, such as TRACE's intercept path.</summary>
        public bool SetRoute(IReadOnlyList<Vector3> worldWaypoints)
        {
            route.Clear();
            waypointIndex = 0;
            IsBlocked = false;
            if (worldWaypoints == null || worldWaypoints.Count == 0)
            {
                ClearDestination();
                return false;
            }

            for (int i = 0; i < worldWaypoints.Count; i++)
            {
                Vector3 waypoint = worldWaypoints[i];
                if (route.Count == 0 || Vector3.Distance(route[route.Count - 1], waypoint) > arrivalDistance)
                    route.Add(waypoint);
            }

            if (route.Count == 0)
            {
                ClearDestination();
                return false;
            }

            destination = route[route.Count - 1];
            hasDestination = true;
            while (waypointIndex < route.Count &&
                   Vector3.Distance(transform.position, route[waypointIndex]) <= arrivalDistance)
                waypointIndex++;
            if (waypointIndex >= route.Count) FinishRoute();
            return true;
        }

        public void ClearDestination()
        {
            hasDestination = false;
            IsBlocked = false;
            route.Clear();
            waypointIndex = 0;
            UpdateAnimator(Vector3.zero);
        }

        public void SetPortalBlocked(string portalId, bool blocked)
        {
            if (string.IsNullOrWhiteSpace(portalId)) return;
            if (blocked) blockedPortals.Add(portalId);
            else blockedPortals.Remove(portalId);
            if (hasDestination) RebuildRoute();
            else repathTimer = repathInterval;
        }

        public bool RebuildRoute()
        {
            repathTimer = 0f;
            route.Clear();
            waypointIndex = 0;
            IsBlocked = false;
            if (!hasDestination) return false;
            if (graph == null || nodeProvider == null)
            {
                IsBlocked = true;
                return false;
            }

            string start = nodeProvider.NearestNodeId(transform.position);
            string goal = nodeProvider.NearestNodeId(destination);
            if (start == null || goal == null || !nodeProvider.HasNode(start) || !nodeProvider.HasNode(goal) ||
                !GraphAStar.TryFind(graph, start, goal, blockedPortals, nodePath))
            {
                IsBlocked = true;
                return false;
            }

            for (int i = 1; i < nodePath.Count; i++) route.Add(graph.Position(nodePath[i]));
            if (route.Count == 0 && Vector3.Distance(transform.position, destination) > arrivalDistance)
                route.Add(destination);
            else if (route.Count > 0 && Vector3.Distance(route[route.Count - 1], destination) > arrivalDistance)
                route.Add(destination);
            if (route.Count == 0) FinishRoute();
            return true;
        }

        private void FinishRoute()
        {
            waypointIndex = route.Count;
            if (animator != null) animator.SetTrigger("Arrive");
        }

        private void UpdateAnimator(Vector3 velocity)
        {
            if (animator == null) return;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            animator.SetFloat("Speed", velocity.magnitude);
            animator.SetFloat("MoveX", localVelocity.x);
            animator.SetFloat("MoveY", localVelocity.z);
            animator.SetBool("Moving", IsMoving);
            float yaw = Vector3.SignedAngle(previousForward, transform.forward, Vector3.up) /
                        Mathf.Max(Time.deltaTime, 0.0001f);
            animator.SetFloat("Turn", Mathf.Clamp(yaw / 180f, -1f, 1f));
            previousForward = transform.forward;
        }
    }
}
