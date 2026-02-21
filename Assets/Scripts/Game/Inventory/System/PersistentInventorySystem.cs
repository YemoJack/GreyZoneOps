using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public partial class InventorySystem
{
    private const int SaveDataVersion = 6;
    public const string DefaultGameSaveKey = "game_save_data";

    private PersistentInventoryModel _persistentModel;
    private readonly Dictionary<int, ItemCatalogEntry> itemDefinitionLookup = new Dictionary<int, ItemCatalogEntry>();
    private readonly List<ItemCatalogEntry> discoveredItemDefinitions = new List<ItemCatalogEntry>();
    private bool _raidItemsCommitted;

    public bool SaveGameData(string key = DefaultGameSaveKey, string filePath = null)
    {
        var saveLoader = this.GetUtility<ISaveLoader>();
        if (saveLoader == null)
        {
            Debug.LogError("InventorySystem.SaveGameData: ISaveLoader is null.");
            return false;
        }

        var data = new GameSaveData
        {
            Version = SaveDataVersion,
            SavedAtUtcTicks = DateTime.UtcNow.Ticks,
            PlayerInventory = BuildPlayerInventorySaveData(),
            PlayerLoadout = BuildPlayerLoadoutSaveData(),
            PlayerProgress = this.GetSystem<PlayerProgressSystem>()?.BuildSaveData() ?? new PlayerProgressSaveData()
        };

        saveLoader.Save(key, data, filePath);
        return true;
    }

    public bool LoadGameData(string key = DefaultGameSaveKey, string filePath = null)
    {
        var saveLoader = this.GetUtility<ISaveLoader>();
        if (saveLoader == null)
        {
            Debug.LogError("InventorySystem.LoadGameData: ISaveLoader is null.");
            return false;
        }

        if (!saveLoader.TryLoad(key, out GameSaveData data, filePath) || data == null)
        {
            return false;
        }

        if (data.PlayerInventory == null)
        {
            Debug.LogWarning("InventorySystem.LoadGameData: PlayerInventory is null in save data.");
            return false;
        }

        bool loadedInventory = ApplyPlayerInventorySaveData(data.PlayerInventory);
        bool loadedLoadout = ApplyPlayerLoadoutSaveData(data.PlayerLoadout ?? new PlayerLoadoutSaveData());
        _raidEntryValue = 0;
        this.GetSystem<PlayerProgressSystem>()?.ApplySaveData(data.PlayerProgress ?? new PlayerProgressSaveData());
        if (loadedInventory || loadedLoadout)
        {
            NotifyChanged();
        }

        return loadedInventory || loadedLoadout;
    }

    public void CommitCurrentRaidItemsToPersistentInventoryNow()
    {
        CommitCurrentRaidItemsToPersistentInventory();
    }

    private void OnExtractionSucceeded(EventExtractionSucceeded _)
    {
        CommitCurrentRaidItemsToPersistentInventory();
    }

    private bool EnsurePersistentModel()
    {
        if (_persistentModel == null)
        {
            _persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        if (_persistentModel == null)
        {
            return false;
        }

        _persistentModel.GetMutableItems();
        return true;
    }

    private PlayerInventorySaveData BuildPlayerInventorySaveData()
    {
        PlayerInventorySaveData saveData = new PlayerInventorySaveData();
        if (!EnsurePersistentModel())
        {
            return saveData;
        }

        var persistentItems = _persistentModel.GetMutableItems();
        for (int i = 0; i < persistentItems.Count; i++)
        {
            ItemSaveData itemData = CaptureItemData(persistentItems[i], includeWarehousePosition: true);
            if (itemData != null)
            {
                saveData.Items.Add(itemData);
            }
        }

        return saveData;
    }

    private PlayerLoadoutSaveData BuildPlayerLoadoutSaveData()
    {
        PlayerLoadoutSaveData loadout = new PlayerLoadoutSaveData();
        if (!ShouldCaptureOutOfRaidLoadout())
        {
            return loadout;
        }

        EquipmentContainer equipment = _model?.PlayerEquipment;
        if (equipment == null)
        {
            return loadout;
        }

        foreach (EquipmentSlotType slot in Enum.GetValues(typeof(EquipmentSlotType)))
        {
            ItemInstance item = equipment.GetItem(slot);
            ItemSaveData itemData = CaptureItemData(item, includeWarehousePosition: false);
            if (itemData == null)
            {
                continue;
            }

            loadout.EquippedItems.Add(new EquippedItemSaveData
            {
                Slot = slot,
                Item = itemData
            });
        }

        foreach (InventoryContainerType type in Enum.GetValues(typeof(InventoryContainerType)))
        {
            if (type == InventoryContainerType.Stash || type == InventoryContainerType.LootBox)
            {
                continue;
            }

            InventoryContainer container = equipment.GetContainer(type);
            if (container == null)
            {
                continue;
            }

            loadout.Containers.Add(CapturePlayerContainerData(type, container));
        }

        return loadout;
    }

    private ItemSaveData CaptureItemData(ItemInstance item, bool includeWarehousePosition)
    {
        if (item?.Definition == null)
        {
            return null;
        }

        var data = new ItemSaveData
        {
            DefinitionId = item.Definition.Id,
            DefinitionName = item.Definition.Name,
            DefinitionResName = item.Definition.ResName,
            Count = Mathf.Max(1, item.Count),
            Rotated = item.Rotated
        };

        if (includeWarehousePosition &&
            _persistentModel != null &&
            _persistentModel.TryGetWarehouseItemPosition(item, out int partIndex, out Vector2Int pos))
        {
            data.HasWarehousePosition = true;
            data.WarehousePartIndex = Mathf.Max(0, partIndex);
            data.WarehousePos = new Vector2Int(Mathf.Max(0, pos.x), Mathf.Max(0, pos.y));
        }

        return data;
    }

    private PlayerContainerSaveData CapturePlayerContainerData(InventoryContainerType type, InventoryContainer container)
    {
        PlayerContainerSaveData data = new PlayerContainerSaveData
        {
            ContainerType = type,
            ContainerName = container != null ? container.ContainerName : string.Empty
        };

        if (container == null)
        {
            return data;
        }

        for (int i = 0; i < container.PartGrids.Count; i++)
        {
            InventoryGrid grid = container.PartGrids[i];
            if (grid == null)
            {
                continue;
            }

            data.GridSizes.Add(new Vector2Int(grid.Width, grid.Height));
            foreach (ItemPlacement placement in grid.GetAllPlacements())
            {
                ItemSaveData itemData = CaptureItemData(placement?.Item, includeWarehousePosition: false);
                if (itemData == null)
                {
                    continue;
                }

                data.Placements.Add(new ContainerItemPlacementSaveData
                {
                    PartIndex = i,
                    Pos = placement.Pos,
                    Rotated = placement.Rotated,
                    Item = itemData
                });
            }
        }

        return data;
    }

    private bool ApplyPlayerInventorySaveData(PlayerInventorySaveData saveData)
    {
        if (saveData == null || !EnsurePersistentModel())
        {
            return false;
        }

        List<ItemSaveData> itemDataList = saveData.Items ?? new List<ItemSaveData>();
        bool hasAnySavedItem = itemDataList.Count > 0;
        if (hasAnySavedItem && !TryBuildItemDefinitionLookup())
        {
            Debug.LogWarning("InventorySystem: item definition lookup is empty. Cannot load inventory. Please assign SOGameConfig.ItemCatalog.");
            return false;
        }

        List<ItemInstance> loadedItems = new List<ItemInstance>();
        for (int i = 0; i < itemDataList.Count; i++)
        {
            ItemInstance item = RestoreItemData(itemDataList[i]);
            if (item != null)
            {
                loadedItems.Add(item);
            }
        }

        _persistentModel.ReplaceItems(loadedItems);

        List<ItemInstance> persistentItems = _persistentModel.GetMutableItems();
        int count = Mathf.Min(itemDataList.Count, persistentItems.Count);
        for (int i = 0; i < count; i++)
        {
            ItemSaveData itemData = itemDataList[i];
            if (itemData == null || !itemData.HasWarehousePosition)
            {
                continue;
            }

            _persistentModel.SetWarehouseItemPosition(
                persistentItems[i],
                itemData.WarehousePartIndex,
                itemData.WarehousePos);
        }

        return true;
    }

    private bool ApplyPlayerLoadoutSaveData(PlayerLoadoutSaveData loadoutData)
    {
        if (loadoutData == null || !ShouldApplyOutOfRaidLoadout() || _model == null)
        {
            return false;
        }

        bool hasLoadout =
            (loadoutData.EquippedItems != null && loadoutData.EquippedItems.Count > 0) ||
            (loadoutData.Containers != null && loadoutData.Containers.Count > 0);

        if (!hasLoadout)
        {
            return false;
        }

        if (!TryBuildItemDefinitionLookup())
        {
            Debug.LogWarning("InventorySystem: item definition lookup is empty. Cannot load player loadout.");
            return false;
        }

        EquipmentContainer loadedEquipment = new EquipmentContainer();

        if (loadoutData.EquippedItems != null)
        {
            for (int i = 0; i < loadoutData.EquippedItems.Count; i++)
            {
                EquippedItemSaveData equipData = loadoutData.EquippedItems[i];
                if (equipData?.Item == null)
                {
                    continue;
                }

                ItemInstance item = RestoreItemData(equipData.Item);
                if (item == null)
                {
                    continue;
                }

                loadedEquipment.TryEquip(equipData.Slot, item, out _);
            }
        }

        if (loadoutData.Containers != null)
        {
            for (int i = 0; i < loadoutData.Containers.Count; i++)
            {
                ApplyPlayerContainerSaveData(loadedEquipment, loadoutData.Containers[i]);
            }
        }

        _model.PlayerEquipment = loadedEquipment;
        if (_model.Containers == null)
        {
            _model.Containers = new Dictionary<string, InventoryContainer>();
        }
        else
        {
            _model.Containers.Clear();
        }

        foreach (InventoryContainer container in loadedEquipment.Containers.Values)
        {
            RegisterContainerRecursive(container);
        }

        _raidEntryValue = 0;
        return true;
    }

    private void ApplyPlayerContainerSaveData(EquipmentContainer equipment, PlayerContainerSaveData data)
    {
        if (equipment == null || data == null)
        {
            return;
        }

        InventoryContainer container = equipment.GetContainer(data.ContainerType);
        if (container == null)
        {
            container = new InventoryContainer(data.ContainerType);
            equipment.TryAddContainer(container);
        }

        if (container == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(data.ContainerName))
        {
            container.ContainerName = data.ContainerName;
        }

        ApplyPlayerContainerGridLayout(container, data);

        if (data.Placements == null)
        {
            return;
        }

        for (int i = 0; i < data.Placements.Count; i++)
        {
            ContainerItemPlacementSaveData placementData = data.Placements[i];
            if (placementData?.Item == null)
            {
                continue;
            }

            InventoryGrid grid = container.GetGrid(placementData.PartIndex);
            if (grid == null)
            {
                continue;
            }

            ItemInstance item = RestoreItemData(placementData.Item);
            if (item == null)
            {
                continue;
            }

            if (!grid.Place(item, placementData.Pos, placementData.Rotated))
            {
                Debug.LogWarning($"InventorySystem: failed to restore loadout item {item.Definition?.Name}.");
            }
        }
    }

    private void ApplyPlayerContainerGridLayout(InventoryContainer container, PlayerContainerSaveData data)
    {
        if (container == null || data == null)
        {
            return;
        }

        container.PartGrids.Clear();
        if (data.GridSizes == null || data.GridSizes.Count == 0)
        {
            return;
        }

        for (int i = 0; i < data.GridSizes.Count; i++)
        {
            Vector2Int size = data.GridSizes[i];
            container.AddGrid(new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)));
        }
    }

    private bool SaveDataContainsAnyItem(PlayerInventorySaveData saveData)
    {
        if (saveData?.Items == null)
        {
            return false;
        }

        for (int i = 0; i < saveData.Items.Count; i++)
        {
            if (saveData.Items[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void CommitCurrentRaidItemsToPersistentInventory()
    {
        if (_raidItemsCommitted)
        {
            return;
        }

        if (!EnsurePersistentModel())
        {
            return;
        }

        EquipmentContainer equipment = _model?.PlayerEquipment;
        if (equipment == null)
        {
            return;
        }

        List<ItemInstance> extractionRoots = new List<ItemInstance>();
        HashSet<string> visitedRootItemIds = new HashSet<string>();

        foreach (var slotItem in equipment.Slots.Values)
        {
            AppendExtractionRoot(slotItem, extractionRoots, visitedRootItemIds);
        }

        foreach (var container in equipment.Containers.Values)
        {
            if (container == null || container.Type == InventoryContainerType.Stash)
            {
                continue;
            }
            foreach (var grid in container.PartGrids)
            {
                if (grid == null)
                {
                    continue;
                }

                foreach (var placement in grid.GetAllPlacements())
                {
                    AppendExtractionRoot(placement?.Item, extractionRoots, visitedRootItemIds);
                }
            }
        }

        int appendedCount = _persistentModel.AppendItemsAsFlat(extractionRoots);
        if (appendedCount > 0)
        {
            _raidItemsCommitted = true;
        }
    }

    private static void AppendExtractionRoot(
        ItemInstance item,
        List<ItemInstance> output,
        HashSet<string> visitedItemIds)
    {
        if (item?.Definition == null || output == null || visitedItemIds == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(item.InstanceId) && !visitedItemIds.Add(item.InstanceId))
        {
            return;
        }

        output.Add(item);
    }

    private ItemInstance RestoreItemData(ItemSaveData itemData)
    {
        if (itemData == null)
        {
            return null;
        }

        if (!TryResolveDefinition(itemData, out var definition) || definition == null)
        {
            Debug.LogWarning($"InventorySystem: missing item definition id={itemData.DefinitionId}, name={itemData.DefinitionName}.");
            return null;
        }

        var item = new ItemInstance(definition, Mathf.Max(1, itemData.Count))
        {
            Rotated = itemData.Rotated
        };

        return item;
    }

    private void RegisterContainerRecursive(InventoryContainer container)
    {
        if (container == null)
        {
            return;
        }

        if (_model.Containers == null)
        {
            _model.Containers = new Dictionary<string, InventoryContainer>();
        }

        _model.Containers[container.InstanceId] = container;

        foreach (var grid in container.PartGrids)
        {
            if (grid == null)
            {
                continue;
            }

            foreach (var placement in grid.GetAllPlacements())
            {
                var attached = placement?.Item?.AttachedContainer;
                if (attached != null)
                {
                    RegisterContainerRecursive(attached);
                }
            }
        }
    }

    private bool TryBuildItemDefinitionLookup()
    {
        itemDefinitionLookup.Clear();
        discoveredItemDefinitions.Clear();

        var config = GameSettingManager.Instance?.Config;
        if (config != null && config.ItemCatalog != null)
        {
            RegisterDefinitionsFromCatalog(config.ItemCatalog);
        }

        RegisterDefinitionsFromCurrentInventory();

        return discoveredItemDefinitions.Count > 0;
    }

    private void RegisterDefinitionsFromCatalog(SOItemCatalog catalog)
    {
        if (catalog == null)
        {
            return;
        }

        var entries = catalog.GetEntries();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry != null)
            {
                RegisterDefinitionToLookup(entry);
            }
        }
    }

    private void RegisterDefinitionToLookup(ItemCatalogEntry definition)
    {
        if (definition == null)
        {
            return;
        }

        if (!discoveredItemDefinitions.Contains(definition))
        {
            discoveredItemDefinitions.Add(definition);
        }

        if (definition.Id <= 0)
        {
            return;
        }

        if (!itemDefinitionLookup.ContainsKey(definition.Id))
        {
            itemDefinitionLookup[definition.Id] = definition;
        }
        else if (itemDefinitionLookup[definition.Id] != definition)
        {
            Debug.LogWarning($"InventorySystem: duplicate item definition id={definition.Id}, name={definition.Name}");
        }
    }

    private void RegisterDefinitionsFromCurrentInventory()
    {
        var equipment = _model?.PlayerEquipment;
        if (equipment == null)
        {
            return;
        }

        var visitedItems = new HashSet<string>();
        foreach (var slotItem in equipment.Slots.Values)
        {
            RegisterDefinitionFromItemRecursive(slotItem, visitedItems);
        }

        foreach (var container in equipment.Containers.Values)
        {
            RegisterDefinitionsFromContainerRecursive(container, visitedItems);
        }
    }

    private void RegisterDefinitionsFromContainerRecursive(InventoryContainer container, HashSet<string> visitedItems)
    {
        if (container == null)
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
                RegisterDefinitionFromItemRecursive(placement?.Item, visitedItems);
            }
        }
    }

    private void RegisterDefinitionFromItemRecursive(ItemInstance item, HashSet<string> visitedItems)
    {
        if (item == null || string.IsNullOrEmpty(item.InstanceId))
        {
            return;
        }

        if (!visitedItems.Add(item.InstanceId))
        {
            return;
        }

        RegisterDefinitionToLookup(item.Definition);

        if (item.AttachedContainer != null)
        {
            RegisterDefinitionsFromContainerRecursive(item.AttachedContainer, visitedItems);
        }
    }

    private bool TryResolveDefinition(ItemSaveData itemData, out ItemCatalogEntry definition)
    {
        definition = null;
        if (itemData == null)
        {
            return false;
        }

        if (itemData.DefinitionId > 0 &&
            itemDefinitionLookup.TryGetValue(itemData.DefinitionId, out definition) &&
            definition != null)
        {
            return true;
        }

        for (int i = 0; i < discoveredItemDefinitions.Count; i++)
        {
            var candidate = discoveredItemDefinitions[i];
            if (candidate == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(itemData.DefinitionName) &&
                string.Equals(candidate.Name, itemData.DefinitionName, StringComparison.OrdinalIgnoreCase))
            {
                definition = candidate;
                return true;
            }

            if (!string.IsNullOrEmpty(itemData.DefinitionResName) &&
                string.Equals(candidate.ResName, itemData.DefinitionResName, StringComparison.OrdinalIgnoreCase))
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool ShouldCaptureOutOfRaidLoadout()
    {
        GameFlowSystem flowSystem = this.GetSystem<GameFlowSystem>();
        GameFlowState state = flowSystem != null ? flowSystem.CurrentState : GameFlowState.None;
        return state != GameFlowState.InRaid &&
               state != GameFlowState.LoadingToGame &&
               state != GameFlowState.RaidEnded;
    }

    private bool ShouldApplyOutOfRaidLoadout()
    {
        return ShouldCaptureOutOfRaidLoadout();
    }
}
