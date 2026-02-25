using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using QFramework;
using System;
using Unity.VisualScripting.Antlr3.Runtime;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour, IController, ICanSendEvent
{
    [Header("Movement")]
    public float MoveSpeed = 4f;
    public float SprintSpeed = 7f;
    public float SpeedChangeRate = 10.0f;
    private float _moveSpeed;
    private float _sprintSpeed;

    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;
    private float _verticalVelocity;

    [Header("Look")]
    public float MouseSensitivity = 1.2f;
    public float PitchClampMin = -75f;
    public float PitchClampMax = 85f;
    [Header("Recoil")]
    public float RecoilRaiseSpeed = 900f;
    public float RecoilReturnSpeed = 360f;
    private float _globalRecoilRaiseSpeed;
    private float _globalRecoilReturnSpeed;

    public Transform CameraRoot;
    public Transform CameraYawPivot;
    public Transform CameraPitchPivot;
    public Camera PlayerCamera;
    public Camera ViewModelCamera;

    [Header("Ground")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    private CharacterController _controller;
    private InputSys _inputSys;
    private Transform _viewTransform;
    private WeaponSystem _weaponSystem;
    private bool runtimeInitialized;

    private float _pitch; // 上下视角
    private float _yaw;   // 左右视角

    private float _speed;
    private const float _threshold = 0.01f;

    private IUnRegister recoilEventUnregister;

    private bool recoilActive;
    private bool playerCounteringRecoil;
    private Vector2 viewBeforeRecoil;
    private float viewBeforeRecoilDuration;
    private Vector2 _recoilTarget;
    private Vector2 _recoilCurrent;
    private IUnRegister recoilRecoverUnregister;

    private float _defaultFov;
    private CancellationTokenSource _aimCts;
    private IUnRegister aimEventUnregister;

    private IUnRegister weaponChangedUnregister;

    private void Awake()
    {
        ResolveViewHierarchyReferences();
    }

    private void Start()
    {
        this.RegisterEvent<EventPlayerSpawned>(OnPlayerSpawned).UnRegisterWhenGameObjectDestroyed(this);
    }


    private void OnPlayerSpawned(EventPlayerSpawned e)
    {
        if (!IsTargetPlayer(e.PlayerTransform))
        {
            return;
        }

        InitializeRuntime();
    }

    private void OnEnable()
    {
        weaponChangedUnregister = this.RegisterEvent<EventPlayerChangeWeapon>(OnWeaponChanged);
        recoilEventUnregister = this.RegisterEvent<EventWeaponRecoilApplied>(OnWeaponRecoilApplied);
        recoilRecoverUnregister = this.RegisterEvent<EventFireRecoilRecover>(OnRestoreViewAfterFireStops);
        aimEventUnregister = this.RegisterEvent<EventFirearmAimChanged>(OnAimStateChanged);
    }

    private void ApplyGameConfig()
    {
        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            return;
        }

        MoveSpeed = settings.Config.MoveSpeed;
        SprintSpeed = settings.Config.SprintSpeed;
        SpeedChangeRate = settings.Config.SpeedChangeRate;
        JumpHeight = settings.Config.JumpHeight;
        Gravity = settings.Config.Gravity;
        GroundedOffset = settings.Config.GroundedOffset;
        GroundedRadius = settings.Config.GroundedRadius;

        MouseSensitivity = settings.Config.MouseSensitivity;
        PitchClampMin = settings.Config.PitchClampMin;
        PitchClampMax = settings.Config.PitchClampMax;
        _globalRecoilRaiseSpeed = settings.Config.RecoilRaiseSpeed;
        _globalRecoilReturnSpeed = settings.Config.RecoilReturnSpeed;
        RecoilRaiseSpeed = _globalRecoilRaiseSpeed;
        RecoilReturnSpeed = _globalRecoilReturnSpeed;
    }



    private void OnDisable()
    {
        weaponChangedUnregister?.UnRegister();
        weaponChangedUnregister = null;
        recoilEventUnregister?.UnRegister();
        recoilEventUnregister = null;
        recoilRecoverUnregister?.UnRegister();
        recoilRecoverUnregister = null;
        aimEventUnregister?.UnRegister();
        aimEventUnregister = null;
        _aimCts?.Cancel();
        _aimCts?.Dispose();
        _aimCts = null;
        if (PlayerCamera != null && _defaultFov > 0f)
        {
            PlayerCamera.fieldOfView = _defaultFov;
        }
    }

    private void Update()
    {
        if (_inputSys == null)
        {
            InitializeRuntime();
            if (_inputSys == null)
            {
                return;
            }
        }

        GroundCheck();
        Look();
        TrackRecoilCompensation();
        UpdateRecoilSmoothing();
        ApplyViewRotation();

        Move();
        JumpAndGravity();
    }

    private void InitializeRuntime()
    {
        if (runtimeInitialized)
        {
            return;
        }

        ResolveViewHierarchyReferences();

        _inputSys = this.GetSystem<InputSys>();
        if (_inputSys == null)
        {
            return;
        }

        _globalRecoilRaiseSpeed = RecoilRaiseSpeed;
        _globalRecoilReturnSpeed = RecoilReturnSpeed;
        ApplyGameConfig();
        _weaponSystem = this.GetSystem<WeaponSystem>();
        _controller = GetComponent<CharacterController>();

        if (PlayerCamera != null)
        {
            _viewTransform = PlayerCamera.transform;
            _weaponSystem?.BindAimProvider(new CameraAimProvider(PlayerCamera));
            _defaultFov = PlayerCamera.fieldOfView;
        }
        else if (CameraPitchPivot != null)
        {
            _viewTransform = CameraPitchPivot;
        }
        else if (CameraRoot != null)
        {
            _viewTransform = CameraRoot;
        }
        else
        {
            Debug.LogWarning("FirstPersonController: 未找到用于移动和瞄准的相机方向引用");
        }

        if (_defaultFov <= 0f && PlayerCamera != null)
        {
            _defaultFov = PlayerCamera.fieldOfView;
        }

        Vector3 rot = transform.eulerAngles;
        _yaw = rot.y;
        _pitch = 0f;

        _moveSpeed = MoveSpeed;
        _sprintSpeed = SprintSpeed;
        if (ShouldLockCursorOnInit())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        runtimeInitialized = true;
    }

    private bool IsTargetPlayer(Transform playerRoot)
    {
        if (playerRoot == null)
        {
            return false;
        }

        return playerRoot == transform || playerRoot == transform.root;
    }

    private bool ShouldLockCursorOnInit()
    {
        var flowSystem = this.GetSystem<GameFlowSystem>();
        if (flowSystem == null)
        {
            return true;
        }

        return flowSystem.CurrentState == GameFlowState.InRaid;
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
        float targetSpeed = _inputSys.Sprint ? _sprintSpeed : _moveSpeed;
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
            viewBeforeRecoilDuration = (currentWeapon.Config as SOFirearmConfig).recoilDuration;
            playerCounteringRecoil = false;
            recoilActive = true;
        }

        ApplyWeaponRecoilConfig(currentWeapon.Config as SOFirearmConfig);
        ApplyRecoilToView(evt.RecoilStep);
    }

    private void ApplyRecoilToView(Vector2 recoilStep)
    {
        _recoilTarget += new Vector2(recoilStep.x, -recoilStep.y);
    }

    private void ApplyViewRotation()
    {
        float finalYaw = _yaw + _recoilCurrent.x;
        float finalPitch = Mathf.Clamp(_pitch + _recoilCurrent.y, PitchClampMin, PitchClampMax);

        // 角色身体：只受 yaw 影响
        transform.rotation = Quaternion.Euler(0f, finalYaw, 0f);

        // 摄像机：受到 pitch + yaw 影响
        var pitchPivot = CameraPitchPivot != null ? CameraPitchPivot : CameraRoot;
        if (pitchPivot != null)
        {
            pitchPivot.localRotation = Quaternion.Euler(finalPitch, 0f, 0f);
        }
    }

    private void ResolveViewHierarchyReferences()
    {
        if (CameraRoot == null)
        {
            var foundCameraRoot = transform.Find("CameraRoot");
            if (foundCameraRoot != null)
            {
                CameraRoot = foundCameraRoot;
            }
        }

        if (CameraYawPivot == null && CameraRoot != null)
        {
            var yawPivot = CameraRoot.Find("CameraYawPivot");
            if (yawPivot != null)
            {
                CameraYawPivot = yawPivot;
            }
        }

        if (CameraPitchPivot == null)
        {
            if (CameraYawPivot != null)
            {
                var pitchPivot = CameraYawPivot.Find("CameraPitchPivot");
                if (pitchPivot != null)
                {
                    CameraPitchPivot = pitchPivot;
                }
            }

            if (CameraPitchPivot == null && CameraRoot != null)
            {
                var pitchPivot = CameraRoot.GetChild("CameraPitchPivot");
                if (pitchPivot != null)
                {
                    CameraPitchPivot = pitchPivot;
                }
            }
        }

        if (PlayerCamera == null)
        {
            PlayerCamera = FindCameraByName("MainCamera");
            if (PlayerCamera == null)
            {
                PlayerCamera = FindBestPlayerCamera();
            }
        }

        if (ViewModelCamera == null)
        {
            ViewModelCamera = FindCameraByName("ViewModelCamera");
        }
    }

    private Camera FindCameraByName(string cameraName)
    {
        if (CameraRoot == null || string.IsNullOrEmpty(cameraName))
        {
            return null;
        }

        var cameras = CameraRoot.GetComponentsInChildren<Camera>(true);
        foreach (var cam in cameras)
        {
            if (cam != null && cam.name == cameraName)
            {
                return cam;
            }
        }

        return null;
    }

    private Camera FindBestPlayerCamera()
    {
        if (CameraRoot == null)
        {
            return null;
        }

        var cameras = CameraRoot.GetComponentsInChildren<Camera>(true);
        if (cameras == null || cameras.Length == 0)
        {
            return null;
        }

        Camera firstCamera = null;
        Camera taggedMainCamera = null;
        Camera nonViewModelCamera = null;
        var viewModelLayer = LayerMask.NameToLayer("ViewModel");

        foreach (var cam in cameras)
        {
            if (cam == null)
            {
                continue;
            }

            if (firstCamera == null)
            {
                firstCamera = cam;
            }

            var isViewModel = viewModelLayer >= 0 && cam.gameObject.layer == viewModelLayer;
            if (!isViewModel && nonViewModelCamera == null)
            {
                nonViewModelCamera = cam;
            }

            if (cam.CompareTag("MainCamera"))
            {
                if (!isViewModel)
                {
                    return cam;
                }

                if (taggedMainCamera == null)
                {
                    taggedMainCamera = cam;
                }
            }
        }

        return nonViewModelCamera ?? taggedMainCamera ?? firstCamera;
    }

    private void OnAimStateChanged(EventFirearmAimChanged evt)
    {
        if (PlayerCamera == null)
        {
            return;
        }

        _aimCts?.Cancel();
        _aimCts?.Dispose();
        _aimCts = new CancellationTokenSource();
        var token = _aimCts.Token;

        float duration = Mathf.Max(evt.Duration, 0.001f);
        float startFov = PlayerCamera.fieldOfView;
        float defaultFov = _defaultFov > 0f ? _defaultFov : startFov;
        float targetFov = defaultFov;

        if (evt.Aiming && evt.ZoomFactor > 0f)
        {
            targetFov = defaultFov / Mathf.Max(evt.ZoomFactor, 0.01f);
            _moveSpeed = MoveSpeed * (evt.Weapon.Config as SOFirearmConfig).aimMoveSpeedMultiplier;
            _sprintSpeed = _moveSpeed;
        }
        else if (!evt.Aiming)
        {
            _moveSpeed = evt.Weapon.Config.moveSpeedMultiplier * MoveSpeed;
            _sprintSpeed = evt.Weapon.Config.runSpeedMultiplier * SprintSpeed;
        }

        AimStateChange(duration, startFov, targetFov, token).Forget();
    }

    private async UniTask AimStateChange(float duration, float startFov, float targetFov, CancellationToken token)
    {
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
        }
        catch (Exception e) when (e is OperationCanceledException)
        {
            //Debug.Log(e);
            // ignore cancellation when switching aim states or disabling
        }
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

    private void OnRestoreViewAfterFireStops(EventFireRecoilRecover evt)
    {
        if (!recoilActive || _inputSys == null)
        {
            return;
        }

        if (!playerCounteringRecoil)
        {
            RestoreViewAfterFireStops().Forget();
        }
        else
        {
            BakeRecoilIntoView();
        }

        recoilActive = false;
        playerCounteringRecoil = false;
    }

    private void UpdateRecoilSmoothing()
    {
        if (RecoilRaiseSpeed <= 0f || RecoilReturnSpeed <= 0f)
        {
            _recoilCurrent = _recoilTarget;
            return;
        }

        float maxDeltaX = GetRecoilSpeed(_recoilCurrent.x, _recoilTarget.x) * Time.deltaTime;
        float maxDeltaY = GetRecoilSpeed(_recoilCurrent.y, _recoilTarget.y) * Time.deltaTime;
        _recoilCurrent.x = Mathf.MoveTowards(_recoilCurrent.x, _recoilTarget.x, maxDeltaX);
        _recoilCurrent.y = Mathf.MoveTowards(_recoilCurrent.y, _recoilTarget.y, maxDeltaY);
    }

    private float GetRecoilSpeed(float current, float target)
    {
        float currentAbs = Mathf.Abs(current);
        float targetAbs = Mathf.Abs(target);
        return targetAbs > currentAbs ? RecoilRaiseSpeed : RecoilReturnSpeed;
    }

    private void BakeRecoilIntoView()
    {
        _yaw += _recoilCurrent.x;
        _pitch = Mathf.Clamp(_pitch + _recoilCurrent.y, PitchClampMin, PitchClampMax);
        _recoilTarget = Vector2.zero;
        _recoilCurrent = Vector2.zero;
        ApplyViewRotation();
    }

    private async UniTask RestoreViewAfterFireStops()
    {
        float duration = viewBeforeRecoilDuration;
        float time = 0f;

        float startYaw = _yaw;
        float startPitch = _pitch;

        float targetYaw = viewBeforeRecoil.x;
        float targetPitch = Mathf.Clamp(viewBeforeRecoil.y, PitchClampMin, PitchClampMax);
        _recoilTarget = Vector2.zero;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // 缓动函数（可改成 SmoothStep / EaseOutQuad 等）
            t = Mathf.SmoothStep(0f, 1f, t);

            _yaw = Mathf.Lerp(startYaw, targetYaw, t);
            _pitch = Mathf.Lerp(startPitch, targetPitch, t);

            ApplyViewRotation();

            await UniTask.Yield();  // 在下一帧继续
        }

        // 最终保证到达目标位置
        _yaw = targetYaw;
        _pitch = targetPitch;
        _recoilTarget = Vector2.zero;
        _recoilCurrent = Vector2.zero;
        ApplyViewRotation();
    }

    private void ApplyWeaponRecoilConfig(SOFirearmConfig firearmConfig)
    {
        if (firearmConfig == null)
        {
            RecoilRaiseSpeed = _globalRecoilRaiseSpeed;
            RecoilReturnSpeed = _globalRecoilReturnSpeed;
            return;
        }

        RecoilRaiseSpeed = firearmConfig.recoilRaiseSpeed > 0f ? firearmConfig.recoilRaiseSpeed : _globalRecoilRaiseSpeed;
        RecoilReturnSpeed = firearmConfig.recoilReturnSpeed > 0f ? firearmConfig.recoilReturnSpeed : _globalRecoilReturnSpeed;
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




    private void OnWeaponChanged(EventPlayerChangeWeapon weapon)
    {
        _moveSpeed = weapon.Slot != null ? weapon.WeaponInstance.Config.moveSpeedMultiplier * MoveSpeed : MoveSpeed;
        _sprintSpeed = weapon.Slot != null ? weapon.WeaponInstance.Config.runSpeedMultiplier * SprintSpeed : SprintSpeed;

        var firearmConfig = weapon.Slot != null ? weapon.WeaponInstance.Config as SOFirearmConfig : null;
        ApplyWeaponRecoilConfig(firearmConfig);
    }



    public IArchitecture GetArchitecture() => GameArchitecture.Interface;
}







