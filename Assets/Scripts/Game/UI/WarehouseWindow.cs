using QFramework;
using UnityEngine;
using ZMUIFrameWork;

public class WarehouseWindow : WindowBase, IController, ICanSendEvent, IEquipmentDragHost
{
    public WarehouseWindowDataComponent dataCompt;

    private InventorySystem raidInventorySystem;
    private InputSys inputSys;

    private ItemInstance draggingItem;
    private string draggingOriginId;
    private int draggingOriginPartIndex;
    private ItemPlacement draggingOriginPlacement;
    private EquipmentSlotType? draggingOriginEquipSlot;
    private IUnRegister inventoryChangedUnreg;

    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<WarehouseWindowDataComponent>();
        dataCompt.InitComponent(this);

        raidInventorySystem = this.GetSystem<InventorySystem>();
        inputSys = this.GetSystem<InputSys>();
        base.OnAwake();
    }

    public override void OnShow()
    {
        base.OnShow();
        SetCursorVisible(true);
        inputSys?.SetInputEnabled(false);

        InitViews();
        BindGridCallbacks();
        RegisterInventoryEvents();
        RefreshAll();
    }

    public override void OnHide()
    {
        UnregisterInventoryEvents();
        raidInventorySystem?.SaveGameData();
        ClearDraggingItem();
        UIModule.Instance.HideWindow<SelectItemWindow>();
        base.OnHide();
    }

    public override void OnDestroy()
    {
        UnregisterInventoryEvents();
        UIModule.Instance.HideWindow<SelectItemWindow>();
        base.OnDestroy();
    }

    public void OnCloseButtonClick()
    {
        HideWindow();
    }

    private void InitViews()
    {
        dataCompt?.PlayerInventoryPlayerInventoryView?.InitPlayerInventory();
        dataCompt?.WarehouseWarehouseContainerView?.InitWarehouseContainer();
    }

    private void BindGridCallbacks()
    {
        dataCompt?.PlayerInventoryPlayerInventoryView?.BindCallbacks(
            (containerId, partIndex, pos) => HandleTryTake(containerId, partIndex, pos),
            (containerId, partIndex, pos, rotated) => HandleTryPlace(containerId, partIndex, pos, rotated),
            slot => HandleTryEquipTake(slot),
            slot => HandleTryEquipPlace(slot),
            (fromSlot, toSlot) => HandleTryEquipSwap(fromSlot, toSlot),
            slot => HandleEquipBeginDrag(slot),
            (slot, item) => HandleEquipReturn(slot, item));

        dataCompt?.WarehouseWarehouseContainerView?.BindCallbacks(
            (containerId, partIndex, pos) => HandleTryTake(containerId, partIndex, pos),
            (containerId, partIndex, pos, rotated) => HandleTryPlace(containerId, partIndex, pos, rotated));
    }

    private void RefreshAll()
    {
        dataCompt?.PlayerInventoryPlayerInventoryView?.RenderAll();
        dataCompt?.WarehouseWarehouseContainerView?.RenderAll();
    }

    private void RegisterInventoryEvents()
    {
        inventoryChangedUnreg = this.RegisterEvent<InventoryChangedEvent>(_ => RefreshAll());
    }

    private void UnregisterInventoryEvents()
    {
        inventoryChangedUnreg?.UnRegister();
        inventoryChangedUnreg = null;
    }

    private bool HandleTryTake(string id, int partIndex, Vector2Int pos)
    {
        if (draggingItem != null)
        {
            return false;
        }

        if (!TryGetGridByContainerId(id, partIndex, out InventoryGrid grid) || grid == null)
        {
            return false;
        }

        ItemInstance itemAt = grid.GetItemAt(pos);
        if (itemAt == null)
        {
            return false;
        }

        draggingOriginPlacement = grid.GetPlacement(itemAt);
        draggingOriginId = id;
        draggingOriginPartIndex = partIndex;

        bool takeOk;
        ItemInstance takenItem = null;
        if (IsWarehouseContainer(id))
        {
            takeOk = dataCompt?.WarehouseWarehouseContainerView != null &&
                     dataCompt.WarehouseWarehouseContainerView.TryTakeItemAt(partIndex, pos, out takenItem, notify: false, syncPersistent: false);
        }
        else
        {
            takeOk = raidInventorySystem != null &&
                     raidInventorySystem.TryTakeItemAt(id, pos, out takenItem, notify: false, partIndex: partIndex);
        }

        if (takeOk)
        {
            draggingItem = takenItem;
            return true;
        }

        draggingOriginPlacement = null;
        draggingOriginId = null;
        return false;
    }

    private bool HandleTryPlace(string id, int partIndex, Vector2Int pos, bool rotated)
    {
        if (draggingItem == null)
        {
            return false;
        }

        if (pos.x < 0 || pos.y < 0)
        {
            ReturnDraggingItemToOrigin();
            RefreshAll();
            return false;
        }

        bool originIsWarehouse = IsWarehouseContainer(draggingOriginId);
        bool targetIsWarehouse = IsWarehouseContainer(id);
        bool itemStillExistsAsStandalone = false;

        bool placed = TryPlaceByContainerId(id, draggingItem, pos, rotated, partIndex, notify: false, syncPersistent: false);
        if (placed)
        {
            itemStillExistsAsStandalone = draggingItem != null && draggingItem.Count > 0;

            if (originIsWarehouse && !targetIsWarehouse)
            {
                dataCompt?.WarehouseWarehouseContainerView?.RemoveFromPersistentItems(draggingItem);
            }
            else if (!originIsWarehouse && targetIsWarehouse)
            {
                if (itemStillExistsAsStandalone)
                {
                    dataCompt?.WarehouseWarehouseContainerView?.AddToPersistentItems(draggingItem);
                }
            }
            else if (originIsWarehouse && targetIsWarehouse && !itemStillExistsAsStandalone)
            {
                dataCompt?.WarehouseWarehouseContainerView?.RemovePersistentItemReference(draggingItem, refreshProgress: false);
            }

            draggingItem = null;
            draggingOriginPlacement = null;
            draggingOriginEquipSlot = null;
            RefreshAll();
            return true;
        }

        ReturnDraggingItemToOrigin();
        RefreshAll();
        return false;
    }

    private bool HandleTryEquipTake(EquipmentSlotType slot)
    {
        if (draggingItem != null || raidInventorySystem == null)
        {
            return false;
        }

        if (raidInventorySystem.TryUnequipItem(slot, out ItemInstance item))
        {
            if (TryAutoPlaceToPlayerContainers(item))
            {
                return true;
            }

            // WarehouseWindow is only used out of raid; do not allow discarding here.
            raidInventorySystem.TryEquipItem(slot, item, out _);
            draggingItem = null;
            draggingOriginEquipSlot = null;
            return true;
        }

        return false;
    }

    private bool HandleTryEquipPlace(EquipmentSlotType slot)
    {
        if (draggingItem == null || raidInventorySystem == null)
        {
            return false;
        }

        EquipmentContainer equipment = raidInventorySystem.GetPlayerEquipment();
        if (equipment != null && equipment.GetItem(slot) != null)
        {
            return false;
        }

        bool originIsWarehouse = IsWarehouseContainer(draggingOriginId);
        bool placed = raidInventorySystem.TryEquipItem(slot, draggingItem, out _);
        if (placed)
        {
            if (originIsWarehouse)
            {
                dataCompt?.WarehouseWarehouseContainerView?.RemoveFromPersistentItems(draggingItem);
            }

            draggingItem = null;
            draggingOriginPlacement = null;
            draggingOriginEquipSlot = null;
            draggingOriginId = null;
            draggingOriginPartIndex = 0;
            return true;
        }

        if (draggingOriginEquipSlot.HasValue)
        {
            raidInventorySystem.TryEquipItem(draggingOriginEquipSlot.Value, draggingItem, out _);
            draggingOriginEquipSlot = null;
            draggingItem = null;
            draggingOriginId = null;
            draggingOriginPartIndex = 0;
        }

        return false;
    }

    private bool HandleTryEquipSwap(EquipmentSlotType fromSlot, EquipmentSlotType toSlot)
    {
        if (raidInventorySystem == null)
        {
            return false;
        }

        ItemInstance fromItem = draggingItem;
        bool swapped = raidInventorySystem.TrySwapEquip(fromSlot, toSlot, fromItem);
        if (swapped)
        {
            draggingItem = null;
            draggingOriginEquipSlot = null;
        }

        return swapped;
    }

    private ItemInstance HandleEquipBeginDrag(EquipmentSlotType slot)
    {
        if (draggingItem != null || raidInventorySystem == null)
        {
            return null;
        }

        if (raidInventorySystem.TryUnequipItem(slot, out ItemInstance item))
        {
            draggingItem = item;
            draggingOriginEquipSlot = slot;
            return item;
        }

        return null;
    }

    private bool HandleEquipReturn(EquipmentSlotType slot, ItemInstance item)
    {
        if (raidInventorySystem == null || item == null)
        {
            draggingItem = null;
            draggingOriginEquipSlot = null;
            return false;
        }

        bool placed = raidInventorySystem.TryEquipItem(slot, item, out _);
        draggingItem = null;
        draggingOriginEquipSlot = null;
        return placed;
    }

    private bool TryAutoPlaceToPlayerContainers(ItemInstance item)
    {
        if (item == null || raidInventorySystem == null)
        {
            return false;
        }

        InventoryContainerModel model = this.GetModel<InventoryContainerModel>();
        if (model == null)
        {
            return false;
        }

        string chestId = model.GetPlayerContainerId(InventoryContainerType.ChestRig);
        if (!string.IsNullOrEmpty(chestId) && raidInventorySystem.TryAutoPlace(chestId, item))
        {
            return true;
        }

        string backpackId = model.GetPlayerContainerId(InventoryContainerType.Backpack);
        if (!string.IsNullOrEmpty(backpackId) && raidInventorySystem.TryAutoPlace(backpackId, item))
        {
            return true;
        }

        string pocketId = model.GetPlayerContainerId(InventoryContainerType.Pocket);
        return !string.IsNullOrEmpty(pocketId) && raidInventorySystem.TryAutoPlace(pocketId, item);
    }

    private bool TryGetGridByContainerId(string containerId, int partIndex, out InventoryGrid grid)
    {
        grid = null;
        if (string.IsNullOrEmpty(containerId))
        {
            return false;
        }

        if (IsWarehouseContainer(containerId))
        {
            grid = dataCompt?.WarehouseWarehouseContainerView?.GetGrid(partIndex);
            return grid != null;
        }

        if (raidInventorySystem == null)
        {
            return false;
        }

        grid = raidInventorySystem.GetGrid(containerId, partIndex);
        return grid != null;
    }

    private bool TryPlaceByContainerId(
        string containerId,
        ItemInstance item,
        Vector2Int pos,
        bool rotated,
        int partIndex,
        bool notify,
        bool syncPersistent)
    {
        if (item == null || string.IsNullOrEmpty(containerId))
        {
            return false;
        }

        if (IsWarehouseContainer(containerId))
        {
            return dataCompt?.WarehouseWarehouseContainerView != null &&
                   dataCompt.WarehouseWarehouseContainerView.TryPlaceItem(partIndex, item, pos, rotated, notify, syncPersistent);
        }

        return raidInventorySystem != null &&
               raidInventorySystem.TryPlaceItem(containerId, item, pos, rotated, partIndex);
    }

    private bool IsWarehouseContainer(string containerId)
    {
        return dataCompt?.WarehouseWarehouseContainerView != null &&
               dataCompt.WarehouseWarehouseContainerView.IsRuntimeContainer(containerId);
    }

    private void ReturnDraggingItemToOrigin()
    {
        if (draggingItem == null)
        {
            return;
        }

        if (draggingOriginPlacement != null && !string.IsNullOrEmpty(draggingOriginId))
        {
            TryPlaceByContainerId(
                draggingOriginId,
                draggingItem,
                draggingOriginPlacement.Pos,
                draggingOriginPlacement.Rotated,
                draggingOriginPartIndex,
                notify: false,
                syncPersistent: false);
        }
        else if (draggingOriginEquipSlot.HasValue && raidInventorySystem != null)
        {
            raidInventorySystem.TryEquipItem(draggingOriginEquipSlot.Value, draggingItem, out _);
        }

        draggingItem = null;
        draggingOriginPlacement = null;
        draggingOriginEquipSlot = null;
        draggingOriginId = null;
        draggingOriginPartIndex = 0;
    }

    public void ClearDraggingItem()
    {
        draggingItem = null;
        draggingOriginEquipSlot = null;
        draggingOriginPlacement = null;
        draggingOriginId = null;
        draggingOriginPartIndex = 0;
    }

    public bool TryPlaceEquipItemToContainer(ItemInstance item, string containerId, int partIndex, Vector2Int pos, bool rotated)
    {
        if (item == null || string.IsNullOrEmpty(containerId))
        {
            return false;
        }

        bool targetIsWarehouse = IsWarehouseContainer(containerId);
        bool placed = TryPlaceByContainerId(containerId, item, pos, rotated, partIndex, notify: false, syncPersistent: false);
        if (!placed)
        {
            return false;
        }

        if (targetIsWarehouse && item.Count > 0)
        {
            dataCompt?.WarehouseWarehouseContainerView?.AddToPersistentItems(item);
        }

        draggingItem = null;
        draggingOriginPlacement = null;
        draggingOriginEquipSlot = null;
        RefreshAll();
        return true;
    }

    public void DropItem(ItemInstance item)
    {
        if (raidInventorySystem == null || item == null)
        {
            return;
        }

        // WarehouseWindow is an out-of-raid UI. Dragging equipment to empty space should cancel the drag,
        // not discard the item into the world.
        ReturnDraggingItemToOrigin();
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    public void OnTidyButtonClick()
    {
        if (draggingItem != null)
        {
            ReturnDraggingItemToOrigin();
        }

        dataCompt?.WarehouseWarehouseContainerView?.TryTidyItems();
        RefreshAll();
    }
}
