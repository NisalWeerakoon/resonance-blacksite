using UnityEngine;

namespace Blacksite.Gameplay
{
    // Fast visible tracer. Damage resolves at fire time, avoiding tiny per-frame
    // collision checks that could miss the character while a bolt floated past.
    public sealed class PulseProjectile : MonoBehaviour
    {
        private LineRenderer line;
        private Material material;
        private float born;
        private const float Lifetime = .13f;

        public static bool FirePlayer(Camera view, Transform shooter, Vector3 muzzle, float range)
        {
            if (view == null) return false;
            Vector3 origin = view.transform.position;
            Vector3 direction = view.transform.forward;
            Vector3 end = origin + direction * range;
            SubmissionAgentHealth target = null;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction, range,
                         ~0, QueryTriggerInteraction.Ignore).OrderByDistance())
            {
                if (hit.transform.IsChildOf(shooter)) continue;
                end = hit.point;
                target = hit.collider.GetComponentInParent<SubmissionAgentHealth>();
                break;
            }
            bool confirmed = target != null && target.gameObject.activeInHierarchy && !target.IsDead;
            if (confirmed) target.TakeHit(end, direction);
            Draw(muzzle, end, Color.cyan, "PLAYER");
            return confirmed;
        }

        public static bool FireEnemy(Vector3 muzzle, Vector3 destination, Transform shooter,
            PlayerVitals player, int damage, Color color, string label)
        {
            if (player == null || player.IsDead) return false;
            Vector3 delta = destination - muzzle;
            float range = delta.magnitude;
            Vector3 end = destination;
            bool blocked = false;
            foreach (RaycastHit hit in Physics.RaycastAll(muzzle, delta.normalized, range,
                         ~0, QueryTriggerInteraction.Ignore).OrderByDistance())
            {
                if (hit.transform.IsChildOf(shooter)) continue;
                if (hit.collider.GetComponentInParent<PlayerVitals>() != null) break;
                // The aim point may be above the capsule. World cover still blocks.
                if (hit.distance < range - .7f)
                {
                    end = hit.point;
                    blocked = true;
                }
                break;
            }
            Draw(muzzle, end, color, label);
            if (blocked) return false;
            player.TakeDamage(damage, label);
            return true;
        }

        private static void Draw(Vector3 from, Vector3 to, Color color, string label)
        {
            var visual = new GameObject(label + "_Shot_Tracer");
            var tracer = visual.AddComponent<PulseProjectile>();
            tracer.line = visual.AddComponent<LineRenderer>();
            tracer.line.useWorldSpace = true;
            tracer.line.positionCount = 2;
            tracer.line.SetPosition(0, from);
            tracer.line.SetPosition(1, to);
            tracer.line.startWidth = .085f;
            tracer.line.endWidth = .025f;
            tracer.line.numCapVertices = 4;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            tracer.material = new Material(shader);
            tracer.material.color = color;
            tracer.line.material = tracer.material;
            tracer.line.startColor = color;
            tracer.line.endColor = color;
            tracer.born = Time.time;
        }

        private void Update()
        {
            float remaining = 1f - (Time.time - born) / Lifetime;
            if (remaining <= 0f) { Destroy(gameObject); return; }
            Color tint = line.startColor;
            tint.a = remaining;
            line.startColor = tint;
            line.endColor = tint;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }

    internal static class RaycastHitSorting
    {
        public static RaycastHit[] OrderByDistance(this RaycastHit[] hits)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            return hits;
        }
    }
}
