using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MapRegionDefinition
{
    // Unique region id used for spawn/extraction association.
    // 区域唯一ID，用于关联出生点/撤离点。
    public string RegionId;
    // Display name (UI/debug).
    // 显示名称（UI/调试）。
    public string DisplayName;
    // Region bounds center (world position).
    // 区域边界中心（世界坐标）。
    public Vector3 Center;
    // Region bounds size (world size).
    // 区域边界尺寸（世界坐标）。
    public Vector3 Size;
}

[Serializable]
public struct MapSpawnPointDefinition
{
    // Unique spawn point id.
    // 出生点唯一ID。
    public string SpawnId;
    // Region id (optional).
    // 所属区域ID（可选）。
    public string RegionId;
    // Spawn position (world).
    // 出生点位置（世界坐标）。
    public Vector3 Position;
    // Initial yaw in degrees.
    // 初始朝向Yaw（度）。
    public float Yaw;
}

[Serializable]
public struct MapExtractionPointDefinition
{
    // Extraction behavior type.
    // 撤离行为类型。
    public MapExtractionType ExtractionType;
    // Trigger area type.
    // 触发区域类型。
    public MapExtractionTriggerType TriggerType;
    // Unique extraction id.
    // 撤离点唯一ID。
    public string ExtractionId;
    // Display name (UI).
    // 显示名称（UI）。
    public string DisplayName;
    // Region id (optional).
    // 所属区域ID（可选）。
    public string RegionId;
    // Extraction center position (world).
    // 撤离点中心位置（世界坐标）。
    public Vector3 Position;
    // Trigger radius.
    // 触发半径。
    public float Radius;
    // Box trigger size when TriggerType is Box (world size).
    // 当TriggerType为Box时的触发盒尺寸（世界坐标）。
    public Vector3 TriggerBoxSize;
    // Extract duration in seconds.
    // 撤离耗时（秒）。
    public float ExtractDuration;
    // Effect prefab spawned when extraction point is generated.
    // 撤离点生成时实例化的特效预制体。
    public GameObject ExtractionEffectPrefab;
    // Enabled at raid start.
    // 是否在开局激活。
    public bool EnabledOnStart;
}

public enum MapExtractionType
{
    // Standard extraction: enter trigger area and hold for duration.
    // 常规撤离：进入触发区并持续倒计时。
    Normal = 0
}

public enum MapExtractionTriggerType
{
    // Sphere trigger by Position + Radius.
    // 球形触发：Position + Radius。
    Radius = 0,
    // Box trigger by Position + TriggerBoxSize.
    // 盒形触发：Position + TriggerBoxSize。
    Box = 1
}

[CreateAssetMenu(fileName = "SOMapDefinition", menuName = "MapConfig/MapDefinition")]
public class SOMapDefinition : ScriptableObject
{
    // Map id used for loading (Cfg_MapDefinition_{mapId}).
    // 地图ID，用于加载（Cfg_MapDefinition_{mapId}）。
    public int mapId;
    // Map display name (UI).
    // 地图显示名称（UI）。
    public string mapName;
    // Map prefab name (single map prefab per definition).
    // Map预制体名称（单地图单预制体）。
    public string mapResName;

    [Header("Map Bounds")]
    // Map bounds center (world).
    // 地图边界中心（世界坐标）。
    public Vector3 mapCenter;
    // Map bounds size (world).
    // 地图边界尺寸（世界坐标）。
    public Vector3 mapSize = new Vector3(1000f, 50f, 1000f);

    [Header("Raid")]
    // Raid duration in seconds.
    // 战局时长（秒）。
    public float raidDurationSeconds = 1800f;

    [Header("Regions")]
    // Regions list.
    // 区域列表。
    public List<MapRegionDefinition> regions = new List<MapRegionDefinition>();

    [Header("Spawn Points")]
    // Spawn points list.
    // 出生点列表。
    public List<MapSpawnPointDefinition> spawnPoints = new List<MapSpawnPointDefinition>();

    [Header("Extraction Points")]
    // Extraction points list.
    // 撤离点列表。
    public List<MapExtractionPointDefinition> extractionPoints = new List<MapExtractionPointDefinition>();

    [Header("Scene Containers")]
    // Scene container configs.
    // 场景容器配置。
    public List<SceneContainerSpawnConfig> sceneContainers = new List<SceneContainerSpawnConfig>();
}
