using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryGrid
{
    public int Width { get; }
    public int Height { get; }

    private readonly ItemInstance[,] _cells;
    private readonly Dictionary<string, ItemPlacement> _placements =
        new Dictionary<string, ItemPlacement>();

    public InventoryGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new ItemInstance[width, height];
    }

    #region --- Query ---

    public ItemPlacement GetPlacement(ItemInstance item)
        => item == null ? null : (_placements.TryGetValue(item.InstanceId, out var p) ? p : null);

    public IEnumerable<ItemPlacement> GetAllPlacements()
        => _placements.Values;

    public ItemInstance GetItemAt(Vector2Int cellPos)
    {
        if (cellPos.x < 0 || cellPos.y < 0 ||
            cellPos.x >= Width || cellPos.y >= Height)
            return null;

        return _cells[cellPos.x, cellPos.y];
    }

    #endregion

    #region --- Core Logic ---

    public bool CanPlace(ItemInstance item, Vector2Int pos, bool rotated)
    {
        var size = GetSize(item, rotated);
        if (!InBounds(pos, size)) return false;

        for (int y = pos.y; y < pos.y + size.y; y++)
            for (int x = pos.x; x < pos.x + size.x; x++)
            {
                if (_cells[x, y] != null) return false;
            }
        return true;
    }

    public bool Place(ItemInstance item, Vector2Int pos, bool rotated)
    {
        // 若该实例已存在于网格中，先清理旧位置
        Remove(item);

        if (!CanPlace(item, pos, rotated)) return false;

        var size = GetSize(item, rotated);

        for (int y = pos.y; y < pos.y + size.y; y++)
            for (int x = pos.x; x < pos.x + size.x; x++)
                _cells[x, y] = item;

        _placements[item.InstanceId] = new ItemPlacement
        {
            Item = item,
            Pos = pos,
            Size = size,
            Rotated = rotated
        };

        item.Rotated = rotated;
        return true;
    }

    public bool TryStackAt(Vector2Int pos, ItemInstance fromItem, out int movedCount)
    {
        movedCount = 0;
        var target = GetItemAt(pos);
        if (target == null || !target.CanStackWith(fromItem)) return false;

        movedCount = fromItem.Count - target.AddToStack(fromItem.Count);
        return movedCount > 0;
    }

    public bool PlaceOrStack(ItemInstance item, Vector2Int pos, bool rotated)
    {
        // 优先尝试叠加，如果叠加完全成功则不需要占用新格子
        if (TryStackAt(pos, item, out var moved))
        {
            item.Count -= moved;
            if (item.Count <= 0)
            {
                item.Count = 0;
                return true;
            }

            // 当前位置被原堆叠占用，无法再放置剩余部分
            return false;
        }

        return Place(item, pos, rotated);
    }

    public bool Remove(ItemInstance item)
    {
        if (item == null || !_placements.TryGetValue(item.InstanceId, out var p))
            return false;

        for (int y = p.Pos.y; y < p.Pos.y + p.Size.y; y++)
            for (int x = p.Pos.x; x < p.Pos.x + p.Size.x; x++)
                _cells[x, y] = null;

        _placements.Remove(item.InstanceId);
        return true;
    }

    public bool Move(ItemInstance item, Vector2Int newPos, bool rotated)
    {
        if (item == null || !_placements.TryGetValue(item.InstanceId, out var old))
            return false;

        Remove(item);

        if (Place(item, newPos, rotated))
            return true;

        // 回滚
        Place(item, old.Pos, old.Rotated);
        return false;
    }

    public bool TryTakeAt(Vector2Int pos, out ItemInstance item)
    {
        item = GetItemAt(pos);
        if (item == null) return false;
        return Remove(item);
    }

    #endregion

    #region --- Auto Find Space ---

    public bool TryFindSpace(ItemInstance item, bool rotated, out Vector2Int pos)
    {
        var size = GetSize(item, rotated);

        for (int y = 0; y <= Height - size.y; y++)
            for (int x = 0; x <= Width - size.x; x++)
            {
                var p = new Vector2Int(x, y);
                if (CanPlace(item, p, rotated))
                {
                    pos = p;
                    return true;
                }
            }

        pos = default;
        return false;
    }

    public bool TryFindSpaceAuto(
        ItemInstance item,
        out Vector2Int pos,
        out bool rotated)
    {
        if (TryFindSpace(item, item.Rotated, out pos))
        {
            rotated = item.Rotated;
            return true;
        }

        if (item.Definition.CanRotate &&
            TryFindSpace(item, !item.Rotated, out pos))
        {
            rotated = !item.Rotated;
            return true;
        }

        pos = default;
        rotated = item.Rotated;
        return false;
    }

    #endregion

    #region --- Utils ---

    private static Vector2Int GetSize(ItemInstance item, bool rotated)
    {
        var s = item.Definition.Size;
        return rotated ? new Vector2Int(s.y, s.x) : s;
    }

    private bool InBounds(Vector2Int pos, Vector2Int size)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x + size.x <= Width &&
               pos.y + size.y <= Height;
    }

    #endregion
}
