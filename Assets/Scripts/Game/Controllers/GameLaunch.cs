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
    }


    #region  测试部分

    private string containerId = "1003";
    public List<SOItemDefinition> itemDataList;

    private void TestInventoryWindow()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[0]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[1]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[2]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[3]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[4]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[5]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[6]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[7]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[8]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[9]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(containerId, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
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



    #endregion



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
