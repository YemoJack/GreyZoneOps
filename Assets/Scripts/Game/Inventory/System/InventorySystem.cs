using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;



public struct InventoryChangedEvent
{
}


public class InventorySystem : AbstractSystem
{
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
        // 这里可以加战斗中限制、容器规则等
        foreach (var grid in GetCandidateGrids(id, partIndex))
        {
            var placed = grid.PlaceOrStack(item, pos, rotated);
            if (placed)
            {
                NotifyChanged();
                return true;
            }
        }
        return false;
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
}
