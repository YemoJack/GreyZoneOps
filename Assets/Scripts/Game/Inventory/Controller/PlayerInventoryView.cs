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
    public Text backpackTotalValueText;

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

        RefreshPlayerTotalValueText();

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

        RefreshPlayerTotalValueText();

    }

    private void RefreshPlayerTotalValueText()
    {
        if (backpackTotalValueText == null)
        {
            return;
        }

        int totalValue = 0;
        var equipment = model?.PlayerEquipment;
        if (equipment != null)
        {
            var countedItemIds = new HashSet<string>();
            var visitedContainerIds = new HashSet<string>();

            foreach (var slotItem in equipment.Slots.Values)
            {
                CollectItemValue(slotItem, countedItemIds, visitedContainerIds, ref totalValue);
            }

            foreach (var container in equipment.Containers.Values)
            {
                CollectContainerValue(container, countedItemIds, visitedContainerIds, ref totalValue);
            }
        }

        backpackTotalValueText.text = $"Total Value: {totalValue}";
    }

    private void CollectItemValue(
        ItemInstance item,
        HashSet<string> countedItemIds,
        HashSet<string> visitedContainerIds,
        ref int totalValue)
    {
        if (item?.Definition == null || string.IsNullOrEmpty(item.InstanceId))
        {
            return;
        }

        if (!countedItemIds.Add(item.InstanceId))
        {
            return;
        }

        int unitValue = Mathf.Max(0, item.Definition.Value);
        int itemCount = Mathf.Max(0, item.Count);
        totalValue += unitValue * itemCount;

        if (item.AttachedContainer != null)
        {
            CollectContainerValue(item.AttachedContainer, countedItemIds, visitedContainerIds, ref totalValue);
        }
    }

    private void CollectContainerValue(
        InventoryContainer container,
        HashSet<string> countedItemIds,
        HashSet<string> visitedContainerIds,
        ref int totalValue)
    {
        if (container == null || string.IsNullOrEmpty(container.InstanceId))
        {
            return;
        }

        if (!visitedContainerIds.Add(container.InstanceId))
        {
            return;
        }

        foreach (var grid in container.PartGrids)
        {
            if (grid == null)
            {
                continue;
            }

            foreach (var placement in grid.GetAllPlacements())
            {
                var item = placement?.Item;
                CollectItemValue(item, countedItemIds, visitedContainerIds, ref totalValue);
            }
        }
    }







    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
