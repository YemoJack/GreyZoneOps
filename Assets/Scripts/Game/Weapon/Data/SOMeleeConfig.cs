using UnityEngine;

[CreateAssetMenu(fileName = "SOMeleeConfig", menuName = "WeaponConfig/SOMeleeConfig")]
public class SOMeleeConfig : SOWeaponConfigBase
{
    [Header("Melee Attack")]
    [Min(0f)] public float damage = 20f;
    [Min(0.01f)] public float range = 1.8f;
    [Min(0f)] public float radius = 0.2f;
    [Min(0.01f)] public float attackInterval = 0.45f;
    public bool isUnarmedWeapon = false;
}
