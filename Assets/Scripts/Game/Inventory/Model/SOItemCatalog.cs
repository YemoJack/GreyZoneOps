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

    [Header("Weapon Extra")]
    public SOWeaponConfigBase WeaponConfig;
    public GameObject WeaponPrefab;

    public Sprite icon
    {
        get => Icon;
        set => Icon = value;
    }

    public bool IsWeapon =>
        Category == ItemCategory.Weapon || WeaponConfig != null || WeaponPrefab != null;

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
    [Tooltip("Primary item data source. Each entry is editable in Inspector.")]
    public List<ItemCatalogEntry> Entries = new List<ItemCatalogEntry>();
    public SOContainerCatalog ContainerCatalog;
    private readonly Dictionary<int, ItemCatalogEntry> entryById = new Dictionary<int, ItemCatalogEntry>();
    private bool entryIndexBuilt;

    private void OnValidate()
    {
        entryIndexBuilt = false;
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
        if (ContainerCatalog != null)
        {
            return ContainerCatalog;
        }

        if (Application.isPlaying)
        {
            return GameSettingManager.Instance?.Config?.ContainerCatalog;
        }

        return null;
    }
}
