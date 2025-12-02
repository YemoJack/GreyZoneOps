using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IWeapon,IController
{
    public int InstanceID { get; set; }

    public SOWeaponConfigBase Config {  get; protected set; }

    public virtual void Init(SOWeaponConfigBase wepConfig)
    {
        InstanceID = gameObject.GetInstanceID();
    }


    protected virtual void Start()
    {
        
    }





    public virtual void OnEquip()
    {
       
    }

    public virtual void OnUnEquip()
    {
        
    }



    /// <summary>
    /// 表现层开火
    /// </summary>
    public virtual void TryAttack()
    {
        
    }




    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
