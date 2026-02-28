using QFramework;
using UnityEngine;

[DisallowMultipleComponent]
public class VMHandsAnimatorDriver : MonoBehaviour, IController
{
    [Header("References")]
    public Animator HandsAnimator;

    [Header("Pose Mapping")]
    public int DefaultPoseId = 0;
    public int AimPoseId = 1;

    [Header("Action Mapping")]
    public int FireActionId = 1;
    public int ReloadActionId = 2;
    public int DryFireActionId = 3;

    [Header("Move Mapping")]
    public float IdleMoveSpeed = 0f;
    public float WalkMoveSpeed = 0.5f;
    public float RunMoveSpeed = 1f;
    public float AirMoveSpeed = 1f;

    private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");
    private static readonly int IsSprintHash = Animator.StringToHash("isSprint");
    private static readonly int IsGunHash = Animator.StringToHash("isGun");
    private static readonly int MeleeAttackTriggerHash = Animator.StringToHash("meleeAttack");
    private static readonly int PoseIdHash = Animator.StringToHash("poseId");
    private static readonly int ActionIdHash = Animator.StringToHash("actionId");
    private static readonly int ActionTriggerBoolHash = Animator.StringToHash("actionTrigger");

    private IUnRegister moveStateUnregister;
    private IUnRegister aimStateUnregister;
    private IUnRegister weaponChangedUnregister;
    private IUnRegister weaponFiredUnregister;
    private IUnRegister reloadStartedUnregister;
    private IUnRegister dryFireUnregister;
    private IUnRegister meleeAttackUnregister;

    private bool clearActionTriggerNextFrame;

    private void Awake()
    {
        ResolveAnimator();
        ResetAnimatorState();
    }

    private void OnEnable()
    {
        moveStateUnregister = this.RegisterEvent<EventPlayerChangeMoveState>(OnMoveStateChanged);
        aimStateUnregister = this.RegisterEvent<EventFirearmAimChanged>(OnAimStateChanged);
        weaponChangedUnregister = this.RegisterEvent<EventPlayerChangeWeapon>(OnWeaponChanged);
        weaponFiredUnregister = this.RegisterEvent<EventWeaponFired>(OnWeaponFired);
        reloadStartedUnregister = this.RegisterEvent<EventWeaponReloadStarted>(OnWeaponReloadStarted);
        dryFireUnregister = this.RegisterEvent<EventWeaponDryFire>(OnWeaponDryFire);
        meleeAttackUnregister = this.RegisterEvent<EventMeleeAttack>(OnMeleeAttack);
        SyncWeaponIdleState();
    }

    private void OnDisable()
    {
        moveStateUnregister?.UnRegister();
        moveStateUnregister = null;
        aimStateUnregister?.UnRegister();
        aimStateUnregister = null;
        weaponChangedUnregister?.UnRegister();
        weaponChangedUnregister = null;
        weaponFiredUnregister?.UnRegister();
        weaponFiredUnregister = null;
        reloadStartedUnregister?.UnRegister();
        reloadStartedUnregister = null;
        dryFireUnregister?.UnRegister();
        dryFireUnregister = null;
        meleeAttackUnregister?.UnRegister();
        meleeAttackUnregister = null;

        if (HandsAnimator != null)
        {
            HandsAnimator.SetBool(ActionTriggerBoolHash, false);
        }
        clearActionTriggerNextFrame = false;
    }

    private void LateUpdate()
    {
        if (!clearActionTriggerNextFrame || HandsAnimator == null)
        {
            return;
        }

        HandsAnimator.SetBool(ActionTriggerBoolHash, false);
        clearActionTriggerNextFrame = false;
    }

    private void Reset()
    {
        ResolveAnimator();
    }

    private void ResolveAnimator()
    {
        if (HandsAnimator == null)
        {
            HandsAnimator = GetComponent<Animator>();
        }

        if (HandsAnimator == null)
        {
            HandsAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void ResetAnimatorState()
    {
        if (HandsAnimator == null)
        {
            return;
        }

        HandsAnimator.SetFloat(MoveSpeedHash, IdleMoveSpeed);
        HandsAnimator.SetBool(IsSprintHash, false);
        HandsAnimator.SetBool(IsGunHash, false);
        HandsAnimator.SetInteger(PoseIdHash, DefaultPoseId);
        HandsAnimator.SetInteger(ActionIdHash, 0);
        HandsAnimator.SetBool(ActionTriggerBoolHash, false);
    }

    private void OnWeaponChanged(EventPlayerChangeWeapon evt)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        ApplyIsGunByWeapon(evt.WeaponInstance);
    }

    private void SyncWeaponIdleState()
    {
        if (HandsAnimator == null)
        {
            return;
        }

        var weaponSystem = this.GetSystem<WeaponSystem>();
        ApplyIsGunByWeapon(weaponSystem != null ? weaponSystem.GetCurrentWeapon() : null);
    }

    private void ApplyIsGunByWeapon(WeaponBase weapon)
    {
        var isGun = weapon != null
            && weapon.Config != null
            && weapon.Config.WeaponType == WeaponType.Firearm;
        HandsAnimator.SetBool(IsGunHash, isGun);
    }

    private void OnMoveStateChanged(EventPlayerChangeMoveState evt)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        switch (evt.CurrentState)
        {
            case EPlayerMoveState.Run:
                HandsAnimator.SetFloat(MoveSpeedHash, RunMoveSpeed);
                HandsAnimator.SetBool(IsSprintHash, true);
                break;
            case EPlayerMoveState.Walk:
                HandsAnimator.SetFloat(MoveSpeedHash, WalkMoveSpeed);
                HandsAnimator.SetBool(IsSprintHash, false);
                break;
            case EPlayerMoveState.Jump:
            case EPlayerMoveState.Fall:
                HandsAnimator.SetFloat(MoveSpeedHash, AirMoveSpeed);
                HandsAnimator.SetBool(IsSprintHash, false);
                break;
            default:
                HandsAnimator.SetFloat(MoveSpeedHash, IdleMoveSpeed);
                HandsAnimator.SetBool(IsSprintHash, false);
                break;
        }
    }

    private void OnAimStateChanged(EventFirearmAimChanged evt)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        HandsAnimator.SetInteger(PoseIdHash, evt.Aiming ? AimPoseId : DefaultPoseId);
    }

    private void OnWeaponFired(EventWeaponFired evt)
    {
        PlayAction(FireActionId);
    }

    private void OnWeaponReloadStarted(EventWeaponReloadStarted evt)
    {
        PlayAction(ReloadActionId);
    }

    private void OnWeaponDryFire(EventWeaponDryFire evt)
    {
        PlayAction(DryFireActionId);
    }

    private void OnMeleeAttack(EventMeleeAttack evt)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        HandsAnimator.SetTrigger(MeleeAttackTriggerHash);
    }

    private void PlayAction(int actionId)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        HandsAnimator.SetInteger(ActionIdHash, actionId);
        HandsAnimator.SetBool(ActionTriggerBoolHash, true);
        clearActionTriggerNextFrame = true;
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
