using QFramework;
using System.Collections.Generic;
using System;
using UnityEngine;


public class WeaponInventoryModel : AbstractModel
{
    public List<WeaponBase> Weapons = new List<WeaponBase>();
    public int CurrentIndex = 0;

    protected override void OnInit() { }

    public WeaponBase CurrentConfig => (Weapons.Count > 0 && CurrentIndex >= 0 && CurrentIndex < Weapons.Count)
        ? Weapons[CurrentIndex] : null;



    public void AddWeapon(WeaponBase weapon)
    {
        Weapons.Add(weapon);
    }


    public void SwitchWeapon(int index)
    {
        if(index < 0 || index >= Weapons.Count)
        {
            return;
        }

        if (Weapons.Count > 1)
        {
            if (CurrentConfig != null)
            {
                CurrentConfig.OnUnEquip();
            }

            CurrentIndex = index;

            CurrentConfig.OnEquip();
            this.SendEvent<EventPlayerChangeWeapon>(new EventPlayerChangeWeapon()
            {
                Weapon = CurrentConfig
            });
        }
        else
        {
            Debug.LogWarning("没有武器多余的武器进行切换");
        }

    }

    public void SwitchWeapon()
    {
        if (Weapons.Count > 1)
        {
            if (CurrentConfig != null)
            {
                CurrentConfig.OnUnEquip();
            }

            CurrentIndex++;

            if(CurrentIndex >= Weapons.Count)
                CurrentIndex = 0;

            CurrentConfig.OnEquip();
            this.SendEvent<EventPlayerChangeWeapon>(new EventPlayerChangeWeapon()
            {
                Weapon = CurrentConfig
            });
        }
        else
        {
            Debug.LogWarning("没有武器多余的武器进行切换");
        }

    }

}
