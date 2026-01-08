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

    }

    public void InitPlayerSystem()
    {
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
        var currentWeapon = weaponSystem.GetCurrentWeapon() as FirearmWeapon;

        if (currentWeapon != null && inputSys.FireModeSwitchPressed)
        {
            currentWeapon.SwitchFireMode();
        }

        bool shouldAutoFire = currentWeapon != null && currentWeapon.IsAutomatic && inputSys.FireHold;
        bool shouldBurstFire = currentWeapon != null && currentWeapon.IsBurstMode && inputSys.FirePressed;
        bool shouldSingleFire = currentWeapon != null && currentWeapon.IsSingleMode && inputSys.FirePressed;

        if (shouldAutoFire || shouldBurstFire || shouldSingleFire)
        {
            this.SendCommand<CmdStartAttack>();
        }

        if (inputSys.ReloadPressed)
        {
            this.SendCommand<CmdReloadWeapon>();
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
}
