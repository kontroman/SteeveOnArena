using UnityEngine;

namespace MineArena.AI
{
    public static class CombatTargeting
    {
        public static Vector3 AimPoint(Transform target)
        {
            var body = target.GetComponent<Collider>();
            if (body == null) body = target.GetComponentInChildren<Collider>();
            return body != null ? body.bounds.center : target.position + Vector3.up;
        }

        public static bool HasLineOfSight(Vector3 origin, Vector3 destination, Transform owner, Transform target)
        {
            Vector3 delta = destination - origin;
            if (delta.sqrMagnitude < 0.0001f) return true;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                var body = hit.transform;
                if (owner != null && (body == owner || body.IsChildOf(owner))) continue;
                if (target != null && (body == target || body.IsChildOf(target))) continue;
                if (body.GetComponentInParent<Projectile>() != null) continue;
                return false;
            }
            return true;
        }
    }
}
