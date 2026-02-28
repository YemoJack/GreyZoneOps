using QFramework;

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


public partial class PlayerSystem : AbstractSystem, IUpdateSystem, ICanSendCommand

{
    private WeaponSystem weaponSystem;
    private InputSys inputSys;
    private IGameLoop updateScheduler;

    private EPlayerMoveState moveState = EPlayerMoveState.Idle;
    private bool initialized;

    protected override void OnInit()
    {
        this.RegisterEvent<EventPlayerSpawned>(OnPlayerSpawned);
    }

    public void InitPlayerSystem()
    {
        if (initialized)
        {
            return;
        }

        weaponSystem = this.GetSystem<WeaponSystem>();
        inputSys = this.GetSystem<InputSys>();

        updateScheduler = this.GetUtility<IGameLoop>();
        updateScheduler.Register(this);
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
        if (weaponSystem == null || inputSys == null)
        {
            return;
        }

        var currentWeapon = weaponSystem.GetCurrentWeapon();
        bool hasWeapon = currentWeapon != null && currentWeapon.Config != null;
        if (hasWeapon && currentWeapon.Config.WeaponType == WeaponType.Firearm)
        {
            FirearmWeapon firearmWeapon = currentWeapon as FirearmWeapon;
            if (firearmWeapon != null && inputSys.FireModeSwitchPressed)
            {
                firearmWeapon.SwitchFireMode();
            }

            bool shouldAutoFire = firearmWeapon != null && firearmWeapon.IsAutomatic && inputSys.FireHold;
            bool shouldBurstFire = firearmWeapon != null && firearmWeapon.IsBurstMode && inputSys.FirePressed;
            bool shouldSingleFire = firearmWeapon != null && firearmWeapon.IsSingleMode && inputSys.FirePressed;

            if (shouldAutoFire || shouldBurstFire || shouldSingleFire)
            {
                this.SendCommand<CmdStartAttack>();
            }

            if (inputSys.ReloadPressed)
            {
                this.SendCommand<CmdReloadWeapon>();
            }
        }
        else if (inputSys.FirePressed)
        {
            // Melee weapon or unarmed state (no weapon) both use a unified attack command.
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

        if (inputSys.Jump)
        {
            moveState = EPlayerMoveState.Jump;
        }
        else if (moveMagnitude < 0.05f)
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

    private void OnPlayerSpawned(EventPlayerSpawned e)
    {
        InitPlayerSystem();
    }
}
