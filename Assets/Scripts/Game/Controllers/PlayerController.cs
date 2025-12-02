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

    [SerializeField]
    private List<SOWeaponConfigBase> weapons;

    private List<WeaponBase> weaponList;

    void Start()
    {
        LockCursor(true);
        inputSys = this.GetSystem<InputSys>();
        weaponSystem = this.GetSystem<WeaponSystem>();

        //this.RegisterEvent<EventPlayerChangeMoveState>(OnPlayerMoveStateChanged).UnRegisterWhenGameObjectDestroyed(this);

        this.RegisterEvent<EventPlayerChangeWeapon>(OnPlayerChangeWeapon).UnRegisterWhenGameObjectDestroyed(this);

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


        if(Input.GetKeyDown(KeyCode.N))
        {
            List<WeaponBase> weaponBases = new List<WeaponBase>();
            foreach (var weaponBase in weapons)
            {
                weaponBases.Add(GenerateWeapon(weaponBase));
            }
            weaponList = weaponBases;
            weaponSystem.EquipWeapon(weaponBases);
        }

    }





    //private void OnPlayerMoveStateChanged(EventPlayerChangeMoveState e)
    //{
    //    //print(e.CurrentState);
    //    Animator.SetInteger("AnimState", (int)e.CurrentState);
    //}

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

    private WeaponBase GenerateWeapon(SOWeaponConfigBase weaponConfig)
    {

        GameObject weapon = Instantiate(weaponConfig.WeaponPrefab,WeaponRoot);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.name = weaponConfig.WeaponName;
        if(weaponConfig.WeaponType == WeaponType.Firearm)
        {
            FirearmWeapon weaponBase = weapon.GetComponent<FirearmWeapon>();
            weaponBase.Init(weaponConfig);
        }


        return weapon.GetComponent<WeaponBase>();
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
