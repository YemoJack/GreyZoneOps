
using UnityEngine;
using YooAsset;
using Cysharp.Threading.Tasks;
using QFramework;
using System.Collections.Generic;
public class GridMain : MonoBehaviour, IController
{
    public EPlayMode LaunchMode;


    // Start is called before the first frame update

    public List<SOItemDefinition> itemDataList;

    private void Awake()
    {
        UIModule.Instance.Initialize();
        OnStart().Forget();

    }
    async UniTask OnStart()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);

        // var updatedRemote = await this.GetUtility<IResLoader>().UpdateRes((progress, desc) =>
        // {

        // });

        (GameArchitecture.Interface as GameArchitecture).Registor();

        //this.GetModel<InventoryContainerModel>().LoadContainerConfig();
        InventoryWindow inventoryWindow = UIModule.Instance.PopUpWindow<InventoryWindow>();


    }

    string id = "100";


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[0]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[1]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[2]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[3]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[4]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[5]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[6]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[7]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ItemInstance itemInstance = new ItemInstance(itemDataList[8]);

            bool isok = this.GetSystem<InventorySystem>().TryAutoPlace(id, itemInstance);
            Debug.Log($"TryAutoPlace {isok}");
        }

    }


    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
