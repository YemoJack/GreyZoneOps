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
[System.Serializable]
public class ItemCatalogEntry
{
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

    [Header("Container Extra")]
    public int ContainerConfigId = -1;

    public bool IsWeapon => Category == ItemCategory.Weapon;

    public SOWeaponConfigBase ResolveWeaponConfig()
    {
        if (!IsWeapon || Id <= 0)
        {
            return null;
        }

        var itemCatalog = GameSettingManager.Instance?.Config?.ItemCatalog;
        if (itemCatalog == null)
        {
            return null;
        }

        return itemCatalog.LoadWeaponConfigByItemId(Id);
    }

    public GameObject ResolveWeaponPrefab()
    {
        return ResolveWeaponConfig()?.WeaponPrefab;
    }

    public bool IsRotatable => Size.x != Size.y;

    public bool IsContainer =>
        ContainerConfigId > 0 ||
        Category == ItemCategory.Backpack ||
        Category == ItemCategory.ChestRig;

    public ContainerCatalogEntry GetRuntimeContainerConfig()
    {
        if (ContainerConfigId <= 0)
        {
            return null;
        }

        var catalog = GameSettingManager.Instance?.Config?.ContainerCatalog;
        if (catalog != null && catalog.TryGetRuntimeConfig(ContainerConfigId, out var runtimeConfig))
        {
            return runtimeConfig;
        }

        return null;
    }
}

[CreateAssetMenu(fileName = "SOItemCatalog", menuName = "InventoryConfig/SOItemCatalog")]
public class SOItemCatalog : ScriptableObject
{
    private const string WeaponConfigResPrefix = "Weapon_";

    [Tooltip("Primary item data source. Each entry is editable in Inspector.")]
    public List<ItemCatalogEntry> Entries = new List<ItemCatalogEntry>();

    private readonly Dictionary<int, ItemCatalogEntry> entryById = new Dictionary<int, ItemCatalogEntry>();
    private readonly Dictionary<int, SOWeaponConfigBase> weaponConfigByItemId = new Dictionary<int, SOWeaponConfigBase>();
    private bool entryIndexBuilt;

    private void OnValidate()
    {
        entryIndexBuilt = false;
        weaponConfigByItemId.Clear();
    }

    public IReadOnlyList<ItemCatalogEntry> GetEntries()
    {
        return Entries;
    }

    public bool TryGetEntryById(int id, out ItemCatalogEntry entry, bool forceRebuild = false)
    {
        if (id <= 0)
        {
            entry = null;
            return false;
        }

        BuildEntryIndexIfNeeded(forceRebuild);
        return entryById.TryGetValue(id, out entry) && entry != null;
    }

    public ContainerCatalogEntry ResolveContainerConfig(ItemCatalogEntry entry)
    {
        if (entry == null || entry.ContainerConfigId <= 0)
        {
            return null;
        }

        var catalog = GetContainerCatalog();
        if (catalog != null &&
            catalog.TryGetRuntimeConfig(entry.ContainerConfigId, out var runtimeConfig))
        {
            return runtimeConfig;
        }

        return null;
    }

    public SOWeaponConfigBase LoadWeaponConfigByItemId(int itemId, bool forceReload = false)
    {
        if (itemId <= 0)
        {
            return null;
        }

        if (!forceReload && weaponConfigByItemId.TryGetValue(itemId, out var cached) && cached != null)
        {
            return cached;
        }

        var resLoader = GameArchitecture.Interface.GetUtility<IResLoader>();
        if (resLoader == null)
        {
            Debug.LogWarning($"SOItemCatalog: IResLoader is null, cannot resolve weapon config for itemId={itemId}.");
            return null;
        }

        string key = BuildWeaponConfigKey(itemId);
        var config = resLoader.LoadSync<SOWeaponConfigBase>(key);
        if (config == null)
        {
            Debug.LogWarning($"SOItemCatalog: Weapon config not found. key={key}, itemId={itemId}");
            return null;
        }

        weaponConfigByItemId[itemId] = config;
        return config;
    }

    public string BuildWeaponConfigKey(int itemId)
    {
        return $"{WeaponConfigResPrefix}{itemId}";
    }

    [ContextMenu("Rebuild Entry Index")]
    public void RebuildEntryIndex()
    {
        entryById.Clear();

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.Id <= 0)
            {
                continue;
            }

            if (!entryById.ContainsKey(entry.Id))
            {
                entryById[entry.Id] = entry;
            }
            else
            {
                Debug.LogWarning($"SOItemCatalog: duplicate item id found: {entry.Id}");
            }
        }

        entryIndexBuilt = true;
    }

    private void BuildEntryIndexIfNeeded(bool forceRebuild)
    {
        if (forceRebuild || !entryIndexBuilt)
        {
            RebuildEntryIndex();
        }
    }

    private SOContainerCatalog GetContainerCatalog()
    {

        if (Application.isPlaying)
        {
            return GameSettingManager.Instance?.Config?.ContainerCatalog;
        }

        return null;
    }
}
