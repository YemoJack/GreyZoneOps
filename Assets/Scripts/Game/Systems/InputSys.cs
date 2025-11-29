using UnityEngine;
using QFramework;

public class InputSys : AbstractSystem
{

    // ------- Axis -------
    public Vector3 MoveAxis { get; private set; }
    public Vector3 LookAxis { get; private set; }

    // ------- Buttons -------
    public bool FirePressed { get; private set; }
    public bool FireHold { get; private set; }
    public bool AimHold { get; private set; }

    public bool ReloadPressed { get; private set; }
    public bool Jump { get; private set; }
    public bool Crouch { get; private set; }
    public bool Sprint { get; private set; }



    // 辅助：返回常用 Vector2 用于移动或旋转
    public Vector2 Move2D => new Vector2(MoveAxis.x, MoveAxis.z);
    public Vector2 Look2D => new Vector2(LookAxis.x, LookAxis.y);


    protected override void OnInit() { }

    public void UpdateInput()
    {
        // -------- Movement input (x,z) --------
        MoveAxis = new Vector3(
            Input.GetAxisRaw("Horizontal"),  // X
            0f,                               // Y 一般不用
            Input.GetAxisRaw("Vertical")      // Z
        );

        // -------- Look input (yaw, pitch) --------
        LookAxis = new Vector3(
            Input.GetAxis("Mouse X"),     // yaw
            Input.GetAxis("Mouse Y"),     // pitch
            0f                            // roll - 暂不使用
        );

        // -------- Buttons --------
        FirePressed = Input.GetMouseButtonDown(0);
        FireHold = Input.GetMouseButton(0);

        AimHold = Input.GetMouseButton(1);

        ReloadPressed = Input.GetKeyDown(KeyCode.R);
        Jump = Input.GetKeyDown(KeyCode.Space);
        Sprint = Input.GetKey(KeyCode.LeftShift);
        Crouch = Input.GetKey(KeyCode.LeftControl);
    }
}
