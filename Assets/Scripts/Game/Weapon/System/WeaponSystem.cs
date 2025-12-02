using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using System;


public struct EventPlayerChangeWeapon
{
    public WeaponBase Weapon;
}


public class WeaponSystem : AbstractSystem
{

    private WeaponInventoryModel weaponInventoryModel;


    protected override void OnInit()
    {
       weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
    }

    /// <summary>
    /// 装备武器列表
    /// </summary>
    /// <param name="weapons"></param>
    public void EquipWeapon(List<WeaponBase> weapons)
    {
        if (weapons == null || weapons.Count == 0)
        {
            return;
        }

        foreach (var weapon in weapons)
        {
            weaponInventoryModel.AddWeapon(weapon);
        }
       
        weaponInventoryModel.SwitchWeapon(0);
    }

    public void SwitchWeapon()
    {
        weaponInventoryModel.SwitchWeapon();
    }

    public void StartAttack()
    {
        var weapon = this.GetModel<WeaponInventoryModel>().CurrentWeapon;
        weapon?.TryAttack();
    }



    public Vector3 GetFireDirection(Transform firePos, float maxRange = 100f)
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // 屏幕中心
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
        Vector3 camForward = Camera.main.transform.forward.normalized;
        if (Vector3.Dot(fireDir, camForward) <= 0)
        {
            Debug.Log("枪口有阻挡，请和障碍物保持一定距离");
            return Vector3.zero;
        }
        

        return fireDir;
    }

}
