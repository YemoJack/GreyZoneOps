using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory
{
    Weapon,
    helmet,
    Armor,
    Ammo,
    Medical,
    ChestRig,
    Backpack,
    Collection
}

public enum ItemQuality
{
    White,
    Green,
    Blue,
    Purple,
    Orange,
    Red
}

[CreateAssetMenu(fileName = "Item_0", menuName = "InventoryConfig/Item Config")]
public class SOItemConfig : ScriptableObject
{
    private const string ItemConfigResPrefix = "Item_";
    private const string WeaponConfigResPrefix = "Weapon_";

    private static readonly Dictionary<int, SOItemConfig> configById = new Dictionary<int, SOItemConfig>();

    [Header("Base")]
    public int Id;
    public string Name;
    public string ResName;
    public Vector2Int Size = Vector2Int.one;
    public Sprite Icon;
    public int MaxStack = 1;
    public ItemCategory Category;
    public ItemQuality Quality = ItemQuality.White;
    [Min(0)]
    public int Value;

    public int ItemId => Id;

    public string ResourceKey => BuildItemConfigKey(ItemId);

    public bool IsWeapon => Category == ItemCategory.Weapon;

    public bool IsRotatable => Size.x != Size.y;

    public bool IsContainer =>
        Category == ItemCategory.Backpack ||
        Category == ItemCategory.ChestRig;

    private void OnValidate()
    {
        configById.Clear();
        MaxStack = Mathf.Max(1, MaxStack);
        Size = new Vector2Int(Mathf.Max(1, Size.x), Mathf.Max(1, Size.y));
        Value = Mathf.Max(0, Value);
    }

    public SOWeaponConfigBase ResolveWeaponConfig()
    {
        if (!IsWeapon || Id <= 0)
        {
            return null;
        }

        var resLoader = GameArchitecture.Interface.GetUtility<IResLoader>();
        if (resLoader == null)
        {
            Debug.LogWarning($"SOItemConfig: IResLoader is null, cannot resolve weapon config for itemId={Id}.");
            return null;
        }

        string key = $"{WeaponConfigResPrefix}{Id}";
        var config = resLoader.LoadSync<SOWeaponConfigBase>(key);
        if (config == null)
        {
            Debug.LogWarning($"SOItemConfig: Weapon config not found. key={key}, itemId={Id}");
        }

        return config;
    }

    public GameObject ResolveWeaponPrefab()
    {
        return ResolveWeaponConfig()?.WeaponPrefab;
    }

    public SOContainerConfig GetRuntimeContainerConfig()
    {
        if (!IsContainer || Id <= 0)
        {
            return null;
        }

        if (SOContainerConfig.TryLoadConfigById(Id, out var runtimeConfig))
        {
            return runtimeConfig;
        }

        return null;
    }

    public static string BuildItemConfigKey(int itemId)
    {
        return $"{ItemConfigResPrefix}{itemId}";
    }

    public static bool TryLoadConfigById(int itemId, out SOItemConfig config, bool forceReload = false)
    {
        config = LoadConfigById(itemId, forceReload);
        return config != null;
    }

    public static SOItemConfig LoadConfigById(int itemId, bool forceReload = false)
    {
        if (itemId <= 0)
        {
            return null;
        }

        if (!forceReload && configById.TryGetValue(itemId, out var cached) && cached != null)
        {
            return cached;
        }

        var resLoader = GameArchitecture.Interface.GetUtility<IResLoader>();
        if (resLoader == null)
        {
            Debug.LogWarning($"SOItemConfig: IResLoader is null, cannot resolve item config for itemId={itemId}.");
            return null;
        }

        string key = BuildItemConfigKey(itemId);
        var config = resLoader.LoadSync<SOItemConfig>(key);
        if (config == null)
        {
            return null;
        }

        configById[itemId] = config;
        return config;
    }
}
