using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QFramework;
using YooAsset;
using Cysharp.Threading.Tasks;


public class GameLaunch : MonoBehaviour, IController, ICanSendEvent
{
    public static GameLaunch Instance { get; private set; }

    private static bool architectureRegistered;
    private static bool loaderInitialized;

    [Header("Launch")]
    public EPlayMode LaunchMode;
    public const string StartSceneName = "StartScene";
    public const string GameSceneName = "GameScene";

    private IGameLoop updateScheduler;
    private bool bootStarted;
    private bool bootCompleted;
    private bool extractionEventsRegistered;
    private bool isLoadingGameScene;
    private bool hasTriedAutoLoadSave;
    private LoadingWindow loadingWindow;
    private GameFlowSystem gameFlowSystem;

    private Transform testPlayerTransform;
    private int extractionTestIndex;

    public static void RequestStartGame()
    {
        if (Instance == null)
        {
            Debug.LogWarning("GameLaunch: instance not ready.");
            return;
        }

        Instance.StartGameFromHome().Forget();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (!bootStarted)
        {
            bootStarted = true;
            BootAsync().Forget();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private async UniTask BootAsync()
    {
        if (bootCompleted)
        {
            return;
        }

        if (!loaderInitialized)
        {
            await this.GetUtility<IResLoader>().InitLoader(LaunchMode);
            loaderInitialized = true;
        }

        if (!architectureRegistered)
        {
            (GameArchitecture.Interface as GameArchitecture)?.Registor();
            architectureRegistered = true;
        }

        if (gameFlowSystem == null)
        {
            gameFlowSystem = this.GetSystem<GameFlowSystem>();
        }
        gameFlowSystem?.EnterBooting();

        updateScheduler = this.GetUtility<IGameLoop>();

        if (!extractionEventsRegistered)
        {
            RegisterExtractionTestEvents();
            extractionEventsRegistered = true;
        }

        TryAutoLoadSaveData();
        bootCompleted = true;
        EnterSceneFlow(SceneManager.GetActiveScene()).Forget();
    }

    public void StartGame()
    {
        StartGameFromHome().Forget();
    }

    private async UniTask StartGameFromHome()
    {
        if (!bootCompleted || isLoadingGameScene)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name == GameSceneName)
        {
            return;
        }

        this.GetSystem<InventorySystem>()?.PrepareRaidRuntimeInventoryFromCurrentLoadout();

        gameFlowSystem?.EnterLoadingToGame();
        this.GetSystem<InventorySystem>()?.SaveGameData();
        isLoadingGameScene = true;
        UIModule.Instance.DestroyAllWindow();
        loadingWindow = UIModule.Instance.PopUpWindow<LoadingWindow>();
        loadingWindow?.SetProgressWidth(0f);

        var loadOp = SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Single);
        if (loadOp == null)
        {
            CloseLoadingWindow();
            isLoadingGameScene = false;
            Debug.LogError($"GameLaunch: failed to load scene {GameSceneName}.");
            return;
        }

        while (!loadOp.isDone)
        {
            var normalizedProgress = loadOp.progress < 0.9f
                ? Mathf.Clamp01(loadOp.progress / 0.9f)
                : 1f;
            loadingWindow?.SetProgress01(normalizedProgress);
            await UniTask.Yield();
        }

        loadingWindow?.SetProgressWidth(1000f);
        await UniTask.Delay(1000, DelayType.UnscaledDeltaTime);
        CloseLoadingWindow();

        isLoadingGameScene = false;
    }

    private void CloseLoadingWindow()
    {
        if (loadingWindow == null)
        {
            return;
        }

        if (loadingWindow.gameObject != null)
        {
            UIModule.Instance.DestroyWinodw<LoadingWindow>();
        }
        loadingWindow = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!bootCompleted)
        {
            return;
        }

        EnterSceneFlow(scene).Forget();
    }

    private async UniTask EnterSceneFlow(Scene scene)
    {
        UIModule.Instance.Initialize();

        if (scene.name == StartSceneName)
        {
            gameFlowSystem?.EnterStartMenu();
            UIModule.Instance.PopUpWindow<HomeWindow>();
            return;
        }

        if (scene.name != GameSceneName)
        {
            return;
        }

        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

        var mapBootstrap = FindObjectOfType<MapSceneBootstrap>();
        if (mapBootstrap != null)
        {
            mapBootstrap.StartMap();
        }
        else
        {
            this.GetSystem<MapSystem>()?.BeginRaid();
        }

        UIModule.Instance.PopUpWindow<GameWindow>();
    }



    // Update is called once per frame
    void Update()
    {
        if (updateScheduler != null)
            updateScheduler.Tick(Time.deltaTime);

        HandlePauseWindowHotkey();



        TestInventoryWindow();
        TestPlayerHealth();
        TestSaveLoad();
        TestExtraction();
    }


    #region 测试代码

    private void HandlePauseWindowHotkey()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != GameSceneName)
        {
            return;
        }

        if (PauseWindow.IsWindowVisible)
        {
            if (SettingWindow.IsWindowVisible)
            {
                UIModule.Instance.HideWindow<SettingWindow>();
                return;
            }

            UIModule.Instance.HideWindow<PauseWindow>();
            return;
        }

        var inputSys = this.GetSystem<InputSys>();
        if (inputSys != null && !inputSys.InputEnabled)
        {
            return;
        }

        UIModule.Instance.PopUpWindow<PauseWindow>();
    }

    private string containerId = "1003";

    private void TestInventoryWindow()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySpawnTestItemAtIndex(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TrySpawnTestItemAtIndex(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TrySpawnTestItemAtIndex(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TrySpawnTestItemAtIndex(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TrySpawnTestItemAtIndex(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            TrySpawnTestItemAtIndex(5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TrySpawnTestItemAtIndex(6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            TrySpawnTestItemAtIndex(7);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TrySpawnTestItemAtIndex(8);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            TrySpawnTestItemAtIndex(9);
        }
    }

    private void TrySpawnTestItemAtIndex(int index)
    {
        var raidInventorySystem = this.GetSystem<InventorySystem>();
        if (raidInventorySystem == null)
        {
            return;
        }

        var config = GameSettingManager.Instance?.Config;
        var catalog = config?.ItemCatalog;
        var entries = GetTestItemEntries();
        if (catalog == null || entries == null || index < 0 || index >= entries.Count || entries[index] == null)
        {
            Debug.LogWarning($"TestInventoryWindow: item catalog entry index out of range: {index}.");
            return;
        }

        var itemInstance = new ItemInstance(entries[index]);
        bool isok = raidInventorySystem.TryAutoPlace(containerId, itemInstance);
        Debug.Log($"TryAutoPlace {isok}");
    }

    private IReadOnlyList<ItemCatalogEntry> GetTestItemEntries()
    {
        var config = GameSettingManager.Instance?.Config;
        if (config == null || config.ItemCatalog == null)
        {
            return null;
        }

        return config.ItemCatalog.GetEntries();
    }

    private void TestPlayerHealth()
    {
        var healthSystem = this.GetSystem<HealthSystem>();
        var health = healthSystem?.GetPlayerHealth();
        if (health == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            healthSystem.ApplyDamage(10f);
            Debug.Log($"Player HP -10 => {health.CurrentHealth}/{health.MaxHealth}");
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            healthSystem.Heal(10f);
            Debug.Log($"Player HP +10 => {health.CurrentHealth}/{health.MaxHealth}");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            healthSystem.ResetHealth();
            Debug.Log($"Player HP reset => {health.CurrentHealth}/{health.MaxHealth}");
        }
    }

    private void TestSaveLoad()
    {
        var raidInventorySystem = this.GetSystem<InventorySystem>();
        if (raidInventorySystem == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            bool ok = raidInventorySystem.SaveGameData();
            Debug.Log($"SaveGameData => {ok}");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            bool ok = raidInventorySystem.LoadGameData();
            Debug.Log($"LoadGameData => {ok}");
        }
    }

    private void RegisterExtractionTestEvents()
    {
        this.RegisterEvent<EventPlayerSpawned>(OnPlayerSpawned).UnRegisterWhenGameObjectDestroyed(this);
        this.RegisterEvent<EventExtractionStarted>(OnExtractionStarted).UnRegisterWhenGameObjectDestroyed(this);
        this.RegisterEvent<EventExtractionProgress>(OnExtractionProgress).UnRegisterWhenGameObjectDestroyed(this);
        this.RegisterEvent<EventExtractionCancelled>(OnExtractionCancelled).UnRegisterWhenGameObjectDestroyed(this);
        this.RegisterEvent<EventExtractionSucceeded>(OnExtractionSucceeded).UnRegisterWhenGameObjectDestroyed(this);
    }

    private void TestExtraction()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            DumpExtractionPoints();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            TeleportPlayerToNextExtractionPoint();
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            TeleportPlayerOutsideExtractionRange();
        }
    }

    private void OnPlayerSpawned(EventPlayerSpawned e)
    {
        testPlayerTransform = e.PlayerTransform;
    }

    private void OnExtractionStarted(EventExtractionStarted e)
    {
        Debug.Log($"[ExtractionTest] Started => id={e.ExtractionId}, duration={e.Duration:0.00}s");
    }

    private void OnExtractionProgress(EventExtractionProgress e)
    {
        //Debug.Log($"[ExtractionTest] Progress => id={e.ExtractionId}, remaining={e.Remaining:0.00}s, progress={e.Progress:0.00}/{e.Duration:0.00}");
    }

    private void OnExtractionCancelled(EventExtractionCancelled e)
    {
        Debug.Log($"[ExtractionTest] Cancelled => id={e.ExtractionId}");
    }

    private void OnExtractionSucceeded(EventExtractionSucceeded e)
    {
        Debug.Log($"[ExtractionTest] Succeeded => id={e.ExtractionId}");
        TrySaveGameData("extraction_succeeded");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            TrySaveGameData("application_pause");
        }
    }

    private void OnApplicationQuit()
    {
        TrySaveGameData("application_quit");
    }

    private void TryAutoLoadSaveData()
    {
        if (hasTriedAutoLoadSave)
        {
            return;
        }

        hasTriedAutoLoadSave = true;
        var raidInventorySystem = this.GetSystem<InventorySystem>();
        if (raidInventorySystem == null)
        {
            Debug.LogWarning("GameLaunch: auto load skipped, raidInventorySystem is null.");
            return;
        }

        bool loaded = raidInventorySystem.LoadGameData();
        Debug.Log($"GameLaunch: auto load save => {loaded}");
    }

    private bool TrySaveGameData(string reason)
    {
        var raidInventorySystem = this.GetSystem<InventorySystem>();
        if (raidInventorySystem == null)
        {
            Debug.LogWarning($"GameLaunch: auto save skipped, raidInventorySystem is null. reason={reason}");
            return false;
        }

        bool ok = raidInventorySystem.SaveGameData();
        Debug.Log($"GameLaunch: auto save => {ok}, reason={reason}");
        return ok;
    }

    private void DumpExtractionPoints()
    {
        var map = this.GetModel<MapModel>()?.CurrentMap;
        if (map == null)
        {
            Debug.LogWarning("[ExtractionTest] Current map is null.");
            return;
        }

        if (map.extractionPoints == null || map.extractionPoints.Count == 0)
        {
            Debug.LogWarning("[ExtractionTest] Map has no extraction points.");
            return;
        }

        for (int i = 0; i < map.extractionPoints.Count; i++)
        {
            var p = map.extractionPoints[i];
            Debug.Log($"[ExtractionTest] #{i} id={p.ExtractionId}, name={p.DisplayName}, enabled={p.EnabledOnStart}, type={p.ExtractionType}, trigger={p.TriggerType}, pos={p.Position}, radius={p.Radius:0.00}, box={p.TriggerBoxSize}, duration={p.ExtractDuration:0.00}s");
        }
    }

    private void TeleportPlayerToNextExtractionPoint()
    {
        if (!TryGetTestPlayerTransform(out var player))
        {
            Debug.LogWarning("[ExtractionTest] Player transform not found.");
            return;
        }

        var map = this.GetModel<MapModel>()?.CurrentMap;
        if (map == null || map.extractionPoints == null || map.extractionPoints.Count == 0)
        {
            Debug.LogWarning("[ExtractionTest] No extraction points available.");
            return;
        }

        if (extractionTestIndex < 0 || extractionTestIndex >= map.extractionPoints.Count)
        {
            extractionTestIndex = 0;
        }

        var point = map.extractionPoints[extractionTestIndex];
        player.position = point.Position;
        Debug.Log($"[ExtractionTest] Teleport player to extraction #{extractionTestIndex}: id={point.ExtractionId}, enabled={point.EnabledOnStart}, trigger={point.TriggerType}");

        extractionTestIndex = (extractionTestIndex + 1) % map.extractionPoints.Count;
    }

    private void TeleportPlayerOutsideExtractionRange()
    {
        if (!TryGetTestPlayerTransform(out var player))
        {
            Debug.LogWarning("[ExtractionTest] Player transform not found.");
            return;
        }

        var map = this.GetModel<MapModel>()?.CurrentMap;
        if (map == null || map.extractionPoints == null || map.extractionPoints.Count == 0)
        {
            player.position += Vector3.right * 10f;
            Debug.Log("[ExtractionTest] Move player away by +X 10 (no extraction config).");
            return;
        }

        var nearest = map.extractionPoints[0];
        var nearestDist = (player.position - nearest.Position).sqrMagnitude;
        for (int i = 1; i < map.extractionPoints.Count; i++)
        {
            var candidate = map.extractionPoints[i];
            var dist = (player.position - candidate.Position).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearest = candidate;
                nearestDist = dist;
            }
        }

        var direction = player.position - nearest.Position;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        var triggerExtent = nearest.TriggerType == MapExtractionTriggerType.Box
            ? Mathf.Max(Mathf.Abs(nearest.TriggerBoxSize.x), Mathf.Max(Mathf.Abs(nearest.TriggerBoxSize.y), Mathf.Abs(nearest.TriggerBoxSize.z))) * 0.5f
            : Mathf.Max(0f, nearest.Radius);

        var safeDistance = triggerExtent + 8f;
        player.position = nearest.Position + direction.normalized * safeDistance;
        Debug.Log($"[ExtractionTest] Teleport player out of extraction range. nearest={nearest.ExtractionId}, safeDistance={safeDistance:0.00}");
    }

    private bool TryGetTestPlayerTransform(out Transform player)
    {
        player = testPlayerTransform;
        if (player != null)
        {
            return true;
        }

        var runtime = FindObjectOfType<PlayerRuntime>();
        if (runtime != null)
        {
            player = runtime.transform;
            testPlayerTransform = player;
            return true;
        }

        return false;
    }


    #endregion



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
