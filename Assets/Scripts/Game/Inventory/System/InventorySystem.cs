using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;



public struct InventoryChangedEvent
{
}


public class InventorySystem : AbstractSystem, ICanSendCommand
{
    private const int MaxContainerNestDepth = 1;
    private InventoryContainerModel _model;

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
        // 这里可以加战斗中限制、容器规则等
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

        // 若未指定分格，则根据 item 所在分格移动
        if (partIndex < 0)
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

    public bool TryAutoPlace(
        string id,
        ItemInstance item,
        int partIndex = -1)
    {
        if (!CanPlaceInContainer(id, item)) return false;
        var grids = GetCandidateGrids(id, partIndex);

        // 优先向现有堆叠补充
        if (TryStackExisting(grids, item))
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
        return item != null && item.Definition is SOContainerItemDefinition;
    }

    private InventoryContainer EnsureAttachedContainer(ItemInstance item)
    {
        if (item == null) return null;
        if (item.AttachedContainer != null) return item.AttachedContainer;
        var def = item.Definition as SOContainerItemDefinition;
        if (def == null || def.containerConfig == null) return null;

        var container = new InventoryContainer(def.containerConfig.containerType);
        container.ContainerName = def.containerConfig.containerName;
        foreach (var part in def.containerConfig.partGridDatas)
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
}
