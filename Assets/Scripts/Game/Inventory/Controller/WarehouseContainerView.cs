using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class WarehouseContainerView : MonoBehaviour, IController
{
    public const string RuntimeWarehouseContainerId = "__warehouse_runtime__";
    private const string WarehouseContainerConfigResKey = "Container_1000";

    public RectTransform containerRoot;

    private ContainerView containerView;
    private PersistentInventoryModel persistentModel;
    private InventoryContainer runtimeContainer;
    private Func<string, int, Vector2Int, bool> onTryTake;
    private Func<string, int, Vector2Int, bool, bool> onTryPlace;

    public bool IsVisible => gameObject.activeSelf;

    public void InitWarehouseContainer()
    {
        if (!IsVisible)
        {
            return;
        }

        if (persistentModel == null)
        {
            persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        EnsureContainerView();
        RebuildRuntimeContainer();
        ApplyCallbacks();
        RenderAll();
    }

    public void RenderAll()
    {
        if (!IsVisible || containerView == null || runtimeContainer == null)
        {
            return;
        }

        containerView.RenderAll(runtimeContainer);
    }

    public void RefreshFromPersistentItems()
    {
        if (!IsVisible)
        {
            return;
        }

        RebuildRuntimeContainer();
        ApplyCallbacks();
        RenderAll();
    }

    public bool TryTidyItems()
    {
        if (persistentModel == null)
        {
            persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        EnsureContainerView();
        if (runtimeContainer == null)
        {
            RebuildRuntimeContainer();
        }

        List<ItemInstance> items = persistentModel?.GetMutableItems();
        if (items == null || runtimeContainer == null)
        {
            return false;
        }

        if (!runtimeContainer.TryTidyItems(items, out List<InventoryGridTidyPlacement> placements))
        {
            Debug.LogWarning("WarehouseContainerView: tidy failed.");
            return false;
        }

        CleanupDepletedPersistentItems(items);
        persistentModel.ClearWarehouseItemPositions();
        for (int i = 0; i < placements.Count; i++)
        {
            InventoryGridTidyPlacement placement = placements[i];
            placement.Item.Rotated = placement.Rotated;
            placement.Item.AttachedContainer = null;
            persistentModel.SetWarehouseItemPosition(placement.Item, placement.PartIndex, placement.Position);
        }

        if (containerView != null)
        {
            containerView.container = runtimeContainer;
        }
        ApplyCallbacks();
        RenderAll();
        return true;
    }

    public void BindCallbacks(
        Func<string, int, Vector2Int, bool> tryTake,
        Func<string, int, Vector2Int, bool, bool> tryPlace)
    {
        onTryTake = tryTake;
        onTryPlace = tryPlace;
        ApplyCallbacks();
    }

    public bool IsRuntimeContainer(string containerId)
    {
        return !string.IsNullOrEmpty(containerId) &&
               string.Equals(containerId, RuntimeWarehouseContainerId, StringComparison.Ordinal);
    }

    public InventoryGrid GetGrid(int partIndex = 0)
    {
        return runtimeContainer?.GetGrid(partIndex);
    }

    public bool TryTakeItemAt(int partIndex, Vector2Int pos, out ItemInstance item, bool notify = true, bool syncPersistent = true)
    {
        item = null;
        if (persistentModel == null)
        {
            persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        var grid = runtimeContainer?.GetGrid(partIndex);
        if (grid == null)
        {
            return false;
        }

        if (!grid.TryTakeAt(pos, out item) || item == null)
        {
            return false;
        }

        if (syncPersistent)
        {
            RemoveFromPersistentItems(item);
        }
        else if (persistentModel != null)
        {
            persistentModel.RemoveWarehouseItemPosition(item);
        }
        if (notify)
        {
            RenderAll();
        }

        return true;
    }

    public bool TryPlaceItem(int partIndex, ItemInstance item, Vector2Int pos, bool rotated, bool notify = true, bool syncPersistent = true)
    {
        if (item?.Definition == null)
        {
            return false;
        }

        if (persistentModel == null)
        {
            persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        var grid = runtimeContainer?.GetGrid(partIndex);
        if (grid == null)
        {
            return false;
        }

        bool hadPersistentRef = ContainsPersistentItem(item);
        if (!grid.PlaceOrStack(item, pos, rotated))
        {
            return false;
        }

        ItemPlacement placement = grid.GetPlacement(item);
        bool itemPlacedAsStandalone = placement != null;
        InventoryContainer attachedContainer = item.AttachedContainer;
        if (itemPlacedAsStandalone)
        {
            item.Rotated = rotated;
            persistentModel?.SetWarehouseItemPosition(item, partIndex, pos);
        }

        // Stash is persisted as a flat item list. If a container item (e.g. backpack/chest rig)
        // enters the warehouse, spill all nested items into the warehouse first, then strip the container link.
        if (itemPlacedAsStandalone && attachedContainer != null)
        {
            if (!SpillContainerItemsIntoWarehouse(attachedContainer))
            {
                grid.Remove(item);
                persistentModel?.RemoveWarehouseItemPosition(item);
                item.AttachedContainer = attachedContainer;
                return false;
            }
        }

        item.AttachedContainer = null;

        if (syncPersistent)
        {
            if (itemPlacedAsStandalone && !hadPersistentRef)
            {
                AddToPersistentItems(item);
            }
            else if (!itemPlacedAsStandalone && hadPersistentRef && item.Count <= 0)
            {
                RemovePersistentItemReference(item, refreshProgress: false);
            }
        }
        if (notify)
        {
            RenderAll();
        }

        return true;
    }

    public bool TryRotateItemInPlace(ItemInstance item)
    {
        if (item?.Definition == null || !item.Definition.IsRotatable || runtimeContainer == null)
        {
            return false;
        }

        if (persistentModel == null)
        {
            persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        for (int partIndex = 0; partIndex < runtimeContainer.PartGrids.Count; partIndex++)
        {
            var grid = runtimeContainer.PartGrids[partIndex];
            if (grid == null)
            {
                continue;
            }

            var placement = grid.GetPlacement(item);
            if (placement == null)
            {
                continue;
            }

            var rotated = !placement.Rotated;
            if (!grid.Move(item, placement.Pos, rotated))
            {
                return false;
            }

            item.Rotated = rotated;
            persistentModel?.SetWarehouseItemPosition(item, partIndex, placement.Pos);
            RenderAll();
            return true;
        }

        return false;
    }

    public void AddToPersistentItems(ItemInstance item)
    {
        if (item?.Definition == null || item.Count <= 0)
        {
            return;
        }

        if (persistentModel == null)
        {
            persistentModel = this.GetModel<PersistentInventoryModel>();
        }

        item.AttachedContainer = null;
        List<ItemInstance> items = persistentModel?.GetMutableItems();
        if (items == null)
        {
            return;
        }

        if (!items.Contains(item))
        {
            items.Add(item);
            this.GetSystem<PlayerProgressSystem>()?.RefreshProgress();
        }
    }

    public void RemoveFromPersistentItems(ItemInstance item)
    {
        if (item?.Definition == null)
        {
            return;
        }

        if (RemovePersistentItemReference(item, refreshProgress: true))
        {
            return;
        }

        RemoveFromPersistentItems(item.Definition, Mathf.Max(1, item.Count));
    }

    public void RemoveFromPersistentItems(SOItemConfig definition, int count)
    {
        RemoveFromPersistentItemsInternal(definition, count);
    }

    private void EnsureContainerView()
    {
        if (containerRoot == null)
        {
            Debug.LogWarning("WarehouseContainerView: containerRoot is null.");
            return;
        }

        if (containerView == null)
        {
            containerView = containerRoot.GetComponentInChildren<ContainerView>(true);
        }

        if (containerView == null)
        {
            Debug.LogWarning("WarehouseContainerView: missing ContainerView under containerRoot.");
            return;
        }

        ApplyCallbacks();
    }

    private void RebuildRuntimeContainer()
    {
        if (!TryResolveContainerTemplate(out string containerName, out List<Vector2Int> gridSizes))
        {
            runtimeContainer = null;
            return;
        }

        runtimeContainer = new InventoryContainer(InventoryContainerType.Stash)
        {
            ContainerName = containerName
        };
        runtimeContainer.InstanceId = RuntimeWarehouseContainerId;

        for (int i = 0; i < gridSizes.Count; i++)
        {
            runtimeContainer.AddGrid(gridSizes[i]);
        }

        var items = persistentModel?.GetMutableItems();
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            PlacePersistentItemIntoRuntimeContainer(items[i]);
        }

        if (containerView != null)
        {
            containerView.container = runtimeContainer;
        }
    }

    private void ApplyCallbacks()
    {
        if (containerView == null || onTryTake == null || onTryPlace == null)
        {
            return;
        }

        containerView.BindCallbacks(onTryTake, onTryPlace);
    }

    private bool TryResolveContainerTemplate(out string containerName, out List<Vector2Int> gridSizes)
    {
        containerName = null;
        gridSizes = new List<Vector2Int>();

        SOContainerConfig containerConfig = LoadWarehouseContainerConfig();
        if (containerConfig == null)
        {
            Debug.LogWarning($"WarehouseContainerView: failed to load warehouse container config '{WarehouseContainerConfigResKey}'.");
            return false;
        }

        containerName = containerConfig.ContainerName;
        if (containerConfig.PartGridDatas != null)
        {
            for (int partIndex = 0; partIndex < containerConfig.PartGridDatas.Count; partIndex++)
            {
                Vector2Int size = containerConfig.PartGridDatas[partIndex].Size;
                gridSizes.Add(new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)));
            }
        }

        if (string.IsNullOrEmpty(containerName) || gridSizes.Count == 0)
        {
            Debug.LogWarning($"WarehouseContainerView: warehouse container config '{WarehouseContainerConfigResKey}' is invalid.");
            return false;
        }

        return true;
    }

    private SOContainerConfig LoadWarehouseContainerConfig()
    {
        IResLoader resLoader = this.GetUtility<IResLoader>();
        if (resLoader == null)
        {
            return null;
        }

        return resLoader.LoadSync<SOContainerConfig>(WarehouseContainerConfigResKey);
    }

    private void CleanupDepletedPersistentItems(List<ItemInstance> items)
    {
        if (items == null || persistentModel == null)
        {
            return;
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            ItemInstance item = items[i];
            if (item?.Definition != null && item.Count > 0)
            {
                continue;
            }

            persistentModel.RemoveWarehouseItemPosition(item);
            items.RemoveAt(i);
        }
    }

    private void PlacePersistentItemIntoRuntimeContainer(ItemInstance item)
    {
        if (item?.Definition == null || runtimeContainer == null)
        {
            return;
        }

        item.AttachedContainer = null;
        bool placed = false;

        if (persistentModel != null &&
            persistentModel.TryGetWarehouseItemPosition(item, out int savedPartIndex, out Vector2Int savedPos))
        {
            InventoryGrid savedGrid = runtimeContainer.GetGrid(savedPartIndex);
            if (savedGrid != null && savedGrid.Place(item, savedPos, item.Rotated))
            {
                placed = true;
                persistentModel.SetWarehouseItemPosition(item, savedPartIndex, savedPos);
            }
        }

        if (!placed && TryPlaceToAnyGrid(item, out int partIndex, out Vector2Int pos, out bool rotated))
        {
            item.Rotated = rotated;
            persistentModel?.SetWarehouseItemPosition(item, partIndex, pos);
            placed = true;
        }

        if (!placed)
        {
            persistentModel?.RemoveWarehouseItemPosition(item);
            Debug.LogWarning($"WarehouseContainerView: no space for item {item.Definition.Name}.");
        }
    }

    private bool TryPlaceToAnyGrid(
        InventoryContainer container,
        ItemInstance item,
        out int partIndex,
        out Vector2Int pos,
        out bool rotated)
    {
        partIndex = -1;
        pos = new Vector2Int(-1, -1);
        rotated = item != null && item.Rotated;
        if (item == null || container == null)
        {
            return false;
        }

        for (int i = 0; i < container.PartGrids.Count; i++)
        {
            InventoryGrid grid = container.PartGrids[i];
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

    private bool TryPlaceToAnyGrid(ItemInstance item, out int partIndex, out Vector2Int pos, out bool rotated)
    {
        return TryPlaceToAnyGrid(runtimeContainer, item, out partIndex, out pos, out rotated);
    }

    private struct SpilledItemRecord
    {
        public ItemInstance Item;
        public InventoryGrid SourceGrid;
        public Vector2Int SourcePos;
        public bool SourceRotated;
        public InventoryContainer OriginalAttachedContainer;
        public int WarehousePartIndex;
        public Vector2Int WarehousePos;
        public bool WasPlacedInWarehouse;
    }

    private bool SpillContainerItemsIntoWarehouse(InventoryContainer sourceContainer)
    {
        if (sourceContainer == null || runtimeContainer == null)
        {
            return true;
        }

        List<SpilledItemRecord> extractedItems = new List<SpilledItemRecord>();
        CollectContainerItemsRecursive(sourceContainer, extractedItems, new HashSet<string>(), new HashSet<string>());

        for (int i = 0; i < extractedItems.Count; i++)
        {
            SpilledItemRecord record = extractedItems[i];
            ItemInstance extracted = record.Item;
            if (extracted?.Definition == null)
            {
                continue;
            }

            if (!TryPlaceToAnyGrid(extracted, out int extractedPartIndex, out Vector2Int extractedPos, out bool extractedRotated))
            {
                Debug.LogWarning($"WarehouseContainerView: no space to spill nested item {extracted.Definition.Name} into warehouse.");
                RollbackSpilledItems(extractedItems);
                return false;
            }

            extracted.Rotated = extractedRotated;
            persistentModel?.SetWarehouseItemPosition(extracted, extractedPartIndex, extractedPos);
            AddToPersistentItems(extracted);

            record.WarehousePartIndex = extractedPartIndex;
            record.WarehousePos = extractedPos;
            record.WasPlacedInWarehouse = true;
            extractedItems[i] = record;
        }

        return true;
    }

    private void CollectContainerItemsRecursive(
        InventoryContainer container,
        List<SpilledItemRecord> output,
        HashSet<string> visitedContainerIds,
        HashSet<string> visitedItemIds)
    {
        if (container == null || output == null || visitedContainerIds == null || visitedItemIds == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(container.InstanceId) && !visitedContainerIds.Add(container.InstanceId))
        {
            return;
        }

        if (container.PartGrids == null)
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

            List<ItemPlacement> placements = new List<ItemPlacement>(grid.GetAllPlacements());
            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                ItemPlacement placement = placements[placementIndex];
                ItemInstance item = placement?.Item;
                if (item?.Definition == null || placement == null)
                {
                    continue;
                }

                grid.Remove(item);

                if (!string.IsNullOrEmpty(item.InstanceId) && !visitedItemIds.Add(item.InstanceId))
                {
                    continue;
                }

                // Recursively flatten nested container items before the warehouse strips container links.
                if (item.AttachedContainer != null)
                {
                    CollectContainerItemsRecursive(item.AttachedContainer, output, visitedContainerIds, visitedItemIds);
                }

                output.Add(new SpilledItemRecord
                {
                    Item = item,
                    SourceGrid = grid,
                    SourcePos = placement.Pos,
                    SourceRotated = placement.Rotated,
                    OriginalAttachedContainer = item.AttachedContainer,
                    WarehousePartIndex = -1,
                    WarehousePos = new Vector2Int(-1, -1),
                    WasPlacedInWarehouse = false
                });
            }
        }
    }

    private void RollbackSpilledItems(List<SpilledItemRecord> records)
    {
        if (records == null)
        {
            return;
        }

        List<ItemInstance> persistentItems = persistentModel?.GetMutableItems();

        for (int i = 0; i < records.Count; i++)
        {
            SpilledItemRecord record = records[i];
            if (!record.WasPlacedInWarehouse)
            {
                continue;
            }

            InventoryGrid warehouseGrid = runtimeContainer?.GetGrid(record.WarehousePartIndex);
            warehouseGrid?.Remove(record.Item);
            persistentModel?.RemoveWarehouseItemPosition(record.Item);
            persistentItems?.Remove(record.Item);
            record.Item.AttachedContainer = record.OriginalAttachedContainer;
        }

        for (int i = 0; i < records.Count; i++)
        {
            SpilledItemRecord record = records[i];
            if (record.Item?.Definition == null || record.SourceGrid == null)
            {
                continue;
            }

            record.Item.AttachedContainer = record.OriginalAttachedContainer;
            record.SourceGrid.Place(record.Item, record.SourcePos, record.SourceRotated);
        }
    }

    private void RemoveFromPersistentItemsInternal(SOItemConfig definition, int count)
    {
        if (definition == null || count <= 0 || persistentModel == null)
        {
            return;
        }

        var items = persistentModel.GetMutableItems();
        int remaining = count;
        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            ItemInstance current = items[i];
            if (current?.Definition == null)
            {
                continue;
            }

            if (current.Definition.Id > 0 && definition.Id > 0)
            {
                if (current.Definition.Id != definition.Id)
                {
                    continue;
                }
            }
            else if (!ReferenceEquals(current.Definition, definition) &&
                     !string.Equals(current.Definition.ResName, definition.ResName, StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(current.Definition.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int currentCount = Mathf.Max(1, current.Count);
            if (currentCount <= remaining)
            {
                remaining -= currentCount;
                items.RemoveAt(i);
                persistentModel.RemoveWarehouseItemPosition(current);
                i--;
            }
            else
            {
                current.Count = currentCount - remaining;
                remaining = 0;
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"WarehouseContainerView: persistent item count mismatch for {definition.Name}, missing={remaining}.");
        }

        if (remaining != count)
        {
            this.GetSystem<PlayerProgressSystem>()?.RefreshProgress();
        }
    }

    private bool ContainsPersistentItem(ItemInstance item)
    {
        if (item == null || persistentModel == null)
        {
            return false;
        }

        List<ItemInstance> items = persistentModel.GetMutableItems();
        return items != null && items.Contains(item);
    }

    public bool RemovePersistentItemReference(ItemInstance item, bool refreshProgress)
    {
        if (item?.Definition == null || persistentModel == null)
        {
            return false;
        }

        List<ItemInstance> items = persistentModel.GetMutableItems();
        if (items == null || items.Remove(item) == false)
        {
            return false;
        }

        persistentModel.RemoveWarehouseItemPosition(item);
        if (refreshProgress)
        {
            this.GetSystem<PlayerProgressSystem>()?.RefreshProgress();
        }

        return true;
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
