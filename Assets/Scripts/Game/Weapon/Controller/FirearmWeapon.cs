using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirearmWeapon :  WeaponBase
{

    public Transform FirePos;


    public override void Init(SOWeaponConfigBase wepConfig)
    {
        base.Init(wepConfig);

        if(wepConfig != null)
            Config = wepConfig as SOFirearmConfig;
        else
        {
            Debug.LogError($"FirearmWeapon WeaponConfig is null");
        }
    }



    public override void OnEquip()
    {
        Debug.Log($"FirearmWeapon {InstanceID} is OnEquip");
    }

    public override void OnUnEquip()
    {
        
    }

    public override void Tick()
    {
        
    }


    public override void TryFire()
    {
        print($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Try Fire");
    }

    public void Reload()
    {
        
    }
}
