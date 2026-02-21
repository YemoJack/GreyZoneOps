using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int Version = 1;
    public long SavedAtUtcTicks;
    public PlayerInventorySaveData PlayerInventory = new PlayerInventorySaveData();
    public PlayerLoadoutSaveData PlayerLoadout = new PlayerLoadoutSaveData();
    public PlayerProgressSaveData PlayerProgress = new PlayerProgressSaveData();
}

[Serializable]
public class PlayerInventorySaveData
{
    public List<ItemSaveData> Items = new List<ItemSaveData>();
}

[Serializable]
public class ItemSaveData
{
    public int DefinitionId;
    public string DefinitionName;
    public string DefinitionResName;
    public int Count;
    public bool Rotated;
    public bool HasWarehousePosition;
    public int WarehousePartIndex = -1;
    public Vector2Int WarehousePos = new Vector2Int(-1, -1);
}

[Serializable]
public class PlayerLoadoutSaveData
{
    public List<EquippedItemSaveData> EquippedItems = new List<EquippedItemSaveData>();
    public List<PlayerContainerSaveData> Containers = new List<PlayerContainerSaveData>();
}

[Serializable]
public class EquippedItemSaveData
{
    public EquipmentSlotType Slot;
    public ItemSaveData Item;
}

[Serializable]
public class PlayerContainerSaveData
{
    public InventoryContainerType ContainerType;
    public string ContainerName;
    public List<Vector2Int> GridSizes = new List<Vector2Int>();
    public List<ContainerItemPlacementSaveData> Placements = new List<ContainerItemPlacementSaveData>();
}

[Serializable]
public class ContainerItemPlacementSaveData
{
    public int PartIndex;
    public Vector2Int Pos;
    public bool Rotated;
    public ItemSaveData Item;
}

[Serializable]
public class PlayerProgressSaveData
{
    // 当前玩家等级
    public int Level = 1;
    // 当前等级下已累计经验值
    public int Experience = 0;
    // 当前持有现金
    public int Cash = 0;
    public int TotalAsset = 0;
    // 累计成功撤离次数
    public int SuccessfulExtractionCount = 0;
    // 累计撤离收益总额
    public int TotalExtractionIncome = 0;
    // 总对局数（每局结束结算后+1）
    public int TotalRaidCount = 0;
    // 最近一次撤离收益
    public int LastExtractionIncome = 0;
    // 最近一次撤离时间（UTC Tick）
    public long LastExtractionUtcTicks = 0;

    public void Normalize()
    {
        Level = Mathf.Max(1, Level);
        Experience = Mathf.Max(0, Experience);
        Cash = Mathf.Max(0, Cash);
        TotalAsset = Mathf.Max(0, TotalAsset);
        SuccessfulExtractionCount = Mathf.Max(0, SuccessfulExtractionCount);
        TotalRaidCount = Mathf.Max(0, TotalRaidCount);
    }

    public PlayerProgressSaveData Clone()
    {
        PlayerProgressSaveData copy = new PlayerProgressSaveData
        {
            Level = Level,
            Experience = Experience,
            Cash = Cash,
            TotalAsset = TotalAsset,
            SuccessfulExtractionCount = SuccessfulExtractionCount,
            TotalExtractionIncome = TotalExtractionIncome,
            TotalRaidCount = TotalRaidCount,
            LastExtractionIncome = LastExtractionIncome,
            LastExtractionUtcTicks = LastExtractionUtcTicks
        };

        copy.Normalize();
        return copy;
    }
}
