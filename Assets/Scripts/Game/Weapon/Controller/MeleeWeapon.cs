using UnityEngine;
using QFramework;

public class MeleeWeapon : WeaponBase
{
    private float nextAttackTime;
    private SOMeleeConfig meleeConfig;
#if UNITY_EDITOR
    [Header("Debug Visualization")]
    [SerializeField] private bool showAttackRangeInScene = true;
    [SerializeField] private float debugDrawDuration = 0.2f;
    [SerializeField] private Color debugRangeColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color debugHitColor = new Color(1f, 0.2f, 0.2f, 1f);
#endif

    public override void TryAttack()
    {
        if (!TryResolveConfig(out meleeConfig))
        {
            return;
        }

        float attackInterval = Mathf.Max(0.01f, meleeConfig.attackInterval);
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;
        var weaponSystem = this.GetSystem<WeaponSystem>();
        Ray attackRay = weaponSystem != null
            ? weaponSystem.GetFireRay()
            : new Ray(transform.position, transform.forward);
        int hitMask = weaponSystem != null
            ? weaponSystem.GetPlayerDamageHitMaskValue()
            : Physics.DefaultRaycastLayers;

        bool hitApplied = MeleeCombatUtility.TryApplyMeleeDamage(
            attackRay: attackRay,
            range: meleeConfig.range,
            radius: meleeConfig.radius,
            hitMask: hitMask,
            attackerRoot: transform.root,
            damage: meleeConfig.damage,
            appliedHit: out var hit);
#if UNITY_EDITOR
        DrawAttackRangeDebug(
            attackRay: attackRay,
            range: meleeConfig.range,
            radius: meleeConfig.radius,
            didHit: hitApplied,
            hitPoint: hitApplied ? hit.point : Vector3.zero);
#endif
        if (hitApplied)
        {
            OnHitTarget(hit, meleeConfig.damage);
        }

        this.SendEvent(new EventMeleeAttack
        {
            WeaponId = Config != null ? Config.WeaponID : 0,
            WeaponName = Config != null ? Config.WeaponName : "Melee",
            Position = attackRay.origin,
            IsUnarmed = meleeConfig != null && meleeConfig.isUnarmedWeapon
        });
    }

    private bool TryResolveConfig(out SOMeleeConfig config)
    {
        config = Config as SOMeleeConfig;
        if (config != null)
        {
            return true;
        }

        Debug.LogWarning("MeleeWeapon: Config must be SOMeleeConfig.");
        return false;
    }

#if UNITY_EDITOR
    private void DrawAttackRangeDebug(Ray attackRay, float range, float radius, bool didHit, Vector3 hitPoint)
    {
        if (!showAttackRangeInScene)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, debugDrawDuration);
        Vector3 dir = attackRay.direction.sqrMagnitude > 0.0001f ? attackRay.direction.normalized : transform.forward;
        Vector3 origin = attackRay.origin;
        Vector3 end = origin + dir * Mathf.Max(0.01f, range);
        float r = Mathf.Max(0f, radius);

        DrawWireCircle(origin, dir, r, debugRangeColor, duration);
        DrawWireCircle(end, dir, r, debugRangeColor, duration);

        Vector3 right = Vector3.Cross(dir, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(dir, Vector3.right);
        }
        right.Normalize();
        Vector3 up = Vector3.Cross(right, dir).normalized;

        Debug.DrawLine(origin + right * r, end + right * r, debugRangeColor, duration, false);
        Debug.DrawLine(origin - right * r, end - right * r, debugRangeColor, duration, false);
        Debug.DrawLine(origin + up * r, end + up * r, debugRangeColor, duration, false);
        Debug.DrawLine(origin - up * r, end - up * r, debugRangeColor, duration, false);
        Debug.DrawLine(origin, end, debugRangeColor, duration, false);

        if (didHit)
        {
            Debug.DrawLine(hitPoint + Vector3.up * 0.08f, hitPoint - Vector3.up * 0.08f, debugHitColor, duration, false);
            Debug.DrawLine(hitPoint + Vector3.right * 0.08f, hitPoint - Vector3.right * 0.08f, debugHitColor, duration, false);
            Debug.DrawLine(hitPoint + Vector3.forward * 0.08f, hitPoint - Vector3.forward * 0.08f, debugHitColor, duration, false);
        }
    }

    private static void DrawWireCircle(Vector3 center, Vector3 normal, float radius, Color color, float duration)
    {
        if (radius <= 0f)
        {
            return;
        }

        Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
        Vector3 tangent = Vector3.Cross(n, Vector3.up);
        if (tangent.sqrMagnitude < 0.0001f)
        {
            tangent = Vector3.Cross(n, Vector3.right);
        }
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(n, tangent).normalized;

        const int segments = 20;
        float step = 360f / segments;
        Vector3 prev = center + tangent * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;
            Vector3 next = center + (Mathf.Cos(angle) * tangent + Mathf.Sin(angle) * bitangent) * radius;
            Debug.DrawLine(prev, next, color, duration, false);
            prev = next;
        }
    }
#endif
}
