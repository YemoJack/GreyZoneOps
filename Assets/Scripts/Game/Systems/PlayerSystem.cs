using QFramework;
using System.Collections.Generic;
using UnityEngine;

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

public partial class PlayerSystem : AbstractSystem, IUpdateSystem
{
    private readonly List<WeaponBase> weaponInstances = new List<WeaponBase>();

    private WeaponSystem weaponSystem;
    private WeaponInventoryModel weaponInventoryModel;
    private InputSys inputSys;
    private SystemUpdateScheduler updateScheduler;

    private EPlayerMoveState moveState = EPlayerMoveState.Idle;
    private bool initialized;

    protected override void OnInit()
    {
        weaponSystem = this.GetSystem<WeaponSystem>();
        weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
        inputSys = this.GetSystem<InputSys>();

        updateScheduler = this.GetUtility<SystemUpdateScheduler>();
        updateScheduler.Register(this);

        this.RegisterEvent<EventPlayerChangeWeapon>(OnPlayerChangeWeapon);
    }

    public void InitializeLoadout(Transform weaponRoot, IEnumerable<GameObject> weaponPrefabs)
    {
        if (weaponRoot == null || weaponPrefabs == null)
        {
            Debug.LogWarning("PlayerSystem InitializeLoadout called with null references.");
            return;
        }

        weaponInstances.Clear();
        foreach (var weaponPrefab in weaponPrefabs)
        {
            if (weaponPrefab == null) continue;
            var weaponObj = Object.Instantiate(weaponPrefab, weaponRoot);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;

            var weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                weaponInstances.Add(weapon);
            }
            else
            {
                Debug.LogWarning($"Prefab {weaponPrefab.name} does not contain a WeaponBase component.");
                Object.Destroy(weaponObj);
            }
        }

        weaponSystem.EquipWeapon(weaponInstances);
        initialized = true;
    }

    public void OnUpdate(float deltaTime)
    {
        if (!initialized)
        {
            return;
        }

        HandleWeaponInput();
        UpdateMoveState();
    }

    private void HandleWeaponInput()
    {
        if (inputSys.FirePressed)
        {
            this.SendCommand<CmdStartAttack>();
        }

        if (inputSys.Mouse2Pressed)
        {
            weaponSystem.SwitchWeapon();
        }
    }

    private void UpdateMoveState()
    {
        var previous = moveState;
        var moveMagnitude = inputSys.Move2D.magnitude;

        if (moveMagnitude < 0.05f)
        {
            moveState = EPlayerMoveState.Idle;
        }
        else if (inputSys.Sprint)
        {
            moveState = EPlayerMoveState.Run;
        }
        else
        {
            moveState = EPlayerMoveState.Walk;
        }

        if (previous != moveState)
        {
            this.SendEvent(new EventPlayerChangeMoveState
            {
                PreviousState = previous,
                CurrentState = moveState
            });
        }
    }

    private void OnPlayerChangeWeapon(EventPlayerChangeWeapon evt)
    {
        foreach (var weapon in weaponInstances)
        {
            if (weapon == null) continue;
            weapon.gameObject.SetActive(weapon.InstanceID == evt.Weapon.InstanceID);
        }
    }
}
