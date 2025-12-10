using System.Threading;
using Cysharp.Threading.Tasks;
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
    public Camera PlayerCamera;

    [Header("Ground")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    private CharacterController _controller;
    private InputSys _inputSys;
    private Transform _viewTransform;
    private WeaponSystem _weaponSystem;

    private float _pitch; // 上下视角
    private float _yaw;   // 左右视角

    private float _speed;
    private const float _threshold = 0.01f;

    private IUnRegister recoilEventUnregister;

    private bool recoilActive;
    private bool playerCounteringRecoil;
    private Vector2 viewBeforeRecoil;
    private float lastRecoilEventTime;

    private float _defaultFov;
    private bool _aimHeld;
    private FirearmWeapon _aimWeapon;
    private CancellationTokenSource _aimCts;

    private void Awake()
    {
        if (PlayerCamera == null && CameraRoot != null)
        {
            PlayerCamera = CameraRoot.GetComponentInChildren<Camera>();
        }
    }

    private void Start()
    {
        _inputSys = this.GetSystem<InputSys>();
        _weaponSystem = this.GetSystem<WeaponSystem>();
        _controller = GetComponent<CharacterController>();

        if (PlayerCamera != null)
        {
            _viewTransform = PlayerCamera.transform;
            _weaponSystem.BindAimProvider(new CameraAimProvider(PlayerCamera));
            _defaultFov = PlayerCamera.fieldOfView;
        }
        else if (CameraRoot != null)
        {
            _viewTransform = CameraRoot;
        }
        else
        {
            Debug.LogWarning("FirstPersonController: 未找到用于移动和瞄准的相机/方向引用");
        }

        if (_defaultFov <= 0f && PlayerCamera != null)
        {
            _defaultFov = PlayerCamera.fieldOfView;
        }

        Vector3 rot = transform.eulerAngles;
        _yaw = rot.y;
        _pitch = 0f;

        // 鼠标锁定
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        recoilEventUnregister = this.RegisterEvent<EventWeaponRecoilApplied>(OnWeaponRecoilApplied);
    }

    private void OnDisable()
    {
        recoilEventUnregister?.UnRegister();
        recoilEventUnregister = null;
        _aimCts?.Cancel();
        _aimCts?.Dispose();
        _aimCts = null;
        if (_aimWeapon != null)
        {
            _aimWeapon.SetAimState(false);
            _aimWeapon = null;
        }
        _aimHeld = false;
    }

    private void Update()
    {
        GroundCheck();
        HandleAimInput();
        Look();
        TrackRecoilCompensation();
        TryRestoreViewAfterFireStops();
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

        ApplyViewRotation();
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

        if (_viewTransform == null)
        {
            return;
        }

        Vector3 camForward = _viewTransform.forward;
        camForward.y = 0f;
        Vector3 camRight = _viewTransform.right;
        camRight.y = 0f;

        Vector3 move = camForward * _inputSys.Move2D.y + camRight * _inputSys.Move2D.x;
        move.Normalize();

        _controller.Move(move * (_speed * Time.deltaTime) + new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
    }

    private void OnWeaponRecoilApplied(EventWeaponRecoilApplied evt)
    {
        if (_weaponSystem == null)
        {
            return;
        }

        var currentWeapon = _weaponSystem.GetCurrentWeapon();
        if (currentWeapon?.Config == null || currentWeapon.Config.WeaponID != evt.WeaponId)
        {
            return;
        }

        if (!recoilActive)
        {
            viewBeforeRecoil = new Vector2(_yaw, _pitch);
            playerCounteringRecoil = false;
            recoilActive = true;
        }

        lastRecoilEventTime = Time.time;
        ApplyRecoilToView(evt.RecoilStep);
    }

    private void ApplyRecoilToView(Vector2 recoilStep)
    {
        _yaw += recoilStep.x;
        _pitch -= recoilStep.y;
        _pitch = Mathf.Clamp(_pitch, PitchClampMin, PitchClampMax);

        ApplyViewRotation();
    }

    private void ApplyViewRotation()
    {
        // 角色身体：只受 yaw 影响
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        // 摄像机：受到 pitch + yaw 影响
        if (CameraRoot != null)
        {
            CameraRoot.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }

    private void HandleAimInput()
    {
        if (PlayerCamera == null || _inputSys == null)
        {
            return;
        }

        var currentWeapon = _weaponSystem?.GetCurrentWeapon() as FirearmWeapon;
        var firearmConfig = currentWeapon?.Config as SOFirearmConfig;
        bool aimHold = _inputSys.AimHold && firearmConfig != null && firearmConfig.zoomFactor > 0f;

        if (currentWeapon != _aimWeapon && _aimWeapon != null)
        {
            _aimWeapon.SetAimState(false);
        }

        if (aimHold == _aimHeld && currentWeapon == _aimWeapon)
        {
            return;
        }

        _aimHeld = aimHold;
        _aimWeapon = currentWeapon;
        StartAimRoutine(aimHold, firearmConfig, currentWeapon).Forget();
    }

    private async UniTaskVoid StartAimRoutine(bool aiming, SOFirearmConfig firearmConfig, FirearmWeapon weapon)
    {
        _aimCts?.Cancel();
        _aimCts?.Dispose();
        _aimCts = new CancellationTokenSource();
        var token = _aimCts.Token;

        float duration = firearmConfig != null ? Mathf.Max(firearmConfig.aimTime, 0.001f) : 0.001f;
        float startFov = PlayerCamera.fieldOfView;
        float defaultFov = _defaultFov > 0f ? _defaultFov : startFov;
        float targetFov = defaultFov;

        if (aiming && firearmConfig != null && firearmConfig.zoomFactor > 0f)
        {
            targetFov = defaultFov / Mathf.Max(firearmConfig.zoomFactor, 0.01f);
        }

        float elapsed = 0f;
        try
        {
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                PlayerCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            PlayerCamera.fieldOfView = targetFov;
            UpdateWeaponAimState(weapon, aiming);
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation when switching aim states or disabling
        }
    }

    private void UpdateWeaponAimState(FirearmWeapon weapon, bool aiming)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.SetAimState(aiming);
    }

    private void TrackRecoilCompensation()
    {
        if (!recoilActive || _inputSys == null)
        {
            return;
        }

        if (_inputSys.Look2D.sqrMagnitude >= _threshold * _threshold)
        {
            playerCounteringRecoil = true;
        }
    }

    private void TryRestoreViewAfterFireStops()
    {
        if (!recoilActive || _inputSys == null)
        {
            return;
        }

        if (_inputSys.FireHold)
        {
            return;
        }

        if (Time.time - lastRecoilEventTime < Time.deltaTime)
        {
            return;
        }

        if (!playerCounteringRecoil)
        {
            _yaw = viewBeforeRecoil.x;
            _pitch = Mathf.Clamp(viewBeforeRecoil.y, PitchClampMin, PitchClampMax);
            ApplyViewRotation();
        }

        recoilActive = false;
        playerCounteringRecoil = false;
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
