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
        this.RegisterEvent<EventAttackInput>(OnAttack);
    }

    protected void OnAttack(EventAttackInput e)
    {
        TryFire();
    }



    public virtual void OnEquip()
    {
       
    }

    public virtual void OnUnEquip()
    {
        
    }


    public virtual void Tick()
    {
        
    }

    public virtual void TryFire()
    {
        
    }




    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
