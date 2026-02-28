using System;
using UnityEngine;

public static class MeleeCombatUtility
{
    public static bool TryApplyMeleeDamage(
        Ray attackRay,
        float range,
        float radius,
        int hitMask,
        Transform attackerRoot,
        float damage,
        out RaycastHit appliedHit)
    {
        appliedHit = default;
        float clampedRange = Mathf.Max(0.01f, range);
        float clampedRadius = Mathf.Max(0f, radius);
        float clampedDamage = Mathf.Max(0f, damage);
        if (clampedDamage <= 0f)
        {
            return false;
        }

        var hits = Physics.SphereCastAll(
            attackRay,
            clampedRadius,
            clampedRange,
            hitMask,
            QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            var hitTransform = hit.collider.transform;
            if (attackerRoot != null && hitTransform != null && hitTransform.IsChildOf(attackerRoot))
            {
                continue;
            }

            var healthComponent = hit.collider.GetComponentInParent<HealthComponent>();
            if (healthComponent == null)
            {
                continue;
            }

            healthComponent.ApplyDamage(clampedDamage);
            appliedHit = hit;
            return true;
        }

        return false;
    }
}
