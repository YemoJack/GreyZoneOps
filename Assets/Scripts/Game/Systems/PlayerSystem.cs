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


public partial class PlayerSystem : AbstractSystem, IUpdateSystem,ICanSendCommand

{
    private WeaponSystem weaponSystem;
    private InputSys inputSys;
    private SystemUpdateScheduler updateScheduler;

    private EPlayerMoveState moveState = EPlayerMoveState.Idle;
    private bool initialized;

    protected override void OnInit()
    {
        weaponSystem = this.GetSystem<WeaponSystem>();
        inputSys = this.GetSystem<InputSys>();

        updateScheduler = this.GetUtility<SystemUpdateScheduler>();
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
}
