using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;



public struct InventoryChangedEvent
{
}


public class InventorySystem : AbstractSystem, ICanSendCommand
{
    private const int MaxContainerNestDepth = 2;
    private const int SaveDataVersion = 1;
    public const string DefaultGameSaveKey = "game_save_data";
    private static readonly EquipmentSlotType[] EquipLoadOrder =
    {
        EquipmentSlotType.ChestRig,
        EquipmentSlotType.Backpack,
        EquipmentSlotType.Helmet,
        EquipmentSlotType.Armor,
        EquipmentSlotType.Weapon1,
        EquipmentSlotType.Weapon2,
        EquipmentSlotType.Weapon3,
        EquipmentSlotType.Weapon4
    };

    private InventoryContainerModel _model;
    private readonly Dictionary<int, ItemCatalogEntry> itemDefinitionLookup = new Dictionary<int, ItemCatalogEntry>();
    private readonly List<ItemCatalogEntry> discoveredItemDefinitions = new List<ItemCatalogEntry>();

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

    public int GetCurrentRaidIncome()
    {
        int totalValue = 0;
        var equipment = _model?.PlayerEquipment;
        if (equipment == null)
        {
            return 0;
        }

        var countedItemIds = new HashSet<string>();
        var visitedContainerIds = new HashSet<string>();

        foreach (var slotItem in equipment.Slots.Values)
        {
            AccumulateItemValue(slotItem, countedItemIds, visitedContainerIds, ref totalValue);
        }

        foreach (var container in equipment.Containers.Values)
        {
            AccumulateContainerValue(container, countedItemIds, visitedContainerIds, ref totalValue);
        }

        return Mathf.Max(0, totalValue);
    }

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
            PlayerInventory = BuildPlayerInventorySaveData()
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

        var loaded = ApplyPlayerInventorySaveData(data.PlayerInventory);
        if (loaded)
        {
            NotifyChanged();
        }

        return loaded;
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

    public bool TryAutoPlaceToPlayerContainers(ItemInstance item)
    {
        if (item == null) return false;
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return false;

        var chestId = model.GetPlayerContainerId(InventoryContainerType.ChestRig);
        if (!string.IsNullOrEmpty(chestId) && TryAutoPlace(chestId, item)) return true;

        var backpackId = model.GetPlayerContainerId(InventoryContainerType.Backpack);
        if (!string.IsNullOrEmpty(backpackId) && TryAutoPlace(backpackId, item)) return true;

        var pocketId = model.GetPlayerContainerId(InventoryContainerType.Pocket);
        if (!string.IsNullOrEmpty(pocketId) && TryAutoPlace(pocketId, item)) return true;

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

    private PlayerInventorySaveData BuildPlayerInventorySaveData()
    {
        var saveData = new PlayerInventorySaveData();
        var equipment = _model?.PlayerEquipment;
        if (equipment == null)
        {
            return saveData;
        }

        var visitedItemIds = new HashSet<string>();
        var visitedContainerIds = new HashSet<string>();

        foreach (var slotEntry in equipment.Slots)
        {
            var item = slotEntry.Value;
            if (item == null || item.Definition == null)
            {
                continue;
            }

            var itemData = CaptureItemData(item, visitedItemIds, visitedContainerIds);
            if (itemData == null)
            {
                continue;
            }

            saveData.EquippedItems.Add(new EquippedItemSaveData
            {
                Slot = slotEntry.Key,
                Item = itemData
            });
        }

        var pocket = equipment.GetContainer(InventoryContainerType.Pocket);
        if (pocket != null)
        {
            saveData.PocketContainer = CaptureContainerData(pocket, visitedItemIds, visitedContainerIds);
        }

        return saveData;
    }

    private ItemSaveData CaptureItemData(
        ItemInstance item,
        HashSet<string> visitedItemIds,
        HashSet<string> visitedContainerIds)
    {
        if (item?.Definition == null || string.IsNullOrEmpty(item.InstanceId))
        {
            return null;
        }

        if (!visitedItemIds.Add(item.InstanceId))
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

        if (item.AttachedContainer != null)
        {
            data.AttachedContainer = CaptureContainerData(item.AttachedContainer, visitedItemIds, visitedContainerIds);
        }

        return data;
    }

    private InventoryContainerSaveData CaptureContainerData(
        InventoryContainer container,
        HashSet<string> visitedItemIds,
        HashSet<string> visitedContainerIds)
    {
        if (container == null || string.IsNullOrEmpty(container.InstanceId))
        {
            return null;
        }

        if (!visitedContainerIds.Add(container.InstanceId))
        {
            return null;
        }

        var data = new InventoryContainerSaveData
        {
            ContainerType = container.Type,
            ContainerName = container.ContainerName
        };

        foreach (var grid in container.PartGrids)
        {
            if (grid == null)
            {
                continue;
            }

            data.GridSizes.Add(new Vector2Int(grid.Width, grid.Height));
        }

        for (int partIndex = 0; partIndex < container.PartGrids.Count; partIndex++)
        {
            var grid = container.PartGrids[partIndex];
            if (grid == null)
            {
                continue;
            }

            foreach (var placement in grid.GetAllPlacements())
            {
                if (placement?.Item == null)
                {
                    continue;
                }

                var itemData = CaptureItemData(placement.Item, visitedItemIds, visitedContainerIds);
                if (itemData == null)
                {
                    continue;
                }

                data.Placements.Add(new ItemPlacementSaveData
                {
                    PartIndex = partIndex,
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
        if (saveData == null)
        {
            return false;
        }

        bool hasAnySavedItem = SaveDataContainsAnyItem(saveData);
        if (hasAnySavedItem && !TryBuildItemDefinitionLookup())
        {
            Debug.LogWarning("InventorySystem: item definition lookup is empty. Cannot load inventory. " +
                             "Please assign SOGameConfig.ItemCatalog.");
            return false;
        }

        var equipment = _model?.PlayerEquipment;
        if (equipment == null)
        {
            _model.PlayerEquipment = new EquipmentContainer();
            equipment = _model.PlayerEquipment;
        }

        ResetPlayerInventoryForLoad(equipment);

        var slotDataMap = new Dictionary<EquipmentSlotType, ItemSaveData>();
        if (saveData.EquippedItems != null)
        {
            foreach (var entry in saveData.EquippedItems)
            {
                if (entry != null && entry.Item != null)
                {
                    slotDataMap[entry.Slot] = entry.Item;
                }
            }
        }

        foreach (var slot in EquipLoadOrder)
        {
            if (!slotDataMap.TryGetValue(slot, out var itemData) || itemData == null)
            {
                continue;
            }

            var item = RestoreItemData(itemData, 0);
            if (item == null)
            {
                continue;
            }

            if (!equipment.TryEquip(slot, item, out _))
            {
                Debug.LogWarning($"InventorySystem: failed to equip loaded item into slot {slot}.");
            }
        }

        var pocket = equipment.GetContainer(InventoryContainerType.Pocket);
        if (pocket == null)
        {
            pocket = CreateContainerFromSave(saveData.PocketContainer, InventoryContainerType.Pocket);
            pocket.ContainerName = string.IsNullOrEmpty(pocket.ContainerName) ? "PlayerPocket" : pocket.ContainerName;
            equipment.TryAddContainer(pocket);
        }

        if (saveData.PocketContainer != null)
        {
            RestoreContainerData(pocket, saveData.PocketContainer, 0);
        }
        else
        {
            ClearContainerContents(pocket);
        }

        RegisterContainerRecursive(pocket);

        return true;
    }

    private bool SaveDataContainsAnyItem(PlayerInventorySaveData saveData)
    {
        if (saveData == null)
        {
            return false;
        }

        if (saveData.EquippedItems != null)
        {
            foreach (var entry in saveData.EquippedItems)
            {
                if (entry?.Item != null)
                {
                    return true;
                }
            }
        }

        return ContainerDataContainsAnyItem(saveData.PocketContainer);
    }

    private bool ContainerDataContainsAnyItem(InventoryContainerSaveData containerData)
    {
        if (containerData?.Placements == null)
        {
            return false;
        }

        foreach (var placement in containerData.Placements)
        {
            if (placement?.Item == null)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ResetPlayerInventoryForLoad(EquipmentContainer equipment)
    {
        var preservedSceneContainers = SnapshotNonPlayerContainers(equipment);

        foreach (EquipmentSlotType slot in Enum.GetValues(typeof(EquipmentSlotType)))
        {
            equipment.TryUnequip(slot, out _);
        }

        foreach (var container in equipment.Containers.Values)
        {
            ClearContainerContents(container);
        }

        if (_model.Containers == null)
        {
            _model.Containers = new Dictionary<string, InventoryContainer>();
        }
        else
        {
            _model.Containers.Clear();
        }

        foreach (var kv in preservedSceneContainers)
        {
            if (!string.IsNullOrEmpty(kv.Key) && kv.Value != null)
            {
                _model.Containers[kv.Key] = kv.Value;
            }
        }

        foreach (var container in equipment.Containers.Values)
        {
            RegisterContainerRecursive(container);
        }
    }

    private Dictionary<string, InventoryContainer> SnapshotNonPlayerContainers(EquipmentContainer equipment)
    {
        var preserved = new Dictionary<string, InventoryContainer>();
        if (_model?.Containers == null || equipment == null)
        {
            return preserved;
        }

        var playerContainerIds = new HashSet<string>();
        CollectPlayerContainerIds(equipment, playerContainerIds);

        foreach (var kv in _model.Containers)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
            {
                continue;
            }

            if (playerContainerIds.Contains(kv.Key))
            {
                continue;
            }

            preserved[kv.Key] = kv.Value;
        }

        return preserved;
    }

    private void CollectPlayerContainerIds(EquipmentContainer equipment, HashSet<string> ids)
    {
        if (equipment == null || ids == null)
        {
            return;
        }

        var visited = new HashSet<string>();

        foreach (var container in equipment.Containers.Values)
        {
            CollectContainerIdsRecursive(container, ids, visited);
        }

        foreach (var item in equipment.Slots.Values)
        {
            if (item?.AttachedContainer != null)
            {
                CollectContainerIdsRecursive(item.AttachedContainer, ids, visited);
            }
        }
    }

    private void CollectContainerIdsRecursive(
        InventoryContainer container,
        HashSet<string> ids,
        HashSet<string> visited)
    {
        if (container == null || string.IsNullOrEmpty(container.InstanceId))
        {
            return;
        }

        if (!visited.Add(container.InstanceId))
        {
            return;
        }

        ids.Add(container.InstanceId);

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
                    CollectContainerIdsRecursive(attached, ids, visited);
                }
            }
        }
    }

    private void ClearContainerContents(InventoryContainer container)
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

            var placements = new List<ItemPlacement>(grid.GetAllPlacements());
            foreach (var placement in placements)
            {
                if (placement?.Item != null)
                {
                    grid.Remove(placement.Item);
                }
            }
        }
    }

    private ItemInstance RestoreItemData(ItemSaveData itemData, int depth)
    {
        if (itemData == null)
        {
            return null;
        }

        if (depth > MaxContainerNestDepth + 2)
        {
            Debug.LogWarning("InventorySystem: container depth exceeded while loading.");
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

        if (itemData.AttachedContainer != null)
        {
            var container = CreateContainerFromSave(itemData.AttachedContainer, itemData.AttachedContainer.ContainerType);
            item.AttachedContainer = container;
            RegisterContainerRecursive(container);
            RestoreContainerData(container, itemData.AttachedContainer, depth + 1);
        }

        return item;
    }

    private InventoryContainer CreateContainerFromSave(InventoryContainerSaveData data, InventoryContainerType fallbackType)
    {
        var type = data != null ? data.ContainerType : fallbackType;
        var container = new InventoryContainer(type);

        if (data != null)
        {
            container.ContainerName = data.ContainerName;
            ApplyContainerGridLayout(container, data);
        }

        return container;
    }

    private void RestoreContainerData(InventoryContainer container, InventoryContainerSaveData data, int depth)
    {
        if (container == null || data == null)
        {
            return;
        }

        ApplyContainerGridLayout(container, data);

        if (data.Placements == null)
        {
            return;
        }

        foreach (var placementData in data.Placements)
        {
            if (placementData == null || placementData.Item == null)
            {
                continue;
            }

            var grid = container.GetGrid(placementData.PartIndex);
            if (grid == null)
            {
                continue;
            }

            var item = RestoreItemData(placementData.Item, depth + 1);
            if (item == null)
            {
                continue;
            }

            if (!grid.Place(item, placementData.Pos, placementData.Rotated))
            {
                Debug.LogWarning($"InventorySystem: failed to place item {item.Definition?.Name} into container {container.ContainerName}.");
                continue;
            }

            if (item.AttachedContainer != null)
            {
                item.AttachedContainer.ParentContainerId = container.InstanceId;
            }
        }

        RegisterContainerRecursive(container);
    }

    private void ApplyContainerGridLayout(InventoryContainer container, InventoryContainerSaveData data)
    {
        if (container == null || data == null)
        {
            return;
        }

        var sizes = data.GridSizes != null && data.GridSizes.Count > 0
            ? data.GridSizes
            : null;

        container.PartGrids.Clear();
        if (sizes == null)
        {
            return;
        }

        for (int i = 0; i < sizes.Count; i++)
        {
            var size = sizes[i];
            container.AddGrid(new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)));
        }
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
            if (entry == null)
            {
                continue;
            }

            RegisterDefinitionToLookup(entry);
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
        return item != null && item.Definition != null && item.Definition.IsContainer;
    }

    private InventoryContainer EnsureAttachedContainer(ItemInstance item)
    {
        if (item == null) return null;
        if (item.AttachedContainer != null) return item.AttachedContainer;
        var containerConfig = item.Definition?.GetRuntimeContainerConfig();
        if (containerConfig == null) return null;

        var container = new InventoryContainer(containerConfig.containerType);
        container.ContainerName = containerConfig.containerName;
        foreach (var part in containerConfig.partGridDatas)
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

    private void AccumulateItemValue(
        ItemInstance item,
        HashSet<string> countedItemIds,
        HashSet<string> visitedContainerIds,
        ref int totalValue)
    {
        if (item?.Definition == null || string.IsNullOrEmpty(item.InstanceId))
        {
            return;
        }

        if (!countedItemIds.Add(item.InstanceId))
        {
            return;
        }

        int unitValue = Mathf.Max(0, item.Definition.Value);
        int itemCount = Mathf.Max(0, item.Count);
        totalValue += unitValue * itemCount;

        if (item.AttachedContainer != null)
        {
            AccumulateContainerValue(item.AttachedContainer, countedItemIds, visitedContainerIds, ref totalValue);
        }
    }

    private void AccumulateContainerValue(
        InventoryContainer container,
        HashSet<string> countedItemIds,
        HashSet<string> visitedContainerIds,
        ref int totalValue)
    {
        if (container == null || string.IsNullOrEmpty(container.InstanceId))
        {
            return;
        }

        if (!visitedContainerIds.Add(container.InstanceId))
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
                AccumulateItemValue(placement?.Item, countedItemIds, visitedContainerIds, ref totalValue);
            }
        }
    }
}
