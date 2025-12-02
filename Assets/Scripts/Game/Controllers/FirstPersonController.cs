using UnityEngine;
using QFramework;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour, IController, ICanSendEvent
{
    [Header("Movement")]
    public float MoveSpeed = 4f;
    public float SprintSpeed = 7f;
    public float SpeedChangeRate = 10.0f;

    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;
    private float _verticalVelocity;

    [Header("Look")]
    public float MouseSensitivity = 1.2f;
    public float PitchClampMin = -75f;
    public float PitchClampMax = 85f;

    public Transform CameraRoot;

    [Header("Ground")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    private CharacterController _controller;
    private InputSys _inputSys;
    private GameObject _mainCamera;

    private float _pitch; // 上下视角
    private float _yaw;   // 左右视角

    private float _speed;
    private const float _threshold = 0.01f;

    private void Awake()
    {
        _mainCamera = Camera.main.gameObject;
    }

    private void Start()
    {
        _inputSys = this.GetSystem<InputSys>();
        _controller = GetComponent<CharacterController>();

        Vector3 rot = transform.eulerAngles;
        _yaw = rot.y;
        _pitch = 0f;

        // 鼠标锁定
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        GroundCheck();
        Look();
        Move();
        JumpAndGravity();
    }

    // -------------------------
    // FPS Look（摄像机旋转）
    // -------------------------
    private void Look()
    {
        Vector2 look = _inputSys.Look2D;

        if (look.sqrMagnitude < _threshold)
            return;

        float delta = Time.deltaTime;

        _yaw += look.x * MouseSensitivity;
        _pitch -= look.y * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, PitchClampMin, PitchClampMax);

        // 角色身体：只受 yaw 影响
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        // 摄像机：受到 pitch + yaw 影响
        CameraRoot.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void GroundCheck()
    {
        Vector3 pos = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(pos, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    // -------------------------
    // FPS 移动
    // -------------------------
    private void Move()
    {
        float targetSpeed = _inputSys.Sprint ? SprintSpeed : MoveSpeed;
        if (_inputSys.Move2D == Vector2.zero) targetSpeed = 0.0f;

        float currentSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

        _speed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * SpeedChangeRate);

        Vector3 camForward = _mainCamera.transform.forward;
        camForward.y = 0f;
        Vector3 camRight = _mainCamera.transform.right;
        camRight.y = 0f;

        Vector3 move = camForward * _inputSys.Move2D.y + camRight * _inputSys.Move2D.x;
        move.Normalize();

        _controller.Move(move * (_speed * Time.deltaTime) + new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
    }

    // -------------------------
    // Jump / Gravity
    // -------------------------
    private void JumpAndGravity()
    {
        if (Grounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            if (_inputSys.Jump)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
        }
        else
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
}
