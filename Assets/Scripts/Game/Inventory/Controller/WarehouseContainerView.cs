using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class WarehouseContainerView : MonoBehaviour, IController
{
    public const string RuntimeWarehouseContainerId = "__warehouse_runtime__";

    public RectTransform containerRoot;
    [Tooltip("Optional override for container prefab name. Leave empty to use Stash config.")]
    public string containerPrefabNameOverride;
    [Tooltip("Fallback grid size used when no stash config exists.")]
    public Vector2Int fallbackGridSize = new Vector2Int(10, 8);

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

        if (!grid.Place(item, pos, rotated))
        {
            return false;
        }

        item.Rotated = rotated;
        item.AttachedContainer = null;
        persistentModel?.SetWarehouseItemPosition(item, partIndex, pos);

        if (syncPersistent)
        {
            AddToPersistentItems(item);
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
        if (item?.Definition == null)
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
        }
    }

    public void RemoveFromPersistentItems(ItemInstance item)
    {
        if (item?.Definition == null)
        {
            return;
        }

        if (persistentModel != null)
        {
            List<ItemInstance> items = persistentModel.GetMutableItems();
            if (items != null && items.Remove(item))
            {
                persistentModel.RemoveWarehouseItemPosition(item);
                return;
            }
        }

        RemoveFromPersistentItems(item.Definition, Mathf.Max(1, item.Count));
    }

    public void RemoveFromPersistentItems(ItemCatalogEntry definition, int count)
    {
        RemoveFromPersistentItemsInternal(definition, count);
    }

    private void EnsureContainerView()
    {
        if (containerRoot == null)
        {
            return;
        }

        ResolveContainerTemplate(out var containerName, out _);
        if (string.IsNullOrEmpty(containerName))
        {
            return;
        }

        if (containerView != null &&
            containerView.container != null &&
            string.Equals(containerView.container.ContainerName, containerName, StringComparison.Ordinal))
        {
            return;
        }

        if (containerView != null)
        {
            Destroy(containerView.gameObject);
        }

        var prefab = this.GetUtility<IResLoader>()?.LoadSync<GameObject>(containerName);
        if (prefab == null)
        {
            Debug.LogWarning($"WarehouseContainerView: missing container prefab {containerName}.");
            return;
        }

        var instance = Instantiate(prefab, containerRoot);
        containerView = instance.GetComponent<ContainerView>();
        if (containerView == null)
        {
            Debug.LogWarning("WarehouseContainerView: container prefab missing ContainerView component.");
            return;
        }

        ApplyCallbacks();
    }

    private void RebuildRuntimeContainer()
    {
        ResolveContainerTemplate(out var containerName, out var gridSizes);
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

    private void ResolveContainerTemplate(out string containerName, out List<Vector2Int> gridSizes)
    {
        containerName = string.IsNullOrEmpty(containerPrefabNameOverride)
            ? null
            : containerPrefabNameOverride;
        gridSizes = new List<Vector2Int>();

        var containerCatalog = GameSettingManager.Instance?.Config?.ContainerCatalog;
        if (containerCatalog != null)
        {
            var entries = containerCatalog.GetRuntimeConfigs();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.ContainerType != InventoryContainerType.Stash)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(containerName))
                {
                    containerName = entry.ContainerName;
                }

                if (entry.PartGridDatas != null)
                {
                    for (int partIndex = 0; partIndex < entry.PartGridDatas.Count; partIndex++)
                    {
                        var size = entry.PartGridDatas[partIndex].Size;
                        gridSizes.Add(new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)));
                    }
                }

                break;
            }
        }

        if (string.IsNullOrEmpty(containerName))
        {
            containerName = "PlayerBackpack";
        }

        if (gridSizes.Count == 0)
        {
            gridSizes.Add(new Vector2Int(
                Mathf.Max(1, fallbackGridSize.x),
                Mathf.Max(1, fallbackGridSize.y)));
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

    private bool TryPlaceToAnyGrid(ItemInstance item, out int partIndex, out Vector2Int pos, out bool rotated)
    {
        partIndex = -1;
        pos = new Vector2Int(-1, -1);
        rotated = item != null && item.Rotated;
        if (item == null || runtimeContainer == null)
        {
            return false;
        }

        for (int i = 0; i < runtimeContainer.PartGrids.Count; i++)
        {
            InventoryGrid grid = runtimeContainer.PartGrids[i];
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

    private void RemoveFromPersistentItemsInternal(ItemCatalogEntry definition, int count)
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
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
