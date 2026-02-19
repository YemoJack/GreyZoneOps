using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using YooAsset;
using Cysharp.Threading.Tasks;


public class GameLaunch : MonoBehaviour, IController, ICanSendEvent
{
    private IGameLoop updateScheduler;

    public EPlayMode LaunchMode;


    private void Awake()
    {
        UIModule.Instance.Initialize();
        OnStart().Forget();

    }

    async UniTask OnStart()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);

        (GameArchitecture.Interface as GameArchitecture).Registor();

        updateScheduler = this.GetUtility<IGameLoop>();

        var mapBootstrap = FindObjectOfType<MapSceneBootstrap>();
        if (mapBootstrap != null)
        {
            mapBootstrap.StartMap();
        }
        else
        {
            this.GetSystem<MapSystem>()?.BeginRaid();
        }

        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

        UIModule.Instance.Initialize();
        UIModule.Instance.PopUpWindow<GameWindow>();

    }



    // Update is called once per frame
    void Update()
    {
        if (updateScheduler != null)
            updateScheduler.Tick(Time.deltaTime);



        TestInventoryWindow();
        TestPlayerHealth();
        TestSaveLoad();
    }


    #region  测试部分

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
        var inventorySystem = this.GetSystem<InventorySystem>();
        if (inventorySystem == null)
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
        bool isok = inventorySystem.TryAutoPlace(containerId, itemInstance);
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
        var inventorySystem = this.GetSystem<InventorySystem>();
        if (inventorySystem == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            bool ok = inventorySystem.SaveGameData();
            Debug.Log($"SaveGameData => {ok}");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            bool ok = inventorySystem.LoadGameData();
            Debug.Log($"LoadGameData => {ok}");
        }
    }



    #endregion



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
