using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using System;


public enum PlayerAnimState
{
    Idle,
    Walk,
    Run,
}
public struct EventPlayerChangeMoveState
{
    public EPlayerMoveState PreviousState;
    public EPlayerMoveState CurrentState;
}


public enum EPlayerMoveState
{
    Idle = 0,
    Walk = 1,
    Run = 2,
    Jump = 3,
    Fall = 4,
}





public class PlayerController : MonoBehaviour,IController
{

    //public Animator Animator;

    private InputSys inputSys;

    private WeaponSystem weaponSystem;


    public Transform WeaponRoot;

    public List<GameObject> weaponObjectList;
    private List<WeaponBase> weaponList = new List<WeaponBase>();

    void Start()
    {
        LockCursor(true);
        inputSys = this.GetSystem<InputSys>();
        weaponSystem = this.GetSystem<WeaponSystem>();

      
        this.RegisterEvent<EventPlayerChangeWeapon>(OnPlayerChangeWeapon).UnRegisterWhenGameObjectDestroyed(this);


        foreach (var weapon in weaponObjectList)
        {
            GameObject wep = Instantiate(weapon,WeaponRoot);
            wep.transform.localPosition = Vector3.zero;
            wep.transform.localRotation = Quaternion.identity;
            weaponList.Add(wep.GetComponent<WeaponBase>());
        }
        weaponSystem.EquipWeapon(weaponList);
    }

   
    private void Update()
    {
        if (inputSys.FirePressed)
        {
            this.SendCommand<CmdStartAttack>();
        }

        if(inputSys.Mouse2Pressed)
        {
            weaponSystem.SwitchWeapon();
        }

    }



    private void OnPlayerChangeWeapon(EventPlayerChangeWeapon weapon)
    {
        foreach (var wep in weaponList)
        {
            wep.gameObject.SetActive(false);
            if(wep.InstanceID == weapon.Weapon.InstanceID)
            {
                wep.gameObject.SetActive(true);
            }
        }
    }



    public void LockCursor(bool isLocked)
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
