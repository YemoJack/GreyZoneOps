using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct ContainerPart
{
    public int partIndex;
    public Vector2Int Size;
}



[System.Serializable]
public class ContainerCatalogEntry
{
    public int ContainerId;
    public string ContainerName;
    public InventoryContainerType ContainerType;
    public List<ContainerPart> PartGridDatas = new List<ContainerPart>();

    public int containerId
    {
        get => ContainerId;
        set => ContainerId = value;
    }

    public string containerName
    {
        get => ContainerName;
        set => ContainerName = value;
    }

    public InventoryContainerType containerType
    {
        get => ContainerType;
        set => ContainerType = value;
    }

    public List<ContainerPart> partGridDatas
    {
        get => PartGridDatas;
        set => PartGridDatas = value ?? new List<ContainerPart>();
    }
}

[CreateAssetMenu(fileName = "SOContainerCatalog", menuName = "InventoryConfig/SOContainerCatalog")]
public class SOContainerCatalog : ScriptableObject
{
    [Tooltip("Primary data source. Each entry is a container row editable in Inspector.")]
    public List<ContainerCatalogEntry> Entries = new List<ContainerCatalogEntry>();

    private readonly List<ContainerCatalogEntry> runtimeEntries = new List<ContainerCatalogEntry>();
    private readonly Dictionary<int, ContainerCatalogEntry> runtimeEntryLookup = new Dictionary<int, ContainerCatalogEntry>();
    private bool runtimeCacheBuilt;

    private void OnValidate()
    {
        runtimeCacheBuilt = false;
    }

    public IReadOnlyList<ContainerCatalogEntry> GetRuntimeConfigs(bool forceRebuild = false)
    {
        EnsureRuntimeCache(forceRebuild);
        return runtimeEntries;
    }

    public bool TryGetRuntimeConfig(int containerId, out ContainerCatalogEntry config, bool forceRebuild = false)
    {
        config = null;
        if (containerId <= 0)
        {
            return false;
        }

        EnsureRuntimeCache(forceRebuild);
        return runtimeEntryLookup.TryGetValue(containerId, out config) && config != null;
    }

    [ContextMenu("Rebuild Runtime Configs")]
    public void BuildRuntimeConfigs()
    {
        runtimeEntries.Clear();
        runtimeEntryLookup.Clear();

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.ContainerId <= 0)
            {
                continue;
            }

            if (runtimeEntryLookup.ContainsKey(entry.ContainerId))
            {
                Debug.LogWarning($"SOContainerCatalog: duplicate container id={entry.ContainerId}, name={entry.ContainerName}");
                continue;
            }

            var config = new ContainerCatalogEntry
            {
                ContainerId = entry.ContainerId,
                ContainerName = entry.ContainerName,
                ContainerType = entry.ContainerType,
                PartGridDatas = new List<ContainerPart>()
            };

            if (entry.PartGridDatas != null)
            {
                for (int partIndex = 0; partIndex < entry.PartGridDatas.Count; partIndex++)
                {
                    var part = entry.PartGridDatas[partIndex];
                    var size = new Vector2Int(Mathf.Max(1, part.Size.x), Mathf.Max(1, part.Size.y));
                    config.partGridDatas.Add(new ContainerPart
                    {
                        partIndex = part.partIndex,
                        Size = size
                    });
                }
            }

            runtimeEntries.Add(config);
            runtimeEntryLookup[config.ContainerId] = config;
        }

        runtimeCacheBuilt = true;
    }

    private void EnsureRuntimeCache(bool forceRebuild)
    {
        if (forceRebuild || !runtimeCacheBuilt)
        {
            BuildRuntimeConfigs();
        }
    }
}
