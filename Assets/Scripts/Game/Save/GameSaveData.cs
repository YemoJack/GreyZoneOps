using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int Version = 1;
    public long SavedAtUtcTicks;
    public PlayerInventorySaveData PlayerInventory = new PlayerInventorySaveData();
}

[Serializable]
public class PlayerInventorySaveData
{
    public List<EquippedItemSaveData> EquippedItems = new List<EquippedItemSaveData>();
    public InventoryContainerSaveData PocketContainer;
}

[Serializable]
public class EquippedItemSaveData
{
    public EquipmentSlotType Slot;
    public ItemSaveData Item;
}

[Serializable]
public class ItemSaveData
{
    public int DefinitionId;
    public string DefinitionName;
    public string DefinitionResName;
    public int Count;
    public bool Rotated;
    public InventoryContainerSaveData AttachedContainer;
}

[Serializable]
public class InventoryContainerSaveData
{
    public InventoryContainerType ContainerType;
    public string ContainerName;
    public List<Vector2Int> GridSizes = new List<Vector2Int>();
    public List<ItemPlacementSaveData> Placements = new List<ItemPlacementSaveData>();
}

[Serializable]
public class ItemPlacementSaveData
{
    public int PartIndex;
    public Vector2Int Pos;
    public bool Rotated;
    public ItemSaveData Item;
}
