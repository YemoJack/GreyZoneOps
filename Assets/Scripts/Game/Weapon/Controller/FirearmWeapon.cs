using QFramework;
using System.Collections;
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


public class FirearmWeapon :  WeaponBase
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
    private float lastRecoilTime;

    public int CurrentAmmo => currentAmmo;
    public int TotalAmmo => firearmConfig != null ? firearmConfig.magSize : 0;

    public bool IsAutomatic => firearmConfig != null && firearmConfig.currentFireMode == FireMode.Auto;
    public bool IsBurstMode => firearmConfig != null && firearmConfig.currentFireMode == FireMode.Burst;
    public bool IsSingleMode => firearmConfig == null || firearmConfig.currentFireMode == FireMode.Single;


    protected override void Start()
    {

        if (Config == null)
            Debug.LogError($"FirearmWeapon WeaponConfig is null");
        firearmConfig = Config as SOFirearmConfig;
        currentAmmo = firearmConfig != null ? firearmConfig.magSize : 0;
        cmdFireamFire = new CmdFireamFire(this);
        inputSys = this.GetSystem<InputSys>();
        objectPoolUtility = this.GetUtility<IObjectPoolUtility>();

        if (Config?.impactEffect != null)
        {
            impactEffectPool = objectPoolUtility.CreatePool(
                factory: () => Instantiate(Config.impactEffect),
                onGet: eff => eff.SetActive(true),
                onRelease: eff => eff.SetActive(false),
                maxCount: 24);
        }
    }



    public override void OnEquip()
    {
        Debug.Log($"FirearmWeapon {InstanceID} is OnEquip");
        currentRecoilOffset = Vector2.zero;
        recoilStepIndex = 0;
        lastRecoilTime = 0f;
        NotifyAmmoChanged();
    }

    public override void OnUnEquip()
    {
        
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


    public override void OnHitTarget(RaycastHit hit,System.Object param = null)
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
            healthComponent.ApplyDamage(Config is SOFirearmConfig firearmConfig ? firearmConfig.baseDamage : 0f);
        }

        Debug.Log($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Hit Target \n time：{t} distance ：{dis}");

    }


    private void PlayImpactEffect(RaycastHit hit)
    {
        if (impactEffectPool != null)
        {
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            GameObject eff = impactEffectPool.Get();
            eff.transform.SetPositionAndRotation(hit.point, rot);

            // 避免特效嵌入表面
            eff.transform.position += hit.normal * 0.01f;
            StartCoroutine(ReleaseImpactEffect(eff, 1f));
        }
    }

    private IEnumerator ReleaseImpactEffect(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            impactEffectPool?.Release(effect);
        }
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

        StartCoroutine(ReloadRoutine());
    }

    public void ConsumeAmmo()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        NotifyAmmoChanged();

    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        Debug.Log("Reloading... (placeholder animation/sound)");
        yield return new WaitForSeconds(firearmConfig.reloadTime);
        currentAmmo = firearmConfig.magSize;
        isReloading = false;
        Debug.Log("Reload complete.");
        NotifyAmmoChanged();

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

        StartCoroutine(BurstFireRoutine());
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
            return false;
        }

        return true;
    }

    private void FireOneRound()
    {
        Vector2 recoilStep;
        Vector2 recoilOffset = CalculateRecoilOffset(out recoilStep);

        Vector3 dir = this.GetSystem<WeaponSystem>().GetFireDirection(FirePos);
        if (dir == Vector3.zero)
        {
            return;
        }

        dir = ApplyRecoilToDirection(dir, recoilOffset);

        if (recoilStep != Vector2.zero)
        {
            this.SendEvent(new EventWeaponRecoilApplied
            {
                WeaponId = Config?.WeaponID ?? 0,
                WeaponName = Config?.WeaponName,
                RecoilStep = recoilStep
            });
        }

        cmdFireamFire.Init(FirePos.position, dir);
        this.SendCommand(cmdFireamFire);
        nextFireTime = firearmConfig.fireRate > 0 ? Time.time + 1f / firearmConfig.fireRate : Time.time;
        //print($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Try Fire");
    }

    private Vector3 ApplyRecoilToDirection(Vector3 baseDirection, Vector2 recoilOffset)
    {
        if (firearmConfig == null)
        {
            return baseDirection;
        }

        if (recoilOffset == Vector2.zero)
        {
            return baseDirection;
        }

        Quaternion recoilRotation = Quaternion.Euler(-recoilOffset.y, recoilOffset.x, 0f);
        return recoilRotation * baseDirection;
    }

    private Vector2 CalculateRecoilOffset(out Vector2 appliedStep)
    {
        appliedStep = Vector2.zero;
        if (firearmConfig.recoilPattern == null || firearmConfig.recoilPattern.Length == 0)
        {
            return currentRecoilOffset;
        }

        int patternIndex = Mathf.Clamp(recoilStepIndex, 0, firearmConfig.recoilPattern.Length - 1);
        Vector2 patternStep = firearmConfig.recoilPattern[patternIndex];

        float controlFactor = Mathf.Clamp01(firearmConfig.recoilControl / 100f);
        float controlMultiplier = Mathf.Lerp(firearmConfig.recoilMulRange.y, firearmConfig.recoilMulRange.x, controlFactor);

        patternStep.x *= firearmConfig.horizontalRecoilMul;
        patternStep.y *= firearmConfig.verticalRecoilMul;
        patternStep *= controlMultiplier;

        currentRecoilOffset += patternStep;
        appliedStep = patternStep;
        recoilStepIndex = Mathf.Min(recoilStepIndex + 1, firearmConfig.recoilPattern.Length - 1);
        lastRecoilTime = Time.time;

        return currentRecoilOffset;
    }

    private void Update()
    {
        if (ShouldRecoverRecoil())
        {
            RecoverRecoil();
        }
    }

    private bool ShouldRecoverRecoil()
    {
        if (firearmConfig == null)
        {
            return false;
        }

        if (isBurstFiring)
        {
            return false;
        }

        if (inputSys != null && inputSys.FireHold)
        {
            return false;
        }

        if (lastRecoilTime <= 0f)
        {
            return false;
        }

        float fireInterval = firearmConfig.fireRate > 0 ? 1f / firearmConfig.fireRate : 0f;
        if (Time.time - lastRecoilTime < Mathf.Max(fireInterval, Time.deltaTime))
        {
            return false;
        }

        return true;
    }

    private void RecoverRecoil()
    {
        if (currentRecoilOffset == Vector2.zero || firearmConfig == null)
        {
            recoilStepIndex = 0;
            return;
        }

        float recoverySpeed = firearmConfig.recoilRecoverySpeed;
        if (recoverySpeed <= 0f)
        {
            return;
        }

        if (Time.time - lastRecoilTime < Time.deltaTime)
        {
            return;
        }

        currentRecoilOffset = Vector2.MoveTowards(currentRecoilOffset, Vector2.zero, recoverySpeed * Time.deltaTime);

        if (currentRecoilOffset == Vector2.zero)
        {
            recoilStepIndex = 0;
        }
    }

    private IEnumerator BurstFireRoutine()
    {
        isBurstFiring = true;
        int burstCount = 3;

        for (int i = 0; i < burstCount; i++)
        {
            if (!CanFire())
            {
                break;
            }

            FireOneRound();
            yield return new WaitForSeconds(firearmConfig.fireRate > 0 ? 1f / firearmConfig.fireRate : 0f);
        }

        isBurstFiring = false;
    }
}
