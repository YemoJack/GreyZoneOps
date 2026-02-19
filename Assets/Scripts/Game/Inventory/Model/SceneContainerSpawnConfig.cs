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

    public ContainerCatalogEntry ResolveContainerConfig(SOContainerCatalog catalog)
    {
        if (catalog != null &&
            containerConfigId > 0 &&
            catalog.TryGetRuntimeConfig(containerConfigId, out var runtimeConfig))
        {
            return runtimeConfig;
        }

        Debug.LogError("ContainerCatalogEntry is null");
        return null;
    }
}

