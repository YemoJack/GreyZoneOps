using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public struct EventAttackInput
{
    
}

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


}
