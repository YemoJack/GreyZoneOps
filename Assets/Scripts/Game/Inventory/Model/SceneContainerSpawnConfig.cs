using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneContainerSpawnConfig
{
    public int containerConfigId;
    public GameObject prefab;
    public Vector3 position;
    public Vector3 rotationEuler;
    public InventoryContainerType fallbackType = InventoryContainerType.LootBox;
    public bool setInteractableLayer = true;

    public SOContainerConfig ResolveContainerConfig()
    {
        if (containerConfigId > 0 &&
            SOContainerConfig.TryLoadConfigById(containerConfigId, out var runtimeConfig))
        {
            return runtimeConfig;
        }

        Debug.LogError($"SOContainerConfig is null. containerConfigId={containerConfigId}");
        return null;
    }
}
