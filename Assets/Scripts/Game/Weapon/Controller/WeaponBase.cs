using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IWeapon,IController,ICanSendEvent
{

    public int InstanceID { get; protected set; }



    public SOWeaponConfigBase Config;




    protected virtual void Start()
    {
        InstanceID = gameObject.GetInstanceID();
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

    public virtual void OnHitTarget(RaycastHit hit,System.Object param = null)
    {

    }


    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
