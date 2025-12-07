using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;


public struct EventPlayerChangeWeapon
{
    public WeaponInventoryEntry Slot;
    public WeaponBase WeaponInstance;
}


public class WeaponSystem : AbstractSystem
{

    private WeaponInventoryModel weaponInventoryModel;
    private readonly Dictionary<int, WeaponBase> weaponInstances = new Dictionary<int, WeaponBase>();
    private readonly List<WeaponBase> instantiatedWeapons = new List<WeaponBase>();
    private IAimRayProvider aimRayProvider;


    protected override void OnInit()
    {
       weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
    }

    public void InitializeLoadout(Transform weaponRoot, IEnumerable<GameObject> weaponPrefabs)
    {
        if (weaponRoot == null || weaponPrefabs == null)
        {
            Debug.LogWarning("WeaponSystem InitializeLoadout called with null references.");
            return;
        }

        instantiatedWeapons.Clear();

        foreach (var weaponPrefab in weaponPrefabs)
        {
            if (weaponPrefab == null) continue;

            var weaponObj = Object.Instantiate(weaponPrefab, weaponRoot);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;

            var weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                RegisterWeaponInstance(weapon);
                instantiatedWeapons.Add(weapon);
                weapon.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Prefab {weaponPrefab.name} does not contain a WeaponBase component.");
                Object.Destroy(weaponObj);
            }
        }

        EquipInitialWeapon();
    }

    /// <summary>
    /// 注册并关联一把武器实例（基于配置 ID）。
    /// </summary>
    /// <param name="weapon"></param>
    public bool RegisterWeaponInstance(WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("RegisterWeaponInstance: weapon is null");
            return false;
        }

        if (weapon.Config == null)
        {
            Debug.LogWarning($"RegisterWeaponInstance: weapon {weapon.name} 配置为空");
            return false;
        }

        if (!weaponInventoryModel.AddOrActivateSlot(weapon.Config, out var entry))
        {
            return false;
        }

        weaponInstances[entry.WeaponId] = weapon;
        if (!instantiatedWeapons.Contains(weapon))
        {
            instantiatedWeapons.Add(weapon);
        }
        return true;
    }

    public void BindAimProvider(IAimRayProvider provider)
    {
        aimRayProvider = provider;
    }

    public void SwitchWeapon()
    {
        var previous = weaponInventoryModel.CurrentSlot;
        if (weaponInventoryModel.TrySwitchNextAvailable(out var entry))
        {
            HandleSwitch(entry, previous);
        }
    }

    public void StartAttack()
    {
        var currentSlot = weaponInventoryModel.CurrentSlot;
        if (currentSlot != null && weaponInstances.TryGetValue(currentSlot.WeaponId, out var weapon))
        {
            weapon.TryAttack();
        }
    }

    public void ReloadCurrentWeapon()
    {
        var currentSlot = weaponInventoryModel.CurrentSlot;
        if (currentSlot != null && weaponInstances.TryGetValue(currentSlot.WeaponId, out var weapon)
            && weapon is FirearmWeapon firearmWeapon)
        {
            firearmWeapon.Reload();
        }
    }



    public Vector3 GetFireDirection(Transform firePos, float maxRange = 100f)
    {
        if (aimRayProvider == null)
        {
            Debug.LogWarning("WeaponSystem: Aim provider is not bound.");
            return Vector3.zero;
        }

        Ray ray = aimRayProvider.GetAimRay();
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
        {
            targetPoint = hit.point;
        }
        else
        {
            // 射线未命中，取最大射程点
            targetPoint = ray.origin + ray.direction * maxRange;
        }

        // 方向 = 目标点 - 枪口
        Vector3 fireDir = (targetPoint - firePos.position).normalized;
        Vector3 camForward = aimRayProvider.GetAimForward();
        if (Vector3.Dot(fireDir, camForward) <= 0)
        {
            Debug.Log("枪口有阻挡，请和障碍物保持一定距离");
            return Vector3.zero;
        }
        

        return fireDir;
    }

    public bool EquipInitialWeapon()
    {
        if (weaponInventoryModel.CurrentSlot == null)
        {
            Debug.LogWarning("EquipInitialWeapon: 无可用武器");
            return false;
        }

        HandleSwitch(weaponInventoryModel.CurrentSlot, null);
        return true;
    }

    public bool SwitchWeaponByIndex(int index)
    {
        var previous = weaponInventoryModel.CurrentSlot;
        if (weaponInventoryModel.TrySwitchWeapon(index, out var entry))
        {
            HandleSwitch(entry, previous);
            return true;
        }

        return false;
    }

    private void HandleSwitch(WeaponInventoryEntry entry, WeaponInventoryEntry previous)
    {
        if (entry == null)
        {
            return;
        }

        if (!weaponInstances.TryGetValue(entry.WeaponId, out var weapon))
        {
            Debug.LogWarning($"HandleSwitch: 未找到武器实例，ID={entry.WeaponId}");
            return;
        }

        if (previous != null && weaponInstances.TryGetValue(previous.WeaponId, out var previousWeapon)
            && previousWeapon != null && previousWeapon != weapon)
        {
            previousWeapon.gameObject.SetActive(false);
            previousWeapon.OnUnEquip();
        }

        UpdateWeaponActiveState(weapon);
        weapon.OnEquip();
        this.SendEvent(new EventPlayerChangeWeapon
        {
            Slot = entry,
            WeaponInstance = weapon
        });
    }

    private void UpdateWeaponActiveState(WeaponBase activeWeapon)
    {
        foreach (var weapon in instantiatedWeapons)
        {
            if (weapon == null) continue;
            weapon.gameObject.SetActive(weapon == activeWeapon);
        }
    }

}
