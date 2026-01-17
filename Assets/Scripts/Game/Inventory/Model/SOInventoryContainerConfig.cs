using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SceneContainerSpawnConfig
{
    public SOContainerConfig containerConfig;
    public string containerIdOverride;
    public GameObject prefab;
    public Vector3 position;
    public Vector3 rotationEuler;
    public InventoryContainerType fallbackType = InventoryContainerType.LootBox;
    public bool setInteractableLayer = true;
}

[CreateAssetMenu(fileName = "SOInventoryContainerConfig", menuName = "InventoryConfig/InventoryContainerConfig")]
public class SOInventoryContainerConfig : ScriptableObject
{
    public int mapId;
    public List<SceneContainerSpawnConfig> sceneContainers = new List<SceneContainerSpawnConfig>();
}
