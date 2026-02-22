using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;



public struct InventoryChangedEvent
{
}


public partial class InventorySystem : AbstractSystem, ICanSendCommand
{
    private const int MaxContainerNestDepth = 2;

    private InventoryContainerModel _model;
    private int _raidEntryValue;

    private void NotifyChanged()
    {
        this.SendEvent(new InventoryChangedEvent());
    }

    protected override void OnInit()
    {
        _model = this.GetModel<InventoryContainerModel>();
    }



    public InventoryContainer GetContainer(string id)
        => _model.Containers.TryGetValue(id, out var c) ? c : null;

    public InventoryGrid GetGrid(string id, int partIndex = 0)
    {
        return GetContainer(id)?.GetGrid(partIndex);
    }

    public EquipmentContainer GetPlayerEquipment()
        => _model.GetPlayerEquipment();

    public void ResetRaidRuntimeInventory()
    {
        _raidItemsCommitted = false;
        _raidEntryValue = 0;

        if (_model == null)
        {
            _model = this.GetModel<InventoryContainerModel>();
        }

        if (_model == null)
        {
            return;
        }

        _model.PlayerEquipment = new EquipmentContainer();

        if (_model.Containers == null)
        {
            _model.Containers = new Dictionary<string, InventoryContainer>();
        }
        else
        {
            _model.Containers.Clear();
        }

        foreach (var container in _model.PlayerEquipment.Containers.Values)
        {
            RegisterContainerRecursive(container);
        }

        NotifyChanged();
    }

    public void PrepareRaidRuntimeInventoryFromCurrentLoadout()
    {
        _raidItemsCommitted = false;

        if (_model == null)
        {
            _model = this.GetModel<InventoryContainerModel>();
        }

        if (_model == null)
        {
            return;
        }

        if (_model.PlayerEquipment == null)
        {
            _model.PlayerEquipment = new EquipmentContainer();
        }

        if (_model.Containers == null)
        {
            _model.Containers = new Dictionary<string, InventoryContainer>();
        }
        else
        {
            _model.Containers.Clear();
        }

        foreach (var container in _model.PlayerEquipment.Containers.Values)
        {
            RegisterContainerRecursive(container);
        }

        _raidEntryValue = CalculateEquipmentTotalValue(_model.PlayerEquipment, includeStash: false);
        NotifyChanged();
    }

    public int GetCurrentRaidIncome()
    {
        return GetCurrentRaidNetIncome();
    }

    public int GetCurrentRaidNetIncome()
    {
        return GetCurrentRaidCarriedValue() - Mathf.Max(0, _raidEntryValue);
    }

    public int GetCurrentRaidEntryValue()
    {
        return Mathf.Max(0, _raidEntryValue);
    }

    public int GetCurrentRaidCarriedValue()
    {
        return CalculateEquipmentTotalValue(_model?.PlayerEquipment, includeStash: false);
    }

    public int CalculateEquipmentTotalValue(EquipmentContainer equipment, bool includeStash)
    {
        int totalValue = 0;
        if (equipment == null)
        {
            return 0;
        }

        var countedItemIds = new HashSet<string>();
        var visitedContainerIds = new HashSet<string>();

        foreach (var slotItem in equipment.Slots.Values)
        {
            AccumulateItemValue(slotItem, countedItemIds, visitedContainerIds, ref totalValue);
        }

        foreach (var container in equipment.Containers.Values)
        {
            if (!includeStash && container != null && container.Type == InventoryContainerType.Stash)
            {
                continue;
            }

            AccumulateContainerValue(container, countedItemIds, visitedContainerIds, ref totalValue);
        }

        return Mathf.Max(0, totalValue);
    }

    public int CalculateItemsTotalValue(IEnumerable<ItemInstance> items)
    {
        int totalValue = 0;
        if (items == null)
        {
            return 0;
        }

        var countedItemIds = new HashSet<string>();
        var visitedContainerIds = new HashSet<string>();

        foreach (var item in items)
        {
            AccumulateItemValue(item, countedItemIds, visitedContainerIds, ref totalValue);
        }

        return Mathf.Max(0, totalValue);
    }
    private IEnumerable<InventoryGrid> GetCandidateGrids(string id, int partIndex)
    {
        var container = GetContainer(id);
        if (container == null) yield break;

        if (partIndex >= 0)
        {
            var g = container.GetGrid(partIndex);
            if (g != null) yield return g;
        }
        else
        {
            foreach (var g in container.PartGrids)
                yield return g;
        }
    }

    public bool TryPlaceItem(
        string id,
        ItemInstance item,
        Vector2Int pos,
        bool rotated,
        int partIndex = -1)
    {
        if (!CanPlaceInContainer(id, item)) return false;
        // 杩欓噷鍙互鍔犳垬鏂椾腑闄愬埗銆佸鍣ㄨ鍒欑瓑
        foreach (var grid in GetCandidateGrids(id, partIndex))
        {
            var placed = grid.PlaceOrStack(item, pos, rotated);
            if (placed)
            {
                SetContainerParentIfNeeded(item, id);
                NotifyChanged();
                return true;
            }
        }
        return false;
    }

    public bool TryEquipItem(EquipmentSlotType slot, ItemInstance item, out ItemInstance replaced)
    {
        replaced = null;
        if (item == null || item.Definition == null) return false;
        if (!CanEquip(slot, item.Definition.Category)) return false;
        if (_model.PlayerEquipment == null) return false;

        var equipped = _model.PlayerEquipment.TryEquip(slot, item, out replaced);
        if (equipped) NotifyChanged();
        return equipped;
    }

    public bool TryUnequipItem(EquipmentSlotType slot, out ItemInstance item)
    {
        item = null;
        if (_model.PlayerEquipment == null) return false;
        var unequipped = _model.PlayerEquipment.TryUnequip(slot, out item);
        if (unequipped) NotifyChanged();
        return unequipped;
    }

    public bool TrySwapEquip(EquipmentSlotType fromSlot, EquipmentSlotType toSlot)
    {
        if (_model.PlayerEquipment == null) return false;
        if (fromSlot == toSlot) return false;

        var equipment = _model.PlayerEquipment;
        var fromItem = equipment.GetItem(fromSlot);
        return TrySwapEquipInternal(equipment, fromSlot, toSlot, fromItem);
    }

    public bool TrySwapEquip(EquipmentSlotType fromSlot, EquipmentSlotType toSlot, ItemInstance fromItem)
    {
        if (_model.PlayerEquipment == null) return false;
        if (fromSlot == toSlot) return false;

        var equipment = _model.PlayerEquipment;
        return TrySwapEquipInternal(equipment, fromSlot, toSlot, fromItem);
    }

    private bool TrySwapEquipInternal(
        EquipmentContainer equipment,
        EquipmentSlotType fromSlot,
        EquipmentSlotType toSlot,
        ItemInstance fromItem)
    {
        if (fromItem == null || fromItem.Definition == null) return false;

        var toItem = equipment.GetItem(toSlot);
        if (toItem == null) return false;
        if (!CanEquip(toSlot, fromItem.Definition.Category)) return false;
        if (toItem != null && (toItem.Definition == null || !CanEquip(fromSlot, toItem.Definition.Category)))
            return false;

        equipment.TryEquip(toSlot, fromItem, out _);
        equipment.TryEquip(fromSlot, toItem, out _);
        NotifyChanged();
        return true;
    }

    public void DropItem(ItemInstance item)
    {
        if (item == null) return;
        this.SendCommand(new CmdDropItem(item));
    }

    private bool CanEquip(EquipmentSlotType slot, ItemCategory category)
    {
        switch (slot)
        {
            case EquipmentSlotType.Weapon1:
            case EquipmentSlotType.Weapon2:
            case EquipmentSlotType.Weapon3:
            case EquipmentSlotType.Weapon4:
                return category == ItemCategory.Weapon;
            case EquipmentSlotType.Helmet:
                return category == ItemCategory.helmet;
            case EquipmentSlotType.Armor:
                return category == ItemCategory.Armor;
            case EquipmentSlotType.ChestRig:
                return category == ItemCategory.ChestRig;
            case EquipmentSlotType.Backpack:
                return category == ItemCategory.Backpack;
            default:
                return false;
        }
    }

    public bool TryMoveItem(
        string id,
        ItemInstance item,
        Vector2Int pos,
        bool rotated,
        int partIndex = -1)
    {
        var container = GetContainer(id);
        if (container == null) return false;

        // 鑻ユ湭鎸囧畾鍒嗘牸锛屽垯鏍规嵁 item 鎵€鍦ㄥ垎鏍肩Щ锟?        if (partIndex < 0)
        {
            for (int i = 0; i < container.PartGrids.Count; i++)
            {
                var g = container.PartGrids[i];
                if (g.GetPlacement(item) != null)
                {
                    partIndex = i;
                    break;
                }
            }
        }

        var grid = GetGrid(id, partIndex);
        if (grid == null) return false;

        var moved = grid.Move(item, pos, rotated);
        if (moved) NotifyChanged();
        return moved;
    }

    public bool TryRotateItemInPlace(ItemInstance item)
    {
        if (item == null || item.Definition == null || !item.Definition.IsRotatable)
        {
            return false;
        }

        if (_model == null || _model.Containers == null)
        {
            return false;
        }

        foreach (var pair in _model.Containers)
        {
            var container = pair.Value;
            if (container == null || container.PartGrids == null)
            {
                continue;
            }

            for (int i = 0; i < container.PartGrids.Count; i++)
            {
                var grid = container.PartGrids[i];
                if (grid == null)
                {
                    continue;
                }

                var placement = grid.GetPlacement(item);
                if (placement == null)
                {
                    continue;
                }

                var targetRotated = !placement.Rotated;
                var moved = grid.Move(item, placement.Pos, targetRotated);
                if (moved)
                {
                    NotifyChanged();
                    return true;
                }

                return false;
            }
        }

        return false;
    }

    public bool TryAutoPlace(
        string id,
        ItemInstance item,
        int partIndex = -1)
    {
        if (!CanPlaceInContainer(id, item)) return false;
        var grids = GetCandidateGrids(id, partIndex);

        // 浼樺厛鍚戠幇鏈夊爢鍙犺ˉ锟?        if (TryStackExisting(grids, item))
        {
            if (item.Count <= 0)
            {
                item.Count = 0;
                NotifyChanged();
                return true;
            }
        }

        foreach (var grid in GetCandidateGrids(id, partIndex))
        {
            if (grid.TryFindSpaceAuto(item, out var pos, out var rotated))
            {
                var placed = grid.Place(item, pos, rotated);
                if (placed)
                {
                    SetContainerParentIfNeeded(item, id);
                    NotifyChanged();
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryAutoPlaceToPlayerContainers(ItemInstance item)
    {
        if (item == null) return false;
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return false;

        var chestId = model.GetPlayerContainerId(InventoryContainerType.ChestRig);
        if (!string.IsNullOrEmpty(chestId) && TryAutoPlace(chestId, item)) return true;

        var backpackId = model.GetPlayerContainerId(InventoryContainerType.Backpack);
        if (!string.IsNullOrEmpty(backpackId) && TryAutoPlace(backpackId, item)) return true;

        var pocketId = model.GetPlayerContainerId(InventoryContainerType.Pocket);
        if (!string.IsNullOrEmpty(pocketId) && TryAutoPlace(pocketId, item)) return true;

        return false;
    }

    public bool TryRemoveItem(
        string id,
        ItemInstance item,
        bool notify = true,
        int partIndex = -1)
    {
        foreach (var grid in GetCandidateGrids(id, partIndex))
        {
            if (grid.GetPlacement(item) != null && grid.Remove(item))
            {
                ClearContainerParentIfNeeded(item);
                if (notify) NotifyChanged();
                return true;
            }
        }
        return false;
    }

    public bool TryTakeItemAt(
        string id,
        Vector2Int pos,
        out ItemInstance item,
        bool notify = true,
        int partIndex = -1)
    {
        foreach (var grid in GetCandidateGrids(id, partIndex))
        {
            var taken = grid.TryTakeAt(pos, out item);
            if (taken)
            {
                ClearContainerParentIfNeeded(item);
                if (notify) NotifyChanged();
                return true;
            }
        }

        item = null;
        return false;
    }

    private bool TryStackExisting(IEnumerable<InventoryGrid> grids, ItemInstance item)
    {
        var changed = false;
        foreach (var grid in grids)
        {
            foreach (var placement in grid.GetAllPlacements())
            {
                if (!placement.Item.CanStackWith(item)) continue;
                var moved = item.Count - placement.Item.AddToStack(item.Count);
                item.Count -= moved;
                if (moved > 0) changed = true;
                if (item.Count <= 0) break;
            }
            if (item.Count <= 0) break;
        }
        if (changed) NotifyChanged();
        return changed;
    }

    private bool CanPlaceInContainer(string containerId, ItemInstance item)
    {
        if (item == null) return false;
        if (!IsContainerItem(item)) return true;
        var container = EnsureAttachedContainer(item);
        if (container == null) return false;
        var targetDepth = GetContainerDepth(containerId);
        var itemDepth = GetContainerSubtreeDepth(container.InstanceId);
        return targetDepth + itemDepth <= MaxContainerNestDepth;
    }

    private int GetContainerDepth(string containerId)
    {
        if (string.IsNullOrEmpty(containerId)) return 0;
        var depth = 0;
        var currentId = containerId;
        var visited = new HashSet<string>();
        while (!string.IsNullOrEmpty(currentId) && _model.Containers.TryGetValue(currentId, out var container))
        {
            if (!visited.Add(currentId)) break;
            if (string.IsNullOrEmpty(container.ParentContainerId)) break;
            depth++;
            currentId = container.ParentContainerId;
        }
        return depth;
    }

    private int GetContainerSubtreeDepth(string containerId)
    {
        if (string.IsNullOrEmpty(containerId)) return 0;
        return GetContainerSubtreeDepthInternal(containerId, new HashSet<string>());
    }

    private int GetContainerSubtreeDepthInternal(string containerId, HashSet<string> visited)
    {
        if (!visited.Add(containerId)) return 0;
        if (!_model.Containers.TryGetValue(containerId, out var container) || container == null) return 0;

        var maxChildDepth = 0;
        foreach (var kvp in _model.Containers)
        {
            var child = kvp.Value;
            if (child == null) continue;
            if (child.ParentContainerId != containerId) continue;

            var depth = GetContainerSubtreeDepthInternal(child.InstanceId, visited);
            if (depth > maxChildDepth) maxChildDepth = depth;
        }

        return 1 + maxChildDepth;
    }

    private bool IsContainerItem(ItemInstance item)
    {
        return item != null && item.Definition != null && item.Definition.IsContainer;
    }

    private InventoryContainer EnsureAttachedContainer(ItemInstance item)
    {
        if (item == null) return null;
        if (item.AttachedContainer != null) return item.AttachedContainer;
        var containerConfig = item.Definition?.GetRuntimeContainerConfig();
        if (containerConfig == null) return null;

        var container = new InventoryContainer(containerConfig.containerType);
        container.ContainerName = containerConfig.containerName;
        foreach (var part in containerConfig.partGridDatas)
        {
            container.AddGrid(part.Size);
        }
        item.AttachedContainer = container;
        if (!_model.Containers.ContainsKey(container.InstanceId))
        {
            _model.Containers[container.InstanceId] = container;
        }
        return container;
    }

    private void SetContainerParentIfNeeded(ItemInstance item, string containerId)
    {
        if (!IsContainerItem(item)) return;
        var container = EnsureAttachedContainer(item);
        if (container == null) return;
        container.ParentContainerId = containerId;
    }

    private void ClearContainerParentIfNeeded(ItemInstance item)
    {
        if (item == null || item.AttachedContainer == null) return;
        item.AttachedContainer.ParentContainerId = null;
    }

    private void AccumulateItemValue(
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
            AccumulateContainerValue(item.AttachedContainer, countedItemIds, visitedContainerIds, ref totalValue);
        }
    }

    private void AccumulateContainerValue(
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
                AccumulateItemValue(placement?.Item, countedItemIds, visitedContainerIds, ref totalValue);
            }
        }
    }

}








