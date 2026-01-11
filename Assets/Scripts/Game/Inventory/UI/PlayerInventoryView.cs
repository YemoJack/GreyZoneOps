using System.Collections;
using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryView : MonoBehaviour, IController
{

    private Transform equipmentSlotRoot;
    private Transform pocketRoot;
    public RectTransform chestRoot;
    public RectTransform backpackRoot;

    private Vector2 chestOrBackpackSize;

    private EquipmentView equipmentView;
    private ContainerView chestView;
    private ContainerView backpackView;

    private ContainerView pocketView;

    private InventoryContainerModel model;


    System.Func<string, int, Vector2Int, bool> tryTake;
    System.Func<string, int, Vector2Int, bool, bool> tryPlace;
    System.Func<EquipmentSlotType, bool> tryEquipTake;
    System.Func<EquipmentSlotType, bool> tryEquipPlace;
    System.Func<EquipmentSlotType, EquipmentSlotType, bool> tryEquipSwap;
    System.Func<EquipmentSlotType, ItemInstance> beginEquipDrag;
    System.Func<EquipmentSlotType, ItemInstance, bool> returnEquipDrag;

    public void InitPlayerInventory()
    {
        model = this.GetModel<InventoryContainerModel>();
        if (model == null) return;

        chestOrBackpackSize = chestRoot.sizeDelta;
        if (chestView == null)
        {
            if (model.PlayerEquipment.GetContainer(InventoryContainerType.ChestRig) != null)
            {
                chestView = CreateContainerView(
                model.GetPlayerContainerName(InventoryContainerType.ChestRig),
                chestRoot);
                chestView.container = model.PlayerEquipment.GetContainer(InventoryContainerType.ChestRig);
            }


        }
        if (backpackView == null)
        {
            if (model.PlayerEquipment.GetContainer(InventoryContainerType.Backpack) != null)
            {
                backpackView = CreateContainerView(
                model.GetPlayerContainerName(InventoryContainerType.Backpack),
                backpackRoot);
                backpackView.container = model.PlayerEquipment.GetContainer(InventoryContainerType.Backpack);
            }
        }

        if (chestView != null)
        {
            LayoutElement layoutElement = chestRoot.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = (chestView.transform as RectTransform).sizeDelta.y + 10f;
        }
        if (backpackView != null)
        {

            LayoutElement layoutElement = backpackRoot.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = (backpackView.transform as RectTransform).sizeDelta.y + 10f;
        }

        pocketRoot = transform.GetChild("Pocket");
        pocketView = pocketRoot?.GetComponent<ContainerView>();
        if (model.PlayerEquipment.GetContainer(InventoryContainerType.Pocket) != null)
        {
            pocketView.container = model.PlayerEquipment.GetContainer(InventoryContainerType.Pocket);
        }


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
        if (chestView != null)
        {
            chestView.BindCallbacks(tryTake, tryPlace);
        }
        if (backpackView != null)
        {
            backpackView.BindCallbacks(tryTake, tryPlace);
        }
        if (pocketView != null)
        {
            pocketView.BindCallbacks(tryTake, tryPlace);
        }

        if (equipmentView != null)
        {
            equipmentView.BindCallbacks(tryEquipTake, tryEquipPlace, tryEquipSwap, beginEquipDrag, returnEquipDrag);
        }

        this.tryTake = tryTake;
        this.tryPlace = tryPlace;
        this.tryEquipTake = tryEquipTake;
        this.tryEquipPlace = tryEquipPlace;
        this.tryEquipSwap = tryEquipSwap;
        this.beginEquipDrag = beginEquipDrag;
        this.returnEquipDrag = returnEquipDrag;
    }



    /// <summary>刷新容器内所有分格。</summary>
    public void RenderAll()
    {

        if (model.PlayerEquipment.GetContainer(InventoryContainerType.ChestRig) != null)
        {
            if (chestView == null)
            {
                chestView = CreateContainerView(model.GetPlayerContainerName(InventoryContainerType.ChestRig), chestRoot);
            }

            chestView.container = model.PlayerEquipment.GetContainer(InventoryContainerType.ChestRig);


            InventoryContainer container = this.GetSystem<InventorySystem>().GetPlayerEquipment().GetContainer(InventoryContainerType.ChestRig);
            chestView.RenderAll(container);

            chestView.BindCallbacks(tryTake, tryPlace);
        }
        else
        {
            if (chestView != null)
            {
                Destroy(chestView.gameObject);
                chestView = null;
                LayoutElement layoutElement = chestRoot.GetComponent<LayoutElement>();
                layoutElement.preferredHeight = chestOrBackpackSize.y;
            }
        }


        if (model.PlayerEquipment.GetContainer(InventoryContainerType.Backpack) != null)
        {
            if (backpackView == null)
            {
                backpackView = CreateContainerView(model.GetPlayerContainerName(InventoryContainerType.Backpack), backpackRoot);
            }

            backpackView.container = model.PlayerEquipment.GetContainer(InventoryContainerType.Backpack);


            InventoryContainer container = this.GetSystem<InventorySystem>().GetPlayerEquipment().GetContainer(InventoryContainerType.Backpack);
            backpackView.RenderAll(container);

            backpackView.BindCallbacks(tryTake, tryPlace);
        }
        else
        {
            if (backpackView != null)
            {
                Destroy(backpackView.gameObject);
                backpackView = null;
                LayoutElement layoutElement = backpackRoot.GetComponent<LayoutElement>();
                layoutElement.preferredHeight = chestOrBackpackSize.y;
            }
        }

        if (chestView != null)
        {
            LayoutElement layoutElement = chestRoot.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = (chestView.transform as RectTransform).sizeDelta.y + 10f;
        }
        if (backpackView != null)
        {

            LayoutElement layoutElement = backpackRoot.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = (backpackView.transform as RectTransform).sizeDelta.y + 10f;
        }



        if (pocketView != null)
        {
            InventoryContainer container = this.GetSystem<InventorySystem>().GetPlayerEquipment().GetContainer(InventoryContainerType.Pocket);
            pocketView.RenderAll(container);
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
