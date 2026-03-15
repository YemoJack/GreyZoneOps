using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;

public class MapSystem : AbstractSystem, IUpdateSystem
{
    private const float TimeEventInterval = 1f;
    private const float ExtractProgressEventInterval = 0.5f;
    private const float SpawnRetryInterval = 0.5f;
    private const float GroundedStableTime = 0.1f;
    private const string GameRootName = "GameRoot";

    private IGameLoop gameLoop;
    private MapModel mapModel;
    private Transform playerTransform;
    private GameObject playerInstance;
    private PlayerRuntime playerRuntime;
    private bool spawnedPlayer;
    private float spawnRetryTimer;
    private SOMapDefinition spawnedContainersFor;
    private SOMapDefinition spawnedMapFor;
    private GameObject mapRoot;
    private GameObject mapInstance;
    private GameObject extractionEffectsSpawnedForMapInstance;
    private bool mapPrefabReady;
    private bool mapLoadInProgress;
    private int mapLoadSerial;
    private InputSys inputSys;

    private bool useSpawnYaw = true;
    private bool alignToGround = true;
    private LayerMask groundLayers = ~0;
    private float groundRayHeight = 2f;
    private float groundRayDistance = 10f;

    private readonly List<ExtractionRuntime> extractionPoints = new List<ExtractionRuntime>();
    private ExtractionRuntime activeExtraction;
    private float lastTimeEvent = -1f;

    protected override void OnInit()
    {
        gameLoop = this.GetUtility<IGameLoop>();
        gameLoop.Register(this);

        mapModel = this.GetModel<MapModel>();
        inputSys = this.GetSystem<InputSys>();
        this.RegisterEvent<EventGameFlowStateChanged>(OnGameFlowStateChanged);
        BuildExtractionRuntime();
    }

    public void LoadMap(SOMapDefinition definition)
    {
        mapModel.SetMap(definition);
        BuildExtractionRuntime();
        spawnedContainersFor = null;
        spawnedMapFor = null;
        extractionEffectsSpawnedForMapInstance = null;
        mapPrefabReady = false;
        mapLoadInProgress = false;
        StartMapLoadAsync(definition, false);
        this.SendEvent(new EventMapLoaded { Map = definition });
        ResetSpawnRuntimeState();
    }

    public void BeginRaid()
    {
        ResetSpawnRuntimeState();

        if (mapModel == null)
        {
            mapModel = this.GetModel<MapModel>();
        }

        if (mapModel.CurrentMap == null)
        {
            mapModel.LoadDefaultMap();
        }

        if (mapModel.CurrentMap == null)
        {
            Debug.LogWarning("MapSystem: BeginRaid failed, map definition is null.");
            return;
        }

        BuildExtractionRuntime();
        SetState(MapState.InRaid);
        if (spawnedMapFor != mapModel.CurrentMap)
        {
            spawnedContainersFor = null;
            spawnedMapFor = null;
            extractionEffectsSpawnedForMapInstance = null;
            mapPrefabReady = false;
        }
        StartMapLoadAsync(mapModel.CurrentMap, true);
        this.SendEvent(new EventMapLoaded { Map = mapModel.CurrentMap });
    }

    public void EndRaid()
    {
        SetState(MapState.Ended);
        ResetSpawnRuntimeState();
    }

    public void OnUpdate(float deltaTime)
    {
        if (mapModel == null || mapModel.State != MapState.InRaid)
        {
            return;
        }

        if (!spawnedPlayer)
        {
            spawnRetryTimer += deltaTime;
            if (spawnRetryTimer >= SpawnRetryInterval)
            {
                spawnRetryTimer = 0f;
                TrySpawnPlayer("Retry");
            }
        }
        else if (playerRuntime != null)
        {
            playerRuntime.TickInputLock(deltaTime, GroundedStableTime, groundLayers);
        }

        mapModel.AddTime(deltaTime);
        DispatchTimeChanged();

        if (mapModel.RaidDuration > 0f && mapModel.RaidElapsed >= mapModel.RaidDuration)
        {
            SetState(MapState.Ended);
            return;
        }

        UpdateExtraction(deltaTime);
    }

    private void BuildExtractionRuntime()
    {
        extractionPoints.Clear();
        activeExtraction = null;

        if (mapModel == null || mapModel.CurrentMap == null)
        {
            return;
        }

        if (mapModel.CurrentMap.extractionPoints == null)
        {
            return;
        }

        foreach (var def in mapModel.CurrentMap.extractionPoints)
        {
            var runtime = new ExtractionRuntime(def);
            extractionPoints.Add(runtime);
        }
    }

    private void OnGameFlowStateChanged(EventGameFlowStateChanged e)
    {
        if (e.Current == GameFlowState.InRaid)
        {
            return;
        }

        ResetSpawnRuntimeState();
    }

    private void ResetSpawnRuntimeState()
    {
        spawnedPlayer = false;
        spawnRetryTimer = 0f;
        playerTransform = null;
        playerRuntime = null;
        playerInstance = null;
        activeExtraction = null;
        lastTimeEvent = -1f;
    }

    private void TrySpawnPlayer(string reason)
    {
        if (spawnedPlayer)
        {
            return;
        }

        if (!mapPrefabReady)
        {
            if (!mapLoadInProgress)
            {
                Debug.LogWarning($"MapSystem: Spawn failed, map prefab not ready. reason={reason}");
            }
            return;
        }

        if (mapModel == null || mapModel.CurrentMap == null)
        {
            Debug.LogWarning($"MapSystem: Spawn failed, map is null. reason={reason}");
            return;
        }

        if (!TrySelectSpawnPoint(out var spawnPoint))
        {
            Debug.LogWarning($"MapSystem: Spawn failed, spawnPoints empty. reason={reason}");
            return;
        }

        if (!TryResolvePlayerTransform(spawnPoint, out var playerRoot))
        {
            Debug.LogWarning($"MapSystem: Spawn failed, player not found. reason={reason}");
            return;
        }

        playerRuntime = EnsurePlayerRuntime(playerRoot);
        if (playerRuntime != null)
        {
            playerRuntime.InitializeRuntime(inputSys);
            playerRuntime.AttachCamera(Camera.main);
            playerRuntime.Teleport(spawnPoint.Position, spawnPoint.Yaw, useSpawnYaw, alignToGround, groundLayers, groundRayHeight, groundRayDistance);
            playerRuntime.OnSpawned();
        }
        else
        {
            playerRoot.position = spawnPoint.Position;
        }

        playerTransform = playerRoot;
        spawnedPlayer = true;
        NotifyPlayerSpawnedAsync(mapModel.CurrentMap, playerRoot, playerRuntime).Forget();
        Debug.Log($"MapSystem: Player spawned at {spawnPoint.Position} (id={spawnPoint.SpawnId}, region={spawnPoint.RegionId}) reason={reason}");
    }

    private async UniTask NotifyPlayerSpawnedAsync(SOMapDefinition definition, Transform playerRoot, PlayerRuntime runtime)
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (playerRoot == null)
        {
            return;
        }

        this.SendEvent(new EventPlayerSpawned
        {
            Map = definition,
            PlayerTransform = playerRoot,
            PlayerRuntime = runtime
        });

        if (mapInstance != null)
        {
            this.SendEvent(new EventMapReady
            {
                Map = definition,
                MapRoot = mapInstance,
                PlayerTransform = playerRoot
            });
        }
    }

    private void StartMapLoadAsync(SOMapDefinition definition, bool spawnPlayerAfterLoad)
    {
        if (definition == null)
        {
            return;
        }

        if (spawnedMapFor == definition && mapInstance != null)
        {
            mapPrefabReady = true;
            mapLoadInProgress = false;
            EnsureSceneContainersSpawned(definition);
            EnsureExtractionEffectsSpawned(definition);
            NotifyMapPrefabReady(definition);
            if (spawnPlayerAfterLoad)
            {
                TrySpawnPlayer("MapLoaded");
            }
            return;
        }

        mapLoadSerial++;
        mapLoadInProgress = true;
        mapPrefabReady = false;
        LoadMapPrefabAndSetupAsync(definition, mapLoadSerial, spawnPlayerAfterLoad).Forget();
    }

    private async UniTask LoadMapPrefabAndSetupAsync(SOMapDefinition definition, int loadSerial, bool spawnPlayerAfterLoad)
    {
        var prefabName = definition.mapResName;
        if (string.IsNullOrEmpty(prefabName))
        {
            if (loadSerial == mapLoadSerial)
            {
                mapLoadInProgress = false;
            }
            Debug.LogWarning("MapSystem: Map prefab name is empty.");
            return;
        }

        var prefab = await this.GetUtility<IResLoader>().LoadAsync<GameObject>(prefabName);
        if (loadSerial != mapLoadSerial)
        {
            return;
        }

        if (prefab == null)
        {
            mapLoadInProgress = false;
            Debug.LogWarning($"MapSystem: Map prefab not found: {prefabName}");
            return;
        }

        if (mapInstance != null)
        {
            Object.Destroy(mapInstance);
            mapInstance = null;
            extractionEffectsSpawnedForMapInstance = null;
        }

        var root = GetOrCreateGameRoot();
        mapInstance = Object.Instantiate(prefab, root.transform);
        mapInstance.name = prefab.name;
        spawnedMapFor = definition;
        await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        Physics.SyncTransforms();
        EnsureSceneContainersSpawned(definition);
        EnsureExtractionEffectsSpawned(definition);
        mapPrefabReady = true;
        mapLoadInProgress = false;
        NotifyMapPrefabReady(definition);
        if (spawnPlayerAfterLoad)
        {
            TrySpawnPlayer("MapLoadedAsync");
        }
    }

    private void NotifyMapPrefabReady(SOMapDefinition definition)
    {
        if (mapInstance == null)
        {
            return;
        }

        this.SendEvent(new EventMapPrefabReady
        {
            Map = definition,
            MapRoot = mapInstance
        });
    }

    private bool EnsureSceneContainersSpawned(SOMapDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (spawnedContainersFor == definition)
        {
            return true;
        }

        if (definition.sceneContainers == null || definition.sceneContainers.Count == 0)
        {
            spawnedContainersFor = definition;
            return false;
        }

        var model = this.GetModel<InventoryContainerModel>();
        if (model == null)
        {
            Debug.LogError("MapSystem: InventoryContainerModel is null.");
            return false;
        }

        SpawnFromMapDefinition(model, definition);
        spawnedContainersFor = definition;
        return true;
    }

    private bool EnsureExtractionEffectsSpawned(SOMapDefinition definition)
    {
        if (definition == null || mapInstance == null)
        {
            return false;
        }

        if (extractionEffectsSpawnedForMapInstance == mapInstance)
        {
            return true;
        }

        if (definition.extractionPoints != null)
        {
            foreach (var point in definition.extractionPoints)
            {
                if (point.ExtractionEffectPrefab == null)
                {
                    continue;
                }

                var fx = Object.Instantiate(
                    point.ExtractionEffectPrefab,
                    point.Position,
                    Quaternion.identity,
                    mapInstance.transform);
                if (!string.IsNullOrEmpty(point.ExtractionId))
                {
                    fx.name = $"{point.ExtractionEffectPrefab.name}_{point.ExtractionId}";
                }
            }
        }

        extractionEffectsSpawnedForMapInstance = mapInstance;
        return true;
    }

    private GameObject GetOrCreateGameRoot()
    {
        if (mapRoot == null)
        {
            mapRoot = GameObject.Find(GameRootName);
        }

        if (mapRoot == null)
        {
            mapRoot = new GameObject(GameRootName);
        }

        return mapRoot;
    }

    private void SpawnFromMapDefinition(InventoryContainerModel model, SOMapDefinition definition)
    {
        foreach (var entry in definition.sceneContainers)
        {
            if (entry == null || entry.prefab == null) continue;

            var container = ResolveContainer(model, entry);
            var pos = entry.position;
            var rot = Quaternion.Euler(entry.rotationEuler);
            var instance = Object.Instantiate(entry.prefab, pos, rot, null);

            if (entry.setInteractableLayer)
            {
                var interactLayer = LayerMask.NameToLayer("Interactable");
                if (interactLayer >= 0)
                {
                    instance.layer = interactLayer;
                }
            }

            var interactable = instance.GetComponent<ContainerInteractable>();
            if (interactable == null)
            {
                interactable = instance.AddComponent<ContainerInteractable>();
            }

            if (container != null)
            {
                interactable.ContainerId = container.InstanceId;
            }


            interactable.FallbackType = entry.fallbackType;
        }
    }

    private static InventoryContainer ResolveContainer(
        InventoryContainerModel model,
        SceneContainerSpawnConfig entry)
    {
        if (model == null || entry == null) return null;

        var containerConfig = entry.ResolveContainerConfig();
        if (containerConfig != null)
        {
            return model.EnsureContainer(containerConfig);
        }

        return null;
    }

    private bool TryResolvePlayerTransform(MapSpawnPointDefinition spawnPoint, out Transform playerRoot)
    {
        playerRoot = null;

        if (playerTransform != null)
        {
            playerRoot = playerTransform;
            return true;
        }

        if (playerInstance != null)
        {
            playerRoot = playerInstance.transform;
            return true;
        }

        if (TrySpawnPlayerPrefab(spawnPoint, out playerRoot))
        {
            return true;
        }

        return false;
    }

    private bool TrySelectSpawnPoint(out MapSpawnPointDefinition spawnPoint)
    {
        spawnPoint = default;
        var map = mapModel.CurrentMap;
        if (map == null || map.spawnPoints == null || map.spawnPoints.Count == 0)
        {
            return false;
        }

        spawnPoint = map.spawnPoints[Random.Range(0, map.spawnPoints.Count)];
        return true;
    }

    private bool TrySpawnPlayerPrefab(MapSpawnPointDefinition spawnPoint, out Transform playerRoot)
    {
        playerRoot = null;
        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            Debug.LogWarning("MapSystem: GameSetting not ready, cannot spawn player prefab.");
            return false;
        }

        var prefabName = settings.Config.PlayerPrefabName;
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning("MapSystem: PlayerPrefabName is empty.");
            return false;
        }

        var prefab = this.GetUtility<IResLoader>().LoadSync<GameObject>(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"MapSystem: Player prefab not found: {prefabName}");
            return false;
        }

        var position = spawnPoint.Position;

        playerInstance = Object.Instantiate(prefab, position, Quaternion.identity);
        playerRoot = playerInstance.transform;
        return playerRoot != null;
    }

    private PlayerRuntime EnsurePlayerRuntime(Transform playerRoot)
    {
        if (playerRoot == null)
        {
            return null;
        }

        var runtime = playerRoot.GetComponent<PlayerRuntime>();
        if (runtime == null)
        {
            runtime = playerRoot.gameObject.AddComponent<PlayerRuntime>();
        }

        return runtime;
    }

    private void UpdateExtraction(float deltaTime)
    {
        if (playerTransform == null || extractionPoints.Count == 0)
        {
            return;
        }

        var position = playerTransform.position;
        ExtractionRuntime candidate = null;

        for (int i = 0; i < extractionPoints.Count; i++)
        {
            var runtime = extractionPoints[i];
            if (runtime.IsTriggered(position))
            {
                candidate = runtime;
                break;
            }
        }

        if (candidate == null)
        {
            if (activeExtraction != null)
            {
                CancelExtraction(activeExtraction);
                activeExtraction = null;
            }
            return;
        }

        if (activeExtraction != candidate)
        {
            if (activeExtraction != null)
            {
                CancelExtraction(activeExtraction);
            }
            StartExtraction(candidate);
            activeExtraction = candidate;
        }

        activeExtraction.Progress += deltaTime;
        DispatchExtractionProgress(activeExtraction);

        if (activeExtraction.Definition.ExtractDuration <= 0f || activeExtraction.Progress >= activeExtraction.Definition.ExtractDuration)
        {
            CompleteExtraction(activeExtraction);
            activeExtraction = null;
        }
    }

    private void StartExtraction(ExtractionRuntime runtime)
    {
        runtime.ResetProgress();
        this.SendEvent(new EventExtractionStarted
        {
            ExtractionId = runtime.Definition.ExtractionId,
            Duration = runtime.Definition.ExtractDuration
        });
    }

    private void CancelExtraction(ExtractionRuntime runtime)
    {
        runtime.ResetProgress();
        this.SendEvent(new EventExtractionCancelled
        {
            ExtractionId = runtime.Definition.ExtractionId
        });
    }

    private void CompleteExtraction(ExtractionRuntime runtime)
    {
        this.SendEvent(new EventExtractionSucceeded
        {
            ExtractionId = runtime.Definition.ExtractionId
        });
        SetState(MapState.Ended);
    }

    private void DispatchTimeChanged()
    {

        var second = Mathf.Floor(mapModel.RaidElapsed / TimeEventInterval) * TimeEventInterval;
        if (Mathf.Approximately(second, lastTimeEvent))
        {
            return;
        }

        lastTimeEvent = second;
        this.SendEvent(new EventMapTimeChanged
        {
            Elapsed = mapModel.RaidElapsed,
            Remaining = mapModel.RemainingTime,
            Duration = mapModel.RaidDuration
        });
    }

    private void DispatchExtractionProgress(ExtractionRuntime runtime)
    {
        if (runtime.Definition.ExtractDuration <= 0f)
        {
            return;
        }

        var second = Mathf.Floor(runtime.Progress / ExtractProgressEventInterval) * ExtractProgressEventInterval;
        if (Mathf.Approximately(second, runtime.LastProgressEvent))
        {
            return;
        }

        runtime.LastProgressEvent = second;
        this.SendEvent(new EventExtractionProgress
        {
            ExtractionId = runtime.Definition.ExtractionId,
            Progress = runtime.Progress,
            Remaining = Mathf.Max(0f, runtime.Definition.ExtractDuration - runtime.Progress),
            Duration = runtime.Definition.ExtractDuration
        });
    }

    private void SetState(MapState state)
    {
        if (mapModel == null)
        {
            return;
        }

        var previous = mapModel.State;
        if (previous == state)
        {
            return;
        }

        mapModel.SetState(state);
        this.SendEvent(new EventMapStateChanged
        {
            Previous = previous,
            Current = state
        });
    }

    private class ExtractionRuntime
    {
        private readonly IExtractionMatcher matcher;

        public MapExtractionPointDefinition Definition { get; }
        public bool IsActive { get; set; }
        public float Progress { get; set; }
        public float LastProgressEvent { get; set; }

        public ExtractionRuntime(MapExtractionPointDefinition definition)
        {
            Definition = definition;
            matcher = CreateMatcher(definition);
            IsActive = definition.EnabledOnStart;
            Progress = 0f;
            LastProgressEvent = -1f;
        }

        public bool IsTriggered(Vector3 playerPosition)
        {
            return IsActive && matcher != null && matcher.IsMatched(playerPosition);
        }

        public void ResetProgress()
        {
            Progress = 0f;
            LastProgressEvent = -1f;
        }
    }

    private static IExtractionMatcher CreateMatcher(MapExtractionPointDefinition definition)
    {
        switch (definition.ExtractionType)
        {
            case MapExtractionType.Normal:
            default:
                return new TriggerAreaMatcher(CreateTrigger(definition));
        }
    }

    private static IExtractionTrigger CreateTrigger(MapExtractionPointDefinition definition)
    {
        switch (definition.TriggerType)
        {
            case MapExtractionTriggerType.Box:
                return new BoxExtractionTrigger(definition.Position, definition.TriggerBoxSize);
            case MapExtractionTriggerType.Radius:
            default:
                return new RadiusExtractionTrigger(definition.Position, definition.Radius);
        }
    }

    private interface IExtractionMatcher
    {
        bool IsMatched(Vector3 playerPosition);
    }

    private interface IExtractionTrigger
    {
        bool Contains(Vector3 worldPosition);
    }

    private class TriggerAreaMatcher : IExtractionMatcher
    {
        private readonly IExtractionTrigger trigger;

        public TriggerAreaMatcher(IExtractionTrigger trigger)
        {
            this.trigger = trigger;
        }

        public bool IsMatched(Vector3 playerPosition)
        {
            return trigger != null && trigger.Contains(playerPosition);
        }
    }

    private class RadiusExtractionTrigger : IExtractionTrigger
    {
        private readonly Vector3 center;
        private readonly float radiusSqr;

        public RadiusExtractionTrigger(Vector3 center, float radius)
        {
            this.center = center;
            var normalizedRadius = Mathf.Max(0f, radius);
            radiusSqr = normalizedRadius * normalizedRadius;
        }

        public bool Contains(Vector3 worldPosition)
        {
            var offset = worldPosition - center;
            return offset.sqrMagnitude <= radiusSqr;
        }
    }

    private class BoxExtractionTrigger : IExtractionTrigger
    {
        private readonly Bounds bounds;

        public BoxExtractionTrigger(Vector3 center, Vector3 size)
        {
            bounds = new Bounds(center, new Vector3(
                Mathf.Max(0f, size.x),
                Mathf.Max(0f, size.y),
                Mathf.Max(0f, size.z)));
        }

        public bool Contains(Vector3 worldPosition)
        {
            return bounds.Contains(worldPosition);
        }
    }

}
