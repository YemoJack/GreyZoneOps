using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;


public struct EventPlayerChangeWeapon
{
    public WeaponInventoryEntry Slot;
    public WeaponBase WeaponInstance;
}


public class WeaponSystem : AbstractSystem
{
    private const string UnarmedWeaponName = "Melee_Unarmed";
    private const string UnarmedWeaponResKey = "Melee_Unarmed";
    private const int PreferredUnarmedWeaponId = 0;

    private WeaponInventoryModel weaponInventoryModel;
    private readonly Dictionary<int, WeaponBase> weaponInstances = new Dictionary<int, WeaponBase>();
    private readonly List<WeaponBase> instantiatedWeapons = new List<WeaponBase>();
    private readonly Dictionary<int, GameObject> weaponViewModelInstances = new Dictionary<int, GameObject>();
    private IAimRayProvider aimRayProvider;
    private bool triedBindMainCamera;
    private Transform runtimeWeaponRoot;
    private WeaponBase unarmedWeaponInstance;
    private SOMeleeConfig unarmedWeaponConfig;
    private IResLoader resLoader;

    private IAimRayProvider FallbackAimProvider
    {
        get
        {
            if (aimRayProvider != null)
            {
                return aimRayProvider;
            }

            if (!triedBindMainCamera)
            {
                triedBindMainCamera = true;
                if (Camera.main != null)
                {
                    Debug.LogWarning("WeaponSystem: Aim provider missing, defaulting to main camera.");
                    aimRayProvider = new CameraAimProvider(Camera.main);
                }
            }

            return aimRayProvider;
        }
    }


    protected override void OnInit()
    {
        weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
        resLoader = this.GetUtility<IResLoader>();
    }

    public void InitializeLoadout(Transform weaponRoot, EquipmentContainer equipment, Transform viewModelWeaponRoot = null)
    {
        if (weaponRoot == null || equipment == null)
        {
            Debug.LogWarning("WeaponSystem InitializeLoadout called with null references.");
            return;
        }

        runtimeWeaponRoot = weaponRoot;
        ClearInstantiatedWeapons();
        weaponInventoryModel.ClearSlots();

        var slots = new[]
        {
            EquipmentSlotType.Weapon1,
            EquipmentSlotType.Weapon2,
            EquipmentSlotType.Weapon3,
            EquipmentSlotType.Weapon4
        };

        foreach (var slot in slots)
        {
            var item = equipment.GetItem(slot);
            if (item == null || item.Definition == null) continue;

            var weaponItem = item.Definition;
            if (weaponItem == null || !weaponItem.IsWeapon)
            {
                Debug.LogWarning($"WeaponSystem: Item in slot {slot} is not a weapon definition.");
                continue;
            }

            var weaponConfig = weaponItem.ResolveWeaponConfig();
            if (weaponConfig == null)
            {
                Debug.LogWarning($"WeaponSystem: Weapon config missing for item {weaponItem.Name} (itemId={weaponItem.Id}).");
                continue;
            }

            var weaponPrefab = weaponConfig.WeaponPrefab;
            if (weaponPrefab == null)
            {
                Debug.LogWarning($"WeaponSystem: Weapon prefab missing for item {weaponItem.Name}.");
                continue;
            }

            var weaponObj = Object.Instantiate(weaponPrefab, weaponRoot);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;
            weaponObj.transform.SetLayerRecursively(weaponRoot.gameObject.layer);

            var weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                if (weapon.Config == null)
                {
                    weapon.Config = weaponConfig;
                }

                RegisterWeaponInstance(weapon);
                CreateOrReplaceViewModelReplica(entryWeaponId: weapon.Config != null ? weapon.Config.WeaponID : 0,
                    sourcePrefab: weaponPrefab,
                    viewModelWeaponRoot: viewModelWeaponRoot);
                instantiatedWeapons.Add(weapon);
                weapon.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Prefab {weaponPrefab.name} does not contain a WeaponBase component.");
                Object.Destroy(weaponObj);
            }
        }

        EnsureUnarmedWeaponRegistered(weaponRoot);
        EquipInitialWeapon();
    }

    public void InitializeLoadout(Transform weaponRoot, IEnumerable<GameObject> weaponPrefabs, Transform viewModelWeaponRoot = null)
    {
        if (weaponRoot == null || weaponPrefabs == null)
        {
            Debug.LogWarning("WeaponSystem InitializeLoadout called with null references.");
            return;
        }

        runtimeWeaponRoot = weaponRoot;
        ClearInstantiatedWeapons();
        weaponInventoryModel.ClearSlots();

        foreach (var weaponPrefab in weaponPrefabs)
        {
            if (weaponPrefab == null) continue;

            var weaponObj = Object.Instantiate(weaponPrefab, weaponRoot);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;
            weaponObj.transform.SetLayerRecursively(weaponRoot.gameObject.layer);

            var weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                RegisterWeaponInstance(weapon);
                CreateOrReplaceViewModelReplica(entryWeaponId: weapon.Config != null ? weapon.Config.WeaponID : 0,
                    sourcePrefab: weaponPrefab,
                    viewModelWeaponRoot: viewModelWeaponRoot);
                instantiatedWeapons.Add(weapon);
                weapon.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Prefab {weaponPrefab.name} does not contain a WeaponBase component.");
                Object.Destroy(weaponObj);
            }
        }

        EnsureUnarmedWeaponRegistered(weaponRoot);
        EquipInitialWeapon();
    }

    /// <summary>
    /// 注册并关联一把武器实例（基于配置 ID）。
    /// </summary>
    /// <param name="weapon"></param>
    public bool RegisterWeaponInstance(WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("RegisterWeaponInstance: weapon is null");
            return false;
        }

        if (weapon.Config == null)
        {
            Debug.LogWarning($"RegisterWeaponInstance: weapon {weapon.name} 配置为空");
            return false;
        }

        if (!weaponInventoryModel.AddOrActivateSlot(weapon.Config, out var entry))
        {
            return false;
        }

        weaponInstances[entry.WeaponId] = weapon;
        if (!instantiatedWeapons.Contains(weapon))
        {
            instantiatedWeapons.Add(weapon);
        }
        return true;
    }

    public void BindAimProvider(IAimRayProvider provider)
    {
        aimRayProvider = provider;
    }

    public void SwitchWeapon()
    {
        var previous = weaponInventoryModel.CurrentSlot;
        if (weaponInventoryModel.TrySwitchNextAvailable(out var entry))
        {
            HandleSwitch(entry, previous);
        }
    }

    public void StartAttack()
    {
        var currentSlot = weaponInventoryModel.CurrentSlot;
        if (currentSlot != null && weaponInstances.TryGetValue(currentSlot.WeaponId, out var weapon))
        {
            weapon.TryAttack();
            return;
        }

        TryUnarmedAttack();
    }

    public void TryUnarmedAttack()
    {
        if (unarmedWeaponInstance == null)
        {
            EnsureUnarmedWeaponRegistered(runtimeWeaponRoot);
        }

        unarmedWeaponInstance?.TryAttack();
    }

    public void ReloadCurrentWeapon()
    {
        var currentSlot = weaponInventoryModel.CurrentSlot;
        if (currentSlot != null && weaponInstances.TryGetValue(currentSlot.WeaponId, out var weapon)
            && weapon is FirearmWeapon firearmWeapon)
        {
            firearmWeapon.Reload();
        }
    }

    public WeaponBase GetCurrentWeapon()
    {
        var currentSlot = weaponInventoryModel.CurrentSlot;
        if (currentSlot != null && weaponInstances.TryGetValue(currentSlot.WeaponId, out var weapon))
        {
            return weapon;
        }

        return null;
    }



    public Ray GetFireRay()
    {
        var provider = FallbackAimProvider;
        if (provider == null)
        {
            Debug.LogWarning("WeaponSystem: Aim provider is not bound.");
            return new Ray(Vector3.zero, Vector3.forward);
        }

        return provider.GetAimRay();
    }


    public Vector3 GetFireDirection(Ray fireRay, float maxRange = 100f)
    {
        Vector3 targetPoint;

        if (Physics.Raycast(fireRay, out RaycastHit hit, maxRange))
        {
            targetPoint = hit.point;
        }
        else
        {
            // 射线未命中，取最大射程点
            targetPoint = fireRay.origin + fireRay.direction * maxRange;
        }

        // 方向 = 目标点 - 摄像机中心
        Vector3 fireDir = (targetPoint - fireRay.origin).normalized;
        var provider = FallbackAimProvider;
        Vector3 camForward = provider != null ? provider.GetAimForward() : Vector3.forward;
        if (Vector3.Dot(fireDir, camForward) <= 0)
        {
            Debug.Log("摄像机前方有阻挡，请和障碍物保持一定距离");
            return Vector3.zero;
        }


        return fireDir;
    }

    public LayerMask GetPlayerDamageHitLayers()
    {
        var config = GameSettingManager.Instance?.Config;
        return config != null ? config.PlayerDamageHitLayers : Physics.DefaultRaycastLayers;
    }

    public int GetPlayerDamageHitMaskValue()
    {
        return GetPlayerDamageHitLayers().value;
    }

    public bool EquipInitialWeapon()
    {
        if (weaponInventoryModel.CurrentSlot == null)
        {
            int unarmedWeaponId = unarmedWeaponInstance != null && unarmedWeaponInstance.Config != null
                ? unarmedWeaponInstance.Config.WeaponID
                : 0;
            if (unarmedWeaponId != 0 && weaponInventoryModel.TryGetSlotById(unarmedWeaponId, out var unarmedSlot))
            {
                HandleSwitch(unarmedSlot, null);
                return true;
            }

            Debug.LogWarning("EquipInitialWeapon: 无可用武器");
            this.SendEvent(new EventPlayerChangeWeapon
            {
                Slot = null,
                WeaponInstance = null
            });
            return false;
        }

        HandleSwitch(weaponInventoryModel.CurrentSlot, null);
        return true;
    }

    public bool SwitchWeaponByIndex(int index)
    {
        var previous = weaponInventoryModel.CurrentSlot;
        if (weaponInventoryModel.TrySwitchWeapon(index, out var entry))
        {
            HandleSwitch(entry, previous);
            return true;
        }

        return false;
    }

    private void HandleSwitch(WeaponInventoryEntry entry, WeaponInventoryEntry previous)
    {
        if (entry == null)
        {
            return;
        }

        if (!weaponInstances.TryGetValue(entry.WeaponId, out var weapon))
        {
            Debug.LogWarning($"HandleSwitch: 未找到武器实例，ID={entry.WeaponId}");
            return;
        }

        if (previous != null && weaponInstances.TryGetValue(previous.WeaponId, out var previousWeapon)
            && previousWeapon != null && previousWeapon != weapon)
        {
            previousWeapon.gameObject.SetActive(false);
            previousWeapon.OnUnEquip();
        }

        UpdateWeaponActiveState(weapon, entry.WeaponId);
        weapon.OnEquip();
        this.SendEvent(new EventPlayerChangeWeapon
        {
            Slot = entry,
            WeaponInstance = weapon
        });
    }

    private void UpdateWeaponActiveState(WeaponBase activeWeapon, int activeWeaponId)
    {
        foreach (var weapon in instantiatedWeapons)
        {
            if (weapon == null) continue;
            weapon.gameObject.SetActive(weapon == activeWeapon);
        }

        foreach (var pair in weaponViewModelInstances)
        {
            if (pair.Value == null)
            {
                continue;
            }

            pair.Value.SetActive(pair.Key == activeWeaponId);
        }
    }

    private void ClearInstantiatedWeapons()
    {
        foreach (var weapon in instantiatedWeapons)
        {
            if (weapon == null) continue;
            Object.Destroy(weapon.gameObject);
        }

        instantiatedWeapons.Clear();
        weaponInstances.Clear();
        unarmedWeaponInstance = null;

        foreach (var vm in weaponViewModelInstances.Values)
        {
            if (vm == null) continue;
            Object.Destroy(vm);
        }

        weaponViewModelInstances.Clear();
    }

    private void EnsureUnarmedWeaponRegistered(Transform weaponRoot)
    {
        if (weaponRoot == null)
        {
            return;
        }

        if (unarmedWeaponInstance != null)
        {
            return;
        }

        if (unarmedWeaponConfig == null)
        {
            unarmedWeaponConfig = LoadOrCreateRuntimeUnarmedConfig();
        }

        if (unarmedWeaponConfig == null)
        {
            return;
        }

        var unarmedObj = new GameObject(UnarmedWeaponName);
        unarmedObj.transform.SetParent(weaponRoot, false);
        unarmedObj.transform.localPosition = Vector3.zero;
        unarmedObj.transform.localRotation = Quaternion.identity;
        unarmedObj.transform.localScale = Vector3.one;
        unarmedObj.transform.SetLayerRecursively(weaponRoot.gameObject.layer);

        var unarmedWeapon = unarmedObj.AddComponent<MeleeWeapon>();
        unarmedWeapon.Config = unarmedWeaponConfig;

        if (!RegisterWeaponInstance(unarmedWeapon))
        {
            Object.Destroy(unarmedObj);
            return;
        }

        unarmedWeaponInstance = unarmedWeapon;
        if (!instantiatedWeapons.Contains(unarmedWeapon))
        {
            instantiatedWeapons.Add(unarmedWeapon);
        }

        unarmedObj.SetActive(false);
    }

    private SOMeleeConfig LoadOrCreateRuntimeUnarmedConfig()
    {
        var loadedConfig = LoadUnarmedConfigByResLoader();
        if (loadedConfig != null)
        {
            var runtimeConfig = Object.Instantiate(loadedConfig);
            resLoader?.UnloadSync(loadedConfig);
            runtimeConfig.name = $"{loadedConfig.name}_Runtime";
            runtimeConfig.WeaponType = WeaponType.Melee;
            runtimeConfig.WeaponName = string.IsNullOrWhiteSpace(runtimeConfig.WeaponName)
                ? UnarmedWeaponName
                : runtimeConfig.WeaponName;
            runtimeConfig.isUnarmedWeapon = true;

            int preferredId = runtimeConfig.WeaponID != 0 ? runtimeConfig.WeaponID : PreferredUnarmedWeaponId;
            runtimeConfig.WeaponID = AllocateUniqueWeaponId(preferredId);
            return runtimeConfig;
        }

        Debug.LogWarning($"WeaponSystem: Failed to load unarmed melee config by IResLoader (keys: '{UnarmedWeaponResKey}'), using runtime default.");
        return CreateRuntimeUnarmedConfig();
    }

    private SOMeleeConfig LoadUnarmedConfigByResLoader()
    {
        if (resLoader == null)
        {
            resLoader = this.GetUtility<IResLoader>();
        }

        if (resLoader == null)
        {
            return null;
        }

        var byResKey = resLoader.LoadSync<SOMeleeConfig>(UnarmedWeaponResKey);
        if (byResKey != null)
        {
            return byResKey;
        }

        Debug.LogError("LoadUnarmedConfigByResLoader is null");

        return null;
    }

    private int AllocateUniqueWeaponId(int preferredId)
    {
        int candidate = preferredId;
        if (candidate == 0)
        {
            candidate = PreferredUnarmedWeaponId;
        }

        while (candidate == 0 || weaponInstances.ContainsKey(candidate))
        {
            candidate--;
        }

        return candidate;
    }

    private SOMeleeConfig CreateRuntimeUnarmedConfig()
    {
        var config = ScriptableObject.CreateInstance<SOMeleeConfig>();
        config.WeaponID = AllocateUniqueWeaponId(PreferredUnarmedWeaponId);
        config.WeaponName = UnarmedWeaponName;
        config.WeaponType = WeaponType.Melee;
        config.Discription = "Default unarmed fist attack.";
        config.WeaponPrefab = null;

        config.moveSpeedMultiplier = 1f;
        config.runSpeedMultiplier = 1f;
        config.impactEffect = null;
        config.damage = 12f;
        config.range = 1.6f;
        config.radius = 0.2f;
        config.attackInterval = 0.5f;
        config.isUnarmedWeapon = true;
        return config;
    }

    private void CreateOrReplaceViewModelReplica(int entryWeaponId, GameObject sourcePrefab, Transform viewModelWeaponRoot)
    {
        if (viewModelWeaponRoot == null || sourcePrefab == null || entryWeaponId == 0)
        {
            return;
        }

        if (weaponViewModelInstances.TryGetValue(entryWeaponId, out var existing) && existing != null)
        {
            Object.Destroy(existing);
        }

        var vmObj = Object.Instantiate(sourcePrefab, viewModelWeaponRoot);
        vmObj.name = $"{sourcePrefab.name}_VM";
        vmObj.transform.localPosition = Vector3.zero;
        vmObj.transform.localRotation = Quaternion.identity;
        vmObj.transform.localScale = Vector3.one;

        PrepareViewModelReplica(vmObj);
        vmObj.SetActive(false);
        weaponViewModelInstances[entryWeaponId] = vmObj;
    }

    private static void PrepareViewModelReplica(GameObject vmObj)
    {
        if (vmObj == null)
        {
            return;
        }

        var viewModelLayer = LayerMask.NameToLayer("ViewModel");
        if (viewModelLayer >= 0)
        {
            vmObj.transform.SetLayerRecursively(viewModelLayer);
        }

        foreach (var behaviour in vmObj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = false;
        }

        foreach (var collider in vmObj.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null)
            {
                continue;
            }

            collider.enabled = false;
        }

        foreach (var rigidbody in vmObj.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        foreach (var audioSource in vmObj.GetComponentsInChildren<AudioSource>(true))
        {
            if (audioSource == null)
            {
                continue;
            }

            audioSource.enabled = false;
        }
    }

}
