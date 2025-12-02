using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FirearmWeapon :  WeaponBase
{

    public Transform FirePos;

    private CmdFireamFire cmdFireamFire;


    public override void Init(SOWeaponConfigBase wepConfig)
    {
        base.Init(wepConfig);

        if(wepConfig != null)
            Config = wepConfig as SOFirearmConfig;
        else
        {
            Debug.LogError($"FirearmWeapon WeaponConfig is null");
        }


        cmdFireamFire = new CmdFireamFire(Config as SOFirearmConfig, FirePos.position, FirePos.forward);
    }



    public override void OnEquip()
    {
        Debug.Log($"FirearmWeapon {InstanceID} is OnEquip");
    }

    public override void OnUnEquip()
    {
        
    }

    public override void TryAttack()
    {
        print($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Try Fire");
        cmdFireamFire.startPos = FirePos.position;
        cmdFireamFire.forward = this.GetSystem<WeaponSystem>().GetFireDirection(FirePos);
        if(cmdFireamFire.forward == Vector3.zero)
        {
            return;
        }
        this.SendCommand<CmdFireamFire>(cmdFireamFire);
    }

    public void Reload()
    {
        
    }
}
