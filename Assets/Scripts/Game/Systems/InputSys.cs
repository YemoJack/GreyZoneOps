using UnityEngine;
using QFramework;

public class InputSys : AbstractSystem, IUpdateSystem
{

    private IGameLoop updateScheduler;
    private bool inputEnabled = true;
    private string moveHorizontalAxis = "Horizontal";
    private string moveVerticalAxis = "Vertical";
    private string lookHorizontalAxis = "Mouse X";
    private string lookVerticalAxis = "Mouse Y";
    private int fireButton = 0;
    private int aimButton = 1;
    private int altButton = 2;
    private KeyCode reloadKey = KeyCode.R;
    private KeyCode fireModeSwitchKey = KeyCode.B;
    private KeyCode jumpKey = KeyCode.Space;
    private KeyCode sprintKey = KeyCode.LeftShift;
    private KeyCode crouchKey = KeyCode.LeftControl;
    private KeyCode tabKey = KeyCode.Tab;
    private KeyCode interactKey = KeyCode.E;

    // ------- Axis -------
    public Vector3 MoveAxis { get; private set; }
    public Vector3 LookAxis { get; private set; }

    // ------- Buttons -------
    public bool FirePressed { get; private set; }
    public bool FireHold { get; private set; }
    public bool AimHold { get; private set; }

    public bool Mouse2Pressed { get; private set; }

    public bool FireModeSwitchPressed { get; private set; }

    public bool ReloadPressed { get; private set; }
    public bool Jump { get; private set; }
    public bool Crouch { get; private set; }
    public bool Sprint { get; private set; }

    public bool Tab { get; private set; }
    public bool TabPressed { get; private set; }
    public bool InteractPressed { get; private set; }

    public bool InputEnabled => inputEnabled;

    public void SetInputEnabled(bool enabled)
    {
        if (inputEnabled == enabled)
        {
            return;
        }

        inputEnabled = enabled;
        if (!inputEnabled)
        {
            ClearInput();
        }
    }

    // 辅助：返回常用 Vector2 用于移动或旋转
    public Vector2 Move2D => new Vector2(MoveAxis.x, MoveAxis.z);
    public Vector2 Look2D => new Vector2(LookAxis.x, LookAxis.y);

    protected override void OnInit()
    {
        updateScheduler = this.GetUtility<IGameLoop>();
        updateScheduler.Register(this);
        ApplyGameConfig();
    }

    private void ApplyGameConfig()
    {
        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            return;
        }

        var axis = settings.Config.AxisConfig;
        if (!string.IsNullOrEmpty(axis.MoveHorizontalAxis)) moveHorizontalAxis = axis.MoveHorizontalAxis;
        if (!string.IsNullOrEmpty(axis.MoveVerticalAxis)) moveVerticalAxis = axis.MoveVerticalAxis;
        if (!string.IsNullOrEmpty(axis.LookHorizontalAxis)) lookHorizontalAxis = axis.LookHorizontalAxis;
        if (!string.IsNullOrEmpty(axis.LookVerticalAxis)) lookVerticalAxis = axis.LookVerticalAxis;

        var keys = settings.Config.KeyConfig;
        fireButton = keys.FireButton;
        aimButton = keys.AimButton;
        altButton = keys.AltButton;
        reloadKey = keys.ReloadKey;
        fireModeSwitchKey = keys.FireModeSwitchKey;
        jumpKey = keys.JumpKey;
        sprintKey = keys.SprintKey;
        crouchKey = keys.CrouchKey;
        tabKey = keys.TabKey;
        interactKey = keys.InteractKey;
    }

    public void OnUpdate(float deltaTime)
    {
        if (!inputEnabled)
        {
            ClearInput();
            return;
        }

        // -------- Movement input (x,z) --------
        MoveAxis = new Vector3(
            Input.GetAxisRaw(moveHorizontalAxis),  // X
            0f,                               // Y 一般不用
            Input.GetAxisRaw(moveVerticalAxis)      // Z
        );

        // -------- Look input (yaw, pitch) --------
        LookAxis = new Vector3(
            Input.GetAxis(lookHorizontalAxis),     // yaw
            Input.GetAxis(lookVerticalAxis),     // pitch
            0f                            // roll - 暂不使用
        );

        // -------- Buttons --------
        FirePressed = Input.GetMouseButtonDown(fireButton);
        FireHold = Input.GetMouseButton(fireButton);
        Mouse2Pressed = Input.GetMouseButtonDown(altButton);

        AimHold = Input.GetMouseButton(aimButton);

        ReloadPressed = Input.GetKeyDown(reloadKey);
        FireModeSwitchPressed = Input.GetKeyDown(fireModeSwitchKey);
        Jump = Input.GetKeyDown(jumpKey);
        Sprint = Input.GetKey(sprintKey);
        Crouch = Input.GetKey(crouchKey);

        Tab = Input.GetKey(tabKey);
        TabPressed = Input.GetKeyDown(tabKey);
        InteractPressed = Input.GetKeyDown(interactKey);

        if (TabPressed)
        {
            this.SendEvent(new EventOpenInventory());
        }
    }

    private void ClearInput()
    {
        MoveAxis = Vector3.zero;
        LookAxis = Vector3.zero;

        FirePressed = false;
        FireHold = false;
        AimHold = false;
        Mouse2Pressed = false;
        FireModeSwitchPressed = false;
        ReloadPressed = false;
        Jump = false;
        Crouch = false;
        Sprint = false;
        Tab = false;
        TabPressed = false;
        InteractPressed = false;
    }
}

public struct EventOpenInventory
{
}
