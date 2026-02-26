using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;


public struct EventWeaponAmmoChanged
{
    public int WeaponId;
    public string WeaponName;
    public int CurrentAmmo;
    public int TotalAmmo;
}

public struct EventWeaponRecoilApplied
{
    public int WeaponId;
    public string WeaponName;
    public Vector2 RecoilStep;
}

public struct EventFirearmAimChanged
{
    public FirearmWeapon Weapon;
    public bool Aiming;
    public float ZoomFactor;
    public float Duration;

}

public struct EventFireRecoilRecover
{

}


public class FirearmWeapon : WeaponBase
{

    public Transform FirePos;

    protected CmdFireamFire cmdFireamFire;

    private SOFirearmConfig firearmConfig;
    private InputSys inputSys;
    private int currentAmmo;
    private float nextFireTime;
    private bool isReloading;
    private bool isBurstFiring;
    private IObjectPoolUtility objectPoolUtility;
    private IObjectPool<GameObject> impactEffectPool;
    private Vector2 currentRecoilOffset = Vector2.zero;
    private int recoilStepIndex;
    private bool isAiming;
    private CancellationTokenSource aimTransitionCts;
    private bool isEquipped;
    private bool lastAimHold;
    private bool lastFireHold;
    private bool dryFireTriggeredThisPress;
    private EPlayerMoveState currentMoveState = EPlayerMoveState.Idle;
    private IUnRegister moveStateUnregister;
    private float firingSpread;
    private bool initialized;
    private CancellationTokenSource impactEffectCts;
    private readonly HashSet<GameObject> impactEffectInstances = new HashSet<GameObject>();

    [HideInInspector]
    /// <summary>垂直后坐力倍率（附件影响）</summary>
    public float verticalRecoilMul = 1f;
    [HideInInspector]
    /// <summary>水平后坐力倍率（附件影响）</summary>
    public float horizontalRecoilMul = 1f;


    public int CurrentAmmo => currentAmmo;
    public int TotalAmmo => firearmConfig != null ? firearmConfig.magSize : 0;

    public bool IsAutomatic => firearmConfig != null && firearmConfig.currentFireMode == FireMode.Auto;
    public bool IsBurstMode => firearmConfig != null && firearmConfig.currentFireMode == FireMode.Burst;
    public bool IsSingleMode => firearmConfig == null || firearmConfig.currentFireMode == FireMode.Single;
    public bool IsAiming => isAiming;


    protected override void Start()
    {
        EnsureInitialized();
        EnsureImpactEffectPool();
    }



    public override void OnEquip()
    {
        Debug.Log($"FirearmWeapon {InstanceID} is OnEquip");
        EnsureInitialized();
        EnsureImpactEffectPool();
        currentRecoilOffset = Vector2.zero;
        recoilStepIndex = 0;
        firingSpread = 0f;

        NotifyAmmoChanged();

        isEquipped = true;
        lastAimHold = false;
        lastFireHold = false;
        dryFireTriggeredThisPress = false;
        RegisterMoveStateListener();
        ApplyConfiguredWeaponPoseInstant(aiming: false);
    }

    public override void OnUnEquip()
    {
        isEquipped = false;
        CancelAimTransition();
        ClearImpactEffectPool();
        SetAimState(false);
        NotifyAimState(false, firearmConfig != null ? firearmConfig.aimTime : 0.001f);
        ApplyConfiguredWeaponPoseInstant(aiming: false);
        moveStateUnregister?.UnRegister();
        moveStateUnregister = null;
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        if (Config == null)
        {
            Debug.LogError($"FirearmWeapon WeaponConfig is null");
        }

        firearmConfig = Config as SOFirearmConfig;
        currentAmmo = firearmConfig != null ? firearmConfig.magSize : 0;
        cmdFireamFire = new CmdFireamFire(this);
        inputSys = this.GetSystem<InputSys>();
        objectPoolUtility = this.GetUtility<IObjectPoolUtility>();
        initialized = true;
    }

    private void OnDestroy()
    {
        moveStateUnregister?.UnRegister();
        moveStateUnregister = null;
        CancelAimTransition();
        ClearImpactEffectPool();
    }

    private void Update()
    {
        if (!isEquipped)
        {
            return;
        }

        bool aimHold = inputSys.AimHold;

        if (aimHold != lastAimHold)
        {
            lastAimHold = aimHold;
            BeginAimTransition(aimHold);
        }

        bool fireHold = inputSys.FireHold;
        if (fireHold != lastFireHold)
        {
            lastFireHold = fireHold;
            if (!fireHold && !isBurstFiring)
            {
                dryFireTriggeredThisPress = false;
                RecoverRecoil();
                this.SendEvent<EventFireRecoilRecover>();
            }
        }

        RecoverSpread();
    }

    public void SetAimState(bool aiming)
    {
        isAiming = aiming;
    }

    private void BeginAimTransition(bool aiming)
    {
        CancelAimTransition();
        aimTransitionCts = new CancellationTokenSource();
        var token = aimTransitionCts.Token;

        float duration = firearmConfig != null ? Mathf.Max(firearmConfig.aimTime, 0.001f) : 0.001f;

        Debug.Log($"FirearmWeapon {InstanceID} is BeginAimTransition {aiming} duration {duration}");
        if (!aiming)
        {
            SetAimState(false);
        }

        AnimateConfiguredWeaponPose(aiming, duration, token).Forget();
        NotifyAimState(aiming, duration);
        CompleteAimAfterDelay(aiming, duration, token).Forget();
    }

    private void CancelAimTransition()
    {
        aimTransitionCts?.Cancel();
        aimTransitionCts?.Dispose();
        aimTransitionCts = null;
    }

    private async UniTaskVoid CompleteAimAfterDelay(bool aiming, float duration, CancellationToken token)
    {
        try
        {
            if (duration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
            }

            SetAimState(aiming);
            Debug.Log($"FirearmWeapon {InstanceID} is CompleteAimAfterDelay {aiming}");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void NotifyAimState(bool aiming, float duration)
    {
        float zoom = firearmConfig != null ? firearmConfig.zoomFactor : 1f;

        this.SendEvent(new EventFirearmAimChanged
        {
            Weapon = this,
            Aiming = aiming,
            ZoomFactor = zoom,
            Duration = duration
        });
    }

    private void ApplyConfiguredWeaponPoseInstant(bool aiming)
    {
        if (firearmConfig == null)
        {
            return;
        }

        transform.localPosition = aiming ? firearmConfig.gunAimPos : firearmConfig.gunEquiptPos;
        transform.localRotation = Quaternion.Euler(aiming ? firearmConfig.gunAimRot : firearmConfig.gunEquiptRot);
    }

    private async UniTaskVoid AnimateConfiguredWeaponPose(bool aiming, float duration, CancellationToken token)
    {
        if (firearmConfig == null)
        {
            return;
        }

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 targetPos = aiming ? firearmConfig.gunAimPos : firearmConfig.gunEquiptPos;
        Quaternion targetRot = Quaternion.Euler(aiming ? firearmConfig.gunAimRot : firearmConfig.gunEquiptRot);

        if (duration <= 0.0001f)
        {
            transform.localPosition = targetPos;
            transform.localRotation = targetRot;
            return;
        }

        float elapsed = 0f;

        try
        {
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                await UniTask.Yield(cancellationToken: token);
                elapsed += Time.deltaTime;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
    }

    public override void TryAttack()
    {
        if (firearmConfig == null)
        {
            Debug.LogWarning("FirearmWeapon: firearmConfig is null, cannot attack.");
            return;
        }

        if (isReloading)
        {
            return;
        }

        if (IsBurstMode)
        {
            TryStartBurstFire();
        }
        else
        {
            TryFireSingleShot();
        }
    }


    public override void OnHitTarget(RaycastHit hit, System.Object param = null)
    {
        PlayImpactEffect(hit);

        float t = 0;
        float dis = hit.distance;
        if (param is (float time, float distance))
        {
            t = time;
            dis = distance;
        }

        var healthComponent = hit.collider.GetComponentInParent<HealthComponent>();
        if (healthComponent != null)
        {
            float hitDamage = CalculateDamage(dis);
            healthComponent.ApplyDamage(hitDamage);
            Debug.Log($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Hit Target {hitDamage} \n time：{t} distance ：{dis}");

        }

        Debug.Log($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Hit Target \n time：{t} distance ：{dis}");

    }


    private void PlayImpactEffect(RaycastHit hit)
    {
        EnsureImpactEffectPool();
        if (impactEffectPool != null)
        {
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            GameObject eff = impactEffectPool.Get();
            eff.transform.SetPositionAndRotation(hit.point, rot);

            // 避免特效嵌入表面
            eff.transform.position += hit.normal * 0.01f;
            var token = impactEffectCts != null
                ? impactEffectCts.Token
                : this.GetCancellationTokenOnDestroy();
            ReleaseImpactEffect(eff, 1f, token).Forget();
        }
    }

    private async UniTask ReleaseImpactEffect(GameObject effect, float delay, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (effect != null)
        {
            impactEffectPool?.Release(effect);
        }
    }

    private void EnsureImpactEffectPool()
    {
        if (impactEffectPool != null || objectPoolUtility == null || Config?.impactEffect == null)
        {
            return;
        }

        impactEffectCts?.Dispose();
        impactEffectCts = new CancellationTokenSource();

        impactEffectPool = objectPoolUtility.CreatePool(
            factory: CreateImpactEffectInstance,
            onGet: eff =>
            {
                if (eff != null)
                {
                    eff.SetActive(true);
                }
            },
            onRelease: eff =>
            {
                if (eff != null)
                {
                    eff.SetActive(false);
                }
            },
            maxCount: 24);
    }

    private GameObject CreateImpactEffectInstance()
    {
        var effect = Instantiate(Config.impactEffect);
        impactEffectInstances.Add(effect);
        return effect;
    }

    private void ClearImpactEffectPool()
    {
        impactEffectCts?.Cancel();
        impactEffectCts?.Dispose();
        impactEffectCts = null;

        impactEffectPool?.Clear();
        impactEffectPool = null;

        if (impactEffectInstances.Count == 0)
        {
            return;
        }

        foreach (var effect in impactEffectInstances)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        impactEffectInstances.Clear();
    }


    public void Reload()
    {
        if (firearmConfig == null)
        {
            Debug.LogWarning("FirearmWeapon: firearmConfig is null, cannot reload.");
            return;
        }

        if (isReloading)
        {
            return;
        }

        if (currentAmmo >= firearmConfig.magSize)
        {
            Debug.Log("Magazine already full.");
            return;
        }

        ReloadRoutine().Forget();
    }

    public void ConsumeAmmo()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        NotifyAmmoChanged();

    }

    private async UniTask ReloadRoutine()
    {
        isReloading = true;
        Debug.Log("Reloading... (placeholder animation/sound)");
        this.SendEvent(new EventWeaponReloadStarted
        {
            WeaponId = Config?.WeaponID ?? 0,
            WeaponName = Config?.WeaponName,
            Duration = firearmConfig != null ? firearmConfig.reloadTime : 0f,
            Position = transform.position
        });
        await UniTask.Delay(TimeSpan.FromSeconds(firearmConfig.reloadTime));
        currentAmmo = firearmConfig.magSize;
        isReloading = false;
        Debug.Log("Reload complete.");
        NotifyAmmoChanged();
        this.SendEvent(new EventWeaponReloadFinished
        {
            WeaponId = Config?.WeaponID ?? 0,
            WeaponName = Config?.WeaponName,
            Position = transform.position
        });

    }

    private void NotifyAmmoChanged()
    {
        if (Config == null)
        {
            return;
        }

        this.SendEvent(new EventWeaponAmmoChanged
        {
            WeaponId = Config.WeaponID,
            WeaponName = Config.WeaponName,
            CurrentAmmo = currentAmmo,
            TotalAmmo = TotalAmmo
        });
    }

    public void SwitchFireMode()
    {
        if (firearmConfig == null)
        {
            Debug.LogWarning("FirearmWeapon: firearmConfig is null, cannot switch fire mode.");
            return;
        }

        FireMode[] orderedModes = new[] { FireMode.Single, FireMode.Burst, FireMode.Auto };
        var currentIndex = System.Array.IndexOf(orderedModes, firearmConfig.currentFireMode);
        int attempts = orderedModes.Length;

        while (attempts-- > 0)
        {
            currentIndex = (currentIndex + 1) % orderedModes.Length;
            var candidate = orderedModes[currentIndex];

            if (firearmConfig.availableFireModes.HasFlag(candidate))
            {
                firearmConfig.currentFireMode = candidate;
                Debug.Log($"Switched fire mode to {candidate}");
                return;
            }
        }
    }

    private void TryFireSingleShot()
    {
        if (!CanFire())
        {
            return;
        }

        FireOneRound();
    }

    private void TryStartBurstFire()
    {
        if (isBurstFiring)
        {
            return;
        }

        if (!CanFire())
        {
            return;
        }

        BurstFireRoutine().Forget();
    }

    private bool CanFire()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo, reload needed.");
            if (!dryFireTriggeredThisPress)
            {
                dryFireTriggeredThisPress = true;
                this.SendEvent(new EventWeaponDryFire
                {
                    WeaponId = Config?.WeaponID ?? 0,
                    WeaponName = Config?.WeaponName,
                    Position = transform.position
                });
            }
            return false;
        }

        return true;
    }

    private void FireOneRound()
    {

        Vector2 recoilOffset = CalculateRecoilOffset();

        var weaponSystem = this.GetSystem<WeaponSystem>();
        Ray fireRay = weaponSystem.GetFireRay();
        Vector3 dir = weaponSystem.GetFireDirection(fireRay, firearmConfig != null ? firearmConfig.range : 100f);
        if (dir == Vector3.zero)
        {
            return;
        }

        dir = ApplySpreadToDirection(dir);
        //dir = ApplyRecoilToDirection(dir, recoilOffset);

        cmdFireamFire.Init(fireRay.origin, dir);
        this.SendCommand(cmdFireamFire);
        nextFireTime = firearmConfig.fireRate > 0 ? Time.time + 1f / firearmConfig.fireRate : Time.time;
        this.SendEvent(new EventWeaponFired
        {
            WeaponId = Config?.WeaponID ?? 0,
            WeaponName = Config?.WeaponName,
            Position = FirePos != null ? FirePos.position : transform.position,
            GunshotRange = firearmConfig != null ? Mathf.Max(0f, firearmConfig.GunshotRange) : 0f
        });

        if (recoilOffset != Vector2.zero)
        {
            this.SendEvent(new EventWeaponRecoilApplied
            {
                WeaponId = Config?.WeaponID ?? 0,
                WeaponName = Config?.WeaponName,
                RecoilStep = recoilOffset
            });
        }
        IncreaseSpreadOnFire();
        //print($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Try Fire");
    }

    private void RegisterMoveStateListener()
    {
        moveStateUnregister?.UnRegister();
        moveStateUnregister = this.RegisterEvent<EventPlayerChangeMoveState>(OnPlayerMoveStateChanged);
    }

    private void OnPlayerMoveStateChanged(EventPlayerChangeMoveState evt)
    {
        currentMoveState = evt.CurrentState;
    }

    private float CalculateCurrentSpread()
    {
        if (firearmConfig == null)
        {
            return 0f;
        }

        float baseSpread;
        if (isAiming)
        {
            baseSpread = firearmConfig.aimSpread;
        }
        else
        {
            baseSpread = GetMovementSpread(currentMoveState);
        }

        return Mathf.Max(0f, baseSpread + firingSpread);
    }

    private float GetMovementSpread(EPlayerMoveState moveState)
    {
        switch (moveState)
        {
            case EPlayerMoveState.Walk:
                return firearmConfig.walkSpread;
            case EPlayerMoveState.Run:
                return firearmConfig.runSpread;
            case EPlayerMoveState.Jump:
            case EPlayerMoveState.Fall:
                return firearmConfig.jumpSpread;
            default:
                return firearmConfig.idleSpread;
        }
    }

    private Vector3 ApplySpreadToDirection(Vector3 baseDirection)
    {
        float spread = CalculateCurrentSpread();
        if (spread <= 0f)
        {
            return baseDirection;
        }

        float yaw = UnityEngine.Random.Range(-spread, spread);
        float pitch = UnityEngine.Random.Range(-spread, spread);
        Quaternion spreadRotation = Quaternion.Euler(-pitch, yaw, 0f);
        return spreadRotation * baseDirection;
    }

    private void IncreaseSpreadOnFire()
    {
        if (firearmConfig == null)
        {
            return;
        }

        float maxSpread = firearmConfig.maxSpreadWhileFiring;
        if (isAiming && firearmConfig.maxAimSpreadWhileFiring > 0f)
        {
            maxSpread = Mathf.Min(maxSpread, firearmConfig.maxAimSpreadWhileFiring);
        }

        firingSpread = Mathf.Min(
            maxSpread,
            firingSpread + firearmConfig.spreadIncreasePerShot);
    }

    private void RecoverSpread()
    {
        if (firearmConfig == null || firingSpread <= 0f)
        {
            return;
        }

        if (inputSys != null && inputSys.FireHold)
        {
            return;
        }

        float delta = Time.deltaTime;
        firingSpread = Mathf.Max(0f, firingSpread - firearmConfig.spreadRecoveryRate * delta);
    }

    private Vector2 CalculateRecoilOffset()
    {

        if (firearmConfig.recoilPattern == null || firearmConfig.recoilPattern.Length == 0)
        {
            return currentRecoilOffset;
        }

        int patternIndex = Mathf.Clamp(recoilStepIndex, 0, firearmConfig.recoilPattern.Length - 1);
        Vector2 patternStep = firearmConfig.recoilPattern[patternIndex];

        float controlFactor = Mathf.Clamp01(firearmConfig.recoilControl / 100f);
        float controlMultiplier = Mathf.Lerp(firearmConfig.recoilMulRange.y, firearmConfig.recoilMulRange.x, controlFactor);

        patternStep.x *= horizontalRecoilMul;
        patternStep.y *= verticalRecoilMul;
        patternStep *= controlMultiplier;

        currentRecoilOffset = patternStep;

        recoilStepIndex = Mathf.Min(recoilStepIndex + 1, firearmConfig.recoilPattern.Length - 1);

        return currentRecoilOffset;
    }



    private void RecoverRecoil()
    {
        if (currentRecoilOffset == Vector2.zero || firearmConfig == null)
        {
            recoilStepIndex = 0;
            return;
        }



        currentRecoilOffset = Vector2.zero;

        recoilStepIndex = 0;
    }




    private async UniTask BurstFireRoutine()
    {
        isBurstFiring = true;
        int burstCount = 3;

        for (int i = 0; i < burstCount; i++)
        {
            if (!CanFire())
                break;

            FireOneRound();

            float delay = firearmConfig.fireRate > 0
                ? 1f / firearmConfig.fireRate
                : 0f;

            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());
            else
                await UniTask.Yield(); // 下一帧继续，防止阻塞
        }

        isBurstFiring = false;
    }

    private float CalculateDamage(float distance)
    {
        if (firearmConfig == null)
        {
            return 0f;
        }

        float multiplier = 1f;
        var falloff = firearmConfig.damageFalloff;
        if (falloff != null && falloff.Count > 0)
        {
            for (int i = 0; i < falloff.Count; i++)
            {
                if (distance >= falloff[i].distance)
                {
                    multiplier = falloff[i].multiplier;
                }
                else
                {
                    break;
                }
            }
        }

        return Mathf.Max(0f, firearmConfig.baseDamage * multiplier);
    }

}
