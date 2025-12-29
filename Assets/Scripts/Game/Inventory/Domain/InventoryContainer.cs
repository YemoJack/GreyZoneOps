using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryContainer
{
    public string InstanceId;
    public InventoryContainerType Type;
    public readonly List<InventoryGrid> PartGrids = new List<InventoryGrid>();

    public InventoryContainer(InventoryContainerType type)
    {
        InstanceId = Guid.NewGuid().ToString("N");
        Type = type;
    }

    public InventoryContainer(InventoryContainerType type, IEnumerable<Vector2Int> sizes) : this(type)
    {
        if (sizes == null) return;
        foreach (var s in sizes)
        {
            AddGrid(s);
        }
        InstanceId = Guid.NewGuid().ToString("N");
    }

    public InventoryGrid GetGrid(int index = 0)
    {
        if (index < 0 || index >= PartGrids.Count) return null;
        return PartGrids[index];
    }

    public void AddGrid(Vector2Int size)
    {
        var w = Mathf.Max(1, size.x);
        var h = Mathf.Max(1, size.y);
        PartGrids.Add(new InventoryGrid(w, h));
    }
}

