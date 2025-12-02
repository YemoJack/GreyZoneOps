using UnityEngine;
using QFramework;
using System.Collections;




[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour, IController,ICanSendEvent
{
    [Header("Player")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;

    public EPlayerMoveState CurrentMoveState { get; private set;}

    [Space(10)]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;

    public float ViewingAngleSensitivity = 1f;

    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
    public float CameraRotYOffst = 0.0f;
    public bool LockCameraPosition = false;
    private bool isFreeLook = false;
    private float savedYaw;
    private float savedPitch;

    public float CameraResetSpeed = 6.0f;  // 回正速度
    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _targetRotation = 0.0f;

    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _staticVerticalVelocity;


    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;


    private CharacterController _controller;
    private GameObject _mainCamera;
    private const float _threshold = 0.01f;

    private InputSys _inputSys;
  

    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main.gameObject;

        _staticVerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
    }

    private void Start()
    {
        _inputSys = this.GetSystem<InputSys>();
      
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;


        _controller = GetComponent<CharacterController>();

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;

        CurrentMoveState = EPlayerMoveState.Idle;
    }

    private void Update()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }


    private void GroundedCheck()
    {
        if (CurrentMoveState == EPlayerMoveState.Jump)
            return;

        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {

        //----- 按住 Alt：启动自由视角 -----
        if (_inputSys.LeftAltHold)
        {
            if (!isFreeLook)
            {
                isFreeLook = true;
                savedYaw = _cinemachineTargetYaw;
                savedPitch = _cinemachineTargetPitch;
            }

            FreeLookCameraRotate();
        }
        //----- 松开 Alt：自动回到保存位置 -----
        else
        {
            if (isFreeLook)
            {
                isFreeLook = false;
                _cinemachineTargetYaw = savedYaw;
                _cinemachineTargetPitch = savedPitch;
            }

            NormalCameraRotate();
        }


        float deltaTimeMultiplier = Time.deltaTime;

        // 限制 pitch yaw
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
            _cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw,
            0.0f);


    }

  
    private void NormalCameraRotate()
    {
        if (_inputSys.Look2D.sqrMagnitude >= _threshold)
        {
            float deltaTimeMultiplier = 1f;
            _cinemachineTargetYaw += _inputSys.Look2D.x * deltaTimeMultiplier * ViewingAngleSensitivity;
            _cinemachineTargetPitch -= _inputSys.Look2D.y * deltaTimeMultiplier * ViewingAngleSensitivity;
        }
    }

    private void FreeLookCameraRotate()
    {
        if (_inputSys.Look2D.sqrMagnitude >= _threshold)
        {
            float deltaTimeMultiplier = 1f;

            // 这两个值只影响 CinemachineCameraTarget
            _cinemachineTargetYaw += _inputSys.Look2D.x * deltaTimeMultiplier * ViewingAngleSensitivity;
            _cinemachineTargetPitch -= _inputSys.Look2D.y * deltaTimeMultiplier * ViewingAngleSensitivity;
        }
    }

    private void Move()
    {
        float targetSpeed = _inputSys.Sprint ? SprintSpeed : MoveSpeed;
        if (_inputSys.Move2D == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = _inputSys.Move2D.magnitude;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }


        Vector3 inputDirection = new Vector3(_inputSys.Move2D.x, 0f, _inputSys.Move2D.y).normalized;

        if (_inputSys.Move2D != Vector2.zero && !_inputSys.LeftAltHold)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);


        // 只有在没有移动输入 & 非自由视角时，自动对准摄像机方向
        if (!_inputSys.LeftAltHold )
        {
            Vector3 cameraForward = _mainCamera.transform.forward;
            cameraForward = Quaternion.Euler(0f, CameraRotYOffst, 0f) * cameraForward;
            cameraForward.y = 0f;

            transform.rotation = Quaternion.LookRotation(cameraForward);
        }


        if (!Grounded) return;

        if (_inputSys.Move2D == Vector2.zero)
        {
            SetMoveState(EPlayerMoveState.Idle);
        }
        else
        {
            // 小于跑步速度认为 Walk
            if (_inputSys.Sprint)
                SetMoveState(EPlayerMoveState.Run);
            else
                SetMoveState(EPlayerMoveState.Walk);
        }

    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // 着地时落地状态
            if (CurrentMoveState == EPlayerMoveState.Fall)
                SetMoveState(EPlayerMoveState.Idle);

            _fallTimeoutDelta = FallTimeout;

            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // Jump
            if (_inputSys.Jump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = _staticVerticalVelocity;
                SetMoveState(EPlayerMoveState.Jump);
                Grounded = false;
            }

            if (_jumpTimeoutDelta >= 0f)
                _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            // 空中时切换为 Fall
            if (_fallTimeoutDelta >= 0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                SetMoveState(EPlayerMoveState.Fall);
            }
        }

        if (_verticalVelocity < _terminalVelocity)
            _verticalVelocity += Gravity * Time.deltaTime;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0f, 1f, 0f, 0.35f);
        Color transparentRed = new Color(1f, 0f, 0f, 0.35f);

        Gizmos.color = Grounded ? transparentGreen : transparentRed;

        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }


    /// <summary>
    /// 设置移动状态
    /// </summary>
    /// <param name="newState"></param>
    private void SetMoveState(EPlayerMoveState newState)
    {
        if (CurrentMoveState == newState) return;

        var evt = new EventPlayerChangeMoveState()
        {
            PreviousState = CurrentMoveState,
            CurrentState = newState
        };
        CurrentMoveState = newState;

        this.SendEvent(evt);  // 发送事件
    }



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}