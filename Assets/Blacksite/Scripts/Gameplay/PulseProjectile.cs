using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class PulseProjectile : MonoBehaviour
    {
        private Transform owner;
        private Vector3 velocity;
        private float expires;
        private int damage;
        private string source;
        private bool playerShot;
        private PlayerVitals playerTarget;
        private System.Action<bool> resolved;

        public static void Launch(Vector3 origin, Vector3 direction, Transform shooter, bool fromPlayer,
            float speed, float range, int hitDamage, Color color, string label,
            System.Action<bool> onResolved = null)
        {
            var bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = label + "_Visible_Bolt";
            Destroy(bolt.GetComponent<Collider>());
            bolt.transform.position = origin;
            bolt.transform.localScale = Vector3.one * (fromPlayer ? .18f : .25f);
            var renderer = bolt.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            renderer.material = new Material(shader);
            renderer.material.color = color;
            var trail = bolt.AddComponent<TrailRenderer>();
            trail.time = .22f;
            trail.startWidth = fromPlayer ? .16f : .23f;
            trail.endWidth = 0f;
            trail.material = renderer.material;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            var projectile = bolt.AddComponent<PulseProjectile>();
            projectile.owner = shooter;
            projectile.velocity = direction.normalized * speed;
            projectile.expires = Time.time + range / speed;
            projectile.damage = hitDamage;
            projectile.source = label;
            projectile.playerShot = fromPlayer;
            if (!fromPlayer) projectile.playerTarget = FindFirstObjectByType<PlayerVitals>();
            projectile.resolved = onResolved;
        }

        private void Update()
        {
            if (!BlacksiteGameFlow.IsPlaying) return;
            if (Time.time >= expires) { Finish(false); return; }
            Vector3 delta = velocity * Time.deltaTime;
            if (!playerShot && playerTarget != null)
            {
                Vector3 center = playerTarget.transform.position + Vector3.up;
                Vector3 closest = transform.position + Vector3.Project(center - transform.position,
                    velocity.normalized);
                float travel = Vector3.Dot(closest - transform.position, velocity.normalized);
                if (travel >= 0f && travel <= delta.magnitude &&
                    Vector3.Distance(closest, center) <= .8f)
                {
                    playerTarget.TakeDamage(damage, source);
                    transform.position = closest;
                    Finish(true);
                    return;
                }
            }
            RaycastHit? nearest = null;
            foreach (RaycastHit hit in Physics.RaycastAll(transform.position, velocity.normalized,
                         delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (owner != null && hit.transform.IsChildOf(owner)) continue;
                if (!playerShot && Vector3.Distance(transform.position, owner.position) < .65f &&
                    hit.collider.GetComponentInParent<PlayerVitals>() == null) continue;
                if (!nearest.HasValue || hit.distance < nearest.Value.distance) nearest = hit;
            }
            if (nearest.HasValue)
            {
                bool hitTarget = false;
                if (playerShot)
                {
                    var target = nearest.Value.collider.GetComponentInParent<SubmissionAgentHealth>();
                    if (target != null) { target.TakeHit(); hitTarget = true; }
                }
                else
                {
                    var target = nearest.Value.collider.GetComponentInParent<PlayerVitals>();
                    if (target != null) { target.TakeDamage(damage, source); hitTarget = true; }
                }
                transform.position = nearest.Value.point;
                if (!playerShot) Debug.Log($"BLACKSITE_BOLT impact={nearest.Value.collider.name} target={hitTarget}");
                Finish(hitTarget);
                return;
            }
            transform.position += delta;
        }

        private void Finish(bool hit)
        {
            resolved?.Invoke(hit);
            Destroy(gameObject);
        }
    }
}
