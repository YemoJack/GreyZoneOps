using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InventoryGridTidyPlacement
{
    public ItemInstance Item;
    public int PartIndex;
    public Vector2Int Position;
    public bool Rotated;
}

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

    public InventoryGrid(Vector2Int size)
    {
        Width = size.x;
        Height = size.y;
        _cells = new ItemInstance[size.x, size.y];
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
        rotated = NormalizeRotated(item, rotated);
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
        rotated = NormalizeRotated(item, rotated);

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
        if (TryStackAt(pos, item, out var moved))
        {
            item.Count -= moved;
            if (item.Count <= 0)
            {
                item.Count = 0;
                return true;
            }

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

    public void Clear()
    {
        _placements.Clear();

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _cells[x, y] = null;
    }

    public bool Move(ItemInstance item, Vector2Int newPos, bool rotated)
    {
        if (item == null || !_placements.TryGetValue(item.InstanceId, out var old))
            return false;

        Remove(item);

        if (Place(item, newPos, rotated))
            return true;

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
        rotated = NormalizeRotated(item, rotated);
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
        var currentRotated = NormalizeRotated(item, item != null && item.Rotated);
        if (TryFindSpace(item, currentRotated, out pos))
        {
            rotated = currentRotated;
            return true;
        }

        if (CanRotate(item) && TryFindSpace(item, !currentRotated, out pos))
        {
            rotated = !currentRotated;
            return true;
        }

        pos = default;
        rotated = currentRotated;
        return false;
    }

    #endregion

    #region --- Utils ---

    public static bool TryBuildTidiedPlacements(
        IEnumerable<InventoryGrid> grids,
        IEnumerable<ItemInstance> items,
        out List<InventoryGridTidyPlacement> placements)
    {
        placements = new List<InventoryGridTidyPlacement>();
        List<InventoryGrid> tempGrids = CloneEmptyGrids(grids);
        if (tempGrids.Count == 0)
        {
            return false;
        }

        List<ItemInstance> sortableItems = BuildSortableItems(items);
        Dictionary<ItemInstance, int> originalCounts = new Dictionary<ItemInstance, int>();
        Dictionary<ItemInstance, bool> originalRotations = new Dictionary<ItemInstance, bool>();
        SnapshotItems(sortableItems, originalCounts, originalRotations);

        for (int i = 0; i < sortableItems.Count; i++)
        {
            ItemInstance item = sortableItems[i];
            if (item == null || item.Definition == null || item.Count <= 0)
            {
                continue;
            }

            StackIntoAnyGrid(tempGrids, item);
            if (item.Count <= 0)
            {
                continue;
            }

            if (!TryPlaceIntoAnyGrid(tempGrids, item, out int partIndex, out Vector2Int pos, out bool rotated))
            {
                RestoreItemState(originalCounts, originalRotations, restoreCounts: true);
                return false;
            }

            placements.Add(new InventoryGridTidyPlacement
            {
                Item = item,
                PartIndex = partIndex,
                Position = pos,
                Rotated = rotated
            });
        }

        RestoreItemState(originalCounts, originalRotations, restoreCounts: false);
        return true;
    }

    private static Vector2Int GetSize(ItemInstance item, bool rotated)
    {
        var s = item.Definition.Size;
        return rotated ? new Vector2Int(s.y, s.x) : s;
    }

    private static bool CanRotate(ItemInstance item)
    {
        return item != null && item.Definition != null && item.Definition.IsRotatable;
    }

    private static bool NormalizeRotated(ItemInstance item, bool rotated)
    {
        return rotated && CanRotate(item);
    }

    private bool InBounds(Vector2Int pos, Vector2Int size)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x + size.x <= Width &&
               pos.y + size.y <= Height;
    }

    private static List<InventoryGrid> CloneEmptyGrids(IEnumerable<InventoryGrid> grids)
    {
        List<InventoryGrid> result = new List<InventoryGrid>();
        if (grids == null)
        {
            return result;
        }

        foreach (InventoryGrid grid in grids)
        {
            if (grid == null)
            {
                continue;
            }

            result.Add(new InventoryGrid(grid.Width, grid.Height));
        }

        return result;
    }

    private static List<ItemInstance> BuildSortableItems(IEnumerable<ItemInstance> items)
    {
        List<ItemInstance> sortableItems = new List<ItemInstance>();
        if (items == null)
        {
            return sortableItems;
        }

        foreach (ItemInstance item in items)
        {
            if (item?.Definition == null || item.Count <= 0)
            {
                continue;
            }

            sortableItems.Add(item);
        }

        sortableItems.Sort(CompareItemsForTidy);
        return sortableItems;
    }

    private static int CompareItemsForTidy(ItemInstance left, ItemInstance right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        Vector2Int leftSize = left.Definition.Size;
        Vector2Int rightSize = right.Definition.Size;
        int leftArea = leftSize.x * leftSize.y;
        int rightArea = rightSize.x * rightSize.y;

        int compare = rightArea.CompareTo(leftArea);
        if (compare != 0)
        {
            return compare;
        }

        compare = Mathf.Max(rightSize.x, rightSize.y).CompareTo(Mathf.Max(leftSize.x, leftSize.y));
        if (compare != 0)
        {
            return compare;
        }

        compare = Mathf.Min(rightSize.x, rightSize.y).CompareTo(Mathf.Min(leftSize.x, leftSize.y));
        if (compare != 0)
        {
            return compare;
        }

        compare = string.Compare(left.Definition.Name, right.Definition.Name, System.StringComparison.Ordinal);
        if (compare != 0)
        {
            return compare;
        }

        return string.Compare(left.InstanceId, right.InstanceId, System.StringComparison.Ordinal);
    }

    private static void SnapshotItems(
        IEnumerable<ItemInstance> items,
        Dictionary<ItemInstance, int> counts,
        Dictionary<ItemInstance, bool> rotations)
    {
        if (items == null)
        {
            return;
        }

        foreach (ItemInstance item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (!counts.ContainsKey(item))
            {
                counts[item] = item.Count;
            }

            if (!rotations.ContainsKey(item))
            {
                rotations[item] = item.Rotated;
            }
        }
    }

    private static void RestoreItemState(
        Dictionary<ItemInstance, int> counts,
        Dictionary<ItemInstance, bool> rotations,
        bool restoreCounts)
    {
        if (restoreCounts && counts != null)
        {
            foreach (KeyValuePair<ItemInstance, int> pair in counts)
            {
                if (pair.Key != null)
                {
                    pair.Key.Count = pair.Value;
                }
            }
        }

        if (rotations == null)
        {
            return;
        }

        foreach (KeyValuePair<ItemInstance, bool> pair in rotations)
        {
            if (pair.Key != null)
            {
                pair.Key.Rotated = pair.Value;
            }
        }
    }

    private static void StackIntoAnyGrid(IEnumerable<InventoryGrid> grids, ItemInstance item)
    {
        if (grids == null || item == null || item.Definition == null || item.Count <= 0)
        {
            return;
        }

        foreach (InventoryGrid grid in grids)
        {
            if (grid == null)
            {
                continue;
            }

            foreach (ItemPlacement placement in grid.GetAllPlacements())
            {
                ItemInstance target = placement?.Item;
                if (target == null || !target.CanStackWith(item))
                {
                    continue;
                }

                item.Count = target.AddToStack(item.Count);
                if (item.Count <= 0)
                {
                    item.Count = 0;
                    return;
                }
            }
        }
    }

    private static bool TryPlaceIntoAnyGrid(
        IList<InventoryGrid> grids,
        ItemInstance item,
        out int partIndex,
        out Vector2Int pos,
        out bool rotated)
    {
        partIndex = -1;
        pos = new Vector2Int(-1, -1);
        rotated = item != null && item.Rotated;
        if (grids == null || item == null)
        {
            return false;
        }

        for (int i = 0; i < grids.Count; i++)
        {
            InventoryGrid grid = grids[i];
            if (grid == null)
            {
                continue;
            }

            if (!grid.TryFindSpaceAuto(item, out pos, out rotated))
            {
                continue;
            }

            if (!grid.Place(item, pos, rotated))
            {
                continue;
            }

            partIndex = i;
            return true;
        }

        return false;
    }

    #endregion
}
