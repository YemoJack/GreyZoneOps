using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ContainerPart
{
    public int partIndex;
    public Vector2Int Size;
}

[CreateAssetMenu(fileName = "Container_0", menuName = "InventoryConfig/Container Config")]
public class SOContainerConfig : ScriptableObject
{
    private const string ContainerConfigResPrefix = "Container_";

    private static readonly Dictionary<int, SOContainerConfig> configById = new Dictionary<int, SOContainerConfig>();

    [Header("Base")]
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

    public string ResourceKey => BuildContainerConfigKey(ContainerId);

    private void OnValidate()
    {
        configById.Clear();

        if (PartGridDatas == null)
        {
            PartGridDatas = new List<ContainerPart>();
            return;
        }

        for (int i = 0; i < PartGridDatas.Count; i++)
        {
            var part = PartGridDatas[i];
            part.Size = new Vector2Int(Mathf.Max(1, part.Size.x), Mathf.Max(1, part.Size.y));
            PartGridDatas[i] = part;
        }
    }

    public static string BuildContainerConfigKey(int containerId)
    {
        return $"{ContainerConfigResPrefix}{containerId}";
    }

    public static bool TryLoadConfigById(int containerId, out SOContainerConfig config, bool forceReload = false)
    {
        config = LoadConfigById(containerId, forceReload);
        return config != null;
    }

    public static SOContainerConfig LoadConfigById(int containerId, bool forceReload = false)
    {
        if (containerId <= 0)
        {
            return null;
        }

        if (!forceReload && configById.TryGetValue(containerId, out var cached) && cached != null)
        {
            return cached;
        }

        var resLoader = GameArchitecture.Interface.GetUtility<IResLoader>();
        if (resLoader == null)
        {
            Debug.LogWarning($"SOContainerConfig: IResLoader is null, cannot resolve container config for containerId={containerId}.");
            return null;
        }

        string key = BuildContainerConfigKey(containerId);
        var config = resLoader.LoadSync<SOContainerConfig>(key);
        if (config == null)
        {
            return null;
        }

        configById[containerId] = config;
        return config;
    }
}
