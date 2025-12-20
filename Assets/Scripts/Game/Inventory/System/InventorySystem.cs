using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;



public struct InventoryChangedEvent
{
}


public class InventorySystem : AbstractSystem
{
    private InventoryModel _model;

    private void NotifyChanged()
    {
        this.SendEvent(new InventoryChangedEvent());
    }

    protected override void OnInit()
    {
        _model = this.GetModel<InventoryModel>();
    }

    public InventoryGrid GetGrid(InventoryContainerType type)
        => _model.Grids.TryGetValue(type, out var grid) ? grid : null;

    public bool TryPlaceItem(
        InventoryContainerType type,
        ItemInstance item,
        Vector2Int pos,
        bool rotated)
    {
        var grid = _model.Grids[type];

        // 这里可以加战斗中限制、容器规则等
        var placed = grid.PlaceOrStack(item, pos, rotated);
        if (placed) NotifyChanged();
        return placed;
    }

    public bool TryMoveItem(
        InventoryContainerType type,
        ItemInstance item,
        Vector2Int pos,
        bool rotated)
    {
        var moved = _model.Grids[type].Move(item, pos, rotated);
        if (moved) NotifyChanged();
        return moved;
    }

    public bool TryAutoPlace(
        InventoryContainerType type,
        ItemInstance item)
    {
        var grid = _model.Grids[type];

        // 优先向现有堆叠补充
        if (TryStackExisting(grid, item))
        {
            if (item.Count <= 0)
            {
                item.Count = 0;
                NotifyChanged();
                return true;
            }
        }

        if (grid.TryFindSpaceAuto(item, out var pos, out var rotated))
        {
            var placed = grid.Place(item, pos, rotated);
            if (placed) NotifyChanged();
            return placed;
        }

        return false;
    }

    public bool TryRemoveItem(
        InventoryContainerType type,
        ItemInstance item)
    {
        var grid = _model.Grids[type];
        var removed = grid.Remove(item);
        if (removed) NotifyChanged();
        return removed;
    }

    public bool TryTakeItemAt(
        InventoryContainerType type,
        Vector2Int pos,
        out ItemInstance item)
    {
        var grid = _model.Grids[type];
        var taken = grid.TryTakeAt(pos, out item);
        if (taken) NotifyChanged();
        return taken;
    }

    private bool TryStackExisting(InventoryGrid grid, ItemInstance item)
    {
        var changed = false;
        foreach (var placement in grid.GetAllPlacements())
        {
            if (!placement.Item.CanStackWith(item)) continue;
            var moved = item.Count - placement.Item.AddToStack(item.Count);
            item.Count -= moved;
            if (moved > 0) changed = true;
            if (item.Count <= 0) break;
        }
        if (changed) NotifyChanged();
        return changed;
    }
}
