using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PlayerInventoryView : MonoBehaviour, IController
{

    private Transform equipmentSlotRoot;
    private Transform pocketRoot;
    public RectTransform chestRoot;
    public RectTransform backpackRoot;

    private EquipmentView equipmentView;
    private ContainerView chest;
    private ContainerView backpack;

    private ContainerView pocket;

    public void InitPlayerInventory()
    {
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return;

        if (chest == null)
        {
            chest = CreateContainerView(
                model.GetPlayerContainerName(InventoryContainerType.ChestRig),
                chestRoot);
        }
        if (backpack == null)
        {
            backpack = CreateContainerView(
                model.GetPlayerContainerName(InventoryContainerType.Backpack),
                backpackRoot);
        }

        if (chest != null)
        {
            chest.containerId = model.GetPlayerContainerId(InventoryContainerType.ChestRig);

            chestRoot.sizeDelta = new Vector2(chestRoot.sizeDelta.x, (chest.transform as RectTransform).sizeDelta.y + 10f);
        }
        if (backpack != null)
        {
            backpack.containerId = model.GetPlayerContainerId(InventoryContainerType.Backpack);

            backpackRoot.sizeDelta = new Vector2(backpackRoot.sizeDelta.x, (backpack.transform as RectTransform).sizeDelta.y + 10f);
        }

        pocketRoot = transform.GetChild("Pocket");
        pocket = pocketRoot?.GetComponent<ContainerView>();
        if (pocket != null)
            pocket.containerId = model.GetPlayerContainerId(InventoryContainerType.Pocket);

        equipmentSlotRoot = transform.GetChild("EquipmentSlot");
        equipmentView = equipmentSlotRoot?.GetComponent<EquipmentView>();
        if (equipmentView != null)
        {
            equipmentView.InitEquipment();
        }

    }

    private ContainerView CreateContainerView(string containerName, RectTransform root)
    {
        if (string.IsNullOrEmpty(containerName) || root == null) return null;
        var prefab = this.GetUtility<IResLoader>().LoadSync<GameObject>(containerName);
        if (prefab == null) return null;
        Transform rootObj = root.GetChild("Root");
        var instance = Instantiate(prefab, rootObj);
        return instance.GetComponent<ContainerView>();
    }


    public void BindCallbacks(System.Func<string, int, Vector2Int, bool> tryTake,
                            System.Func<string, int, Vector2Int, bool, bool> tryPlace,
                            System.Func<EquipmentSlotType, bool> tryEquipTake,
                            System.Func<EquipmentSlotType, bool> tryEquipPlace,
                            System.Func<EquipmentSlotType, EquipmentSlotType, bool> tryEquipSwap,
                            System.Func<EquipmentSlotType, ItemInstance> beginEquipDrag,
                            System.Func<EquipmentSlotType, ItemInstance, bool> returnEquipDrag)
    {
        if (chest != null)
        {
            chest.BindCallbacks(tryTake, tryPlace);
        }
        if (backpack != null)
        {
            backpack.BindCallbacks(tryTake, tryPlace);
        }
        if (pocket != null)
        {
            pocket.BindCallbacks(tryTake, tryPlace);
        }

        if (equipmentView != null)
        {
            equipmentView.BindCallbacks(tryEquipTake, tryEquipPlace, tryEquipSwap, beginEquipDrag, returnEquipDrag);
        }

    }






    /// <summary>刷新容器内所有分格。</summary>
    public void RenderAll()
    {
        if (chest != null)
        {
            chest.RenderAll();
        }
        if (backpack != null)
        {
            backpack.RenderAll();
        }
        if (pocket != null)
        {
            pocket.RenderAll();
        }

        if (equipmentView != null)
        {
            equipmentView.RenderAll();
        }

    }







    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
