using QFramework;
using System.Collections;
using UnityEngine;


public class FirearmWeapon :  WeaponBase
{

    public Transform FirePos;

    protected CmdFireamFire cmdFireamFire;

    private SOFirearmConfig firearmConfig;
    private int currentAmmo;
    private float nextFireTime;
    private bool isReloading;


    protected override void Start()
    {

        if (Config == null)
            Debug.LogError($"FirearmWeapon WeaponConfig is null");
        firearmConfig = Config as SOFirearmConfig;
        currentAmmo = firearmConfig != null ? firearmConfig.magSize : 0;
        cmdFireamFire = new CmdFireamFire(this);
    }



    public override void OnEquip()
    {
        Debug.Log($"FirearmWeapon {InstanceID} is OnEquip");
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

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo, reload needed.");
            return;
        }

        Vector3 dir = this.GetSystem<WeaponSystem>().GetFireDirection(FirePos);
        if(dir == Vector3.zero)
        {
            return;
        }
        cmdFireamFire.Init(FirePos.position, dir);
        this.SendCommand(cmdFireamFire);
        nextFireTime = firearmConfig.fireRate > 0 ? Time.time + 1f / firearmConfig.fireRate : Time.time;
        //print($"Firearm Weapon {Config.WeaponName} {Config.WeaponType} Try Fire");
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
        if (Config.impactEffect != null)
        {
            //TODO 待优化 对象池 创建 销毁
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            GameObject eff = GameObject.Instantiate(Config.impactEffect, hit.point, rot);

            // 避免特效嵌入表面
            eff.transform.position += hit.normal * 0.01f;
            GameObject.Destroy(eff, 1f);
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

    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        Debug.Log("Reloading... (placeholder animation/sound)");
        yield return new WaitForSeconds(firearmConfig.reloadTime);
        currentAmmo = firearmConfig.magSize;
        isReloading = false;
        Debug.Log("Reload complete.");

    }
}
