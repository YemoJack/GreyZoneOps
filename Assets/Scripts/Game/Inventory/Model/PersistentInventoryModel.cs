using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PersistentInventoryModel : AbstractModel
{
    public struct WarehouseItemPositionData
    {
        public int PartIndex;
        public Vector2Int Position;
    }

    public List<ItemInstance> Items { get; private set; }
    private Dictionary<string, WarehouseItemPositionData> warehouseItemPositions;

    protected override void OnInit()
    {
        Items = new List<ItemInstance>();
        warehouseItemPositions = new Dictionary<string, WarehouseItemPositionData>();
    }

    public List<ItemInstance> GetMutableItems()
    {
        if (Items == null)
        {
            Items = new List<ItemInstance>();
        }

        return Items;
    }

    public void ReplaceItems(List<ItemInstance> items)
    {
        Items = new List<ItemInstance>();
        ClearWarehouseItemPositions();
        AppendItemsAsFlat(items);
    }

    public void SetWarehouseItemPosition(ItemInstance item, int partIndex, Vector2Int pos)
    {
        if (warehouseItemPositions == null)
        {
            warehouseItemPositions = new Dictionary<string, WarehouseItemPositionData>();
        }

        string key = GetItemKey(item);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        warehouseItemPositions[key] = new WarehouseItemPositionData
        {
            PartIndex = Mathf.Max(0, partIndex),
            Position = new Vector2Int(Mathf.Max(0, pos.x), Mathf.Max(0, pos.y))
        };
    }

    public bool TryGetWarehouseItemPosition(ItemInstance item, out int partIndex, out Vector2Int pos)
    {
        partIndex = -1;
        pos = new Vector2Int(-1, -1);

        string key = GetItemKey(item);
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (warehouseItemPositions == null)
        {
            return false;
        }

        if (!warehouseItemPositions.TryGetValue(key, out WarehouseItemPositionData data))
        {
            return false;
        }

        partIndex = data.PartIndex;
        pos = data.Position;
        return true;
    }

    public void RemoveWarehouseItemPosition(ItemInstance item)
    {
        string key = GetItemKey(item);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (warehouseItemPositions == null)
        {
            return;
        }

        warehouseItemPositions.Remove(key);
    }

    public void ClearWarehouseItemPositions()
    {
        warehouseItemPositions?.Clear();
    }

    public int AppendItemsAsFlat(IEnumerable<ItemInstance> items)
    {
        if (Items == null)
        {
            Items = new List<ItemInstance>();
        }

        if (items == null)
        {
            return 0;
        }

        int startCount = Items.Count;
        HashSet<string> visitedItemIds = new HashSet<string>();
        HashSet<string> visitedContainerIds = new HashSet<string>();

        foreach (ItemInstance item in items)
        {
            AppendFlatItemRecursive(item, Items, visitedItemIds, visitedContainerIds);
        }

        return Items.Count - startCount;
    }

    private static string GetItemKey(ItemInstance item)
    {
        return item != null ? item.InstanceId : null;
    }

    private static void AppendFlatItemRecursive(
        ItemInstance source,
        List<ItemInstance> output,
        HashSet<string> visitedItemIds,
        HashSet<string> visitedContainerIds)
    {
        if (source?.Definition == null || output == null || visitedItemIds == null || visitedContainerIds == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(source.InstanceId) && !visitedItemIds.Add(source.InstanceId))
        {
            return;
        }

        ItemInstance flatItem = new ItemInstance(source.Definition, Mathf.Max(1, source.Count))
        {
            Rotated = source.Rotated,
            AttachedContainer = null
        };
        output.Add(flatItem);

        InventoryContainer container = source.AttachedContainer;
        if (container == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(container.InstanceId) && !visitedContainerIds.Add(container.InstanceId))
        {
            return;
        }

        for (int gridIndex = 0; gridIndex < container.PartGrids.Count; gridIndex++)
        {
            InventoryGrid grid = container.PartGrids[gridIndex];
            if (grid == null)
            {
                continue;
            }

            foreach (ItemPlacement placement in grid.GetAllPlacements())
            {
                AppendFlatItemRecursive(placement?.Item, output, visitedItemIds, visitedContainerIds);
            }
        }
    }
}
