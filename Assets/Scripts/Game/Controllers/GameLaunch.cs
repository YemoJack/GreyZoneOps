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
    private bool extractionSaveRegistered;
    private bool isLoadingGameScene;
    private bool hasTriedAutoLoadSave;
    private LoadingWindow loadingWindow;
    private GameFlowSystem gameFlowSystem;

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

        if (!extractionSaveRegistered)
        {
            this.RegisterEvent<EventExtractionSucceeded>(_ => TrySaveGameData("extraction_succeeded"))
                .UnRegisterWhenGameObjectDestroyed(this);
            extractionSaveRegistered = true;
        }

        updateScheduler = this.GetUtility<IGameLoop>();

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
    }


    #region Runtime

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


    #endregion



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
