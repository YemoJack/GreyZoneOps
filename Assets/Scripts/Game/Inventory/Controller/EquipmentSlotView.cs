using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotView : MonoBehaviour, IController, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public EquipmentSlotType slotType;
    public Image bgIcon;
    public Image iconBG;
    public Image icon;
    public Text nameText;
    public InventoryItemView dragItemPrefab;
    public Vector2 dragCellSize = new Vector2(60f, 60f);
    public Vector2 dragSpacing = Vector2.zero;

    private System.Func<EquipmentSlotType, bool> onTryTake;
    private System.Func<EquipmentSlotType, bool> onTryPlace;
    private System.Func<EquipmentSlotType, EquipmentSlotType, bool> onTrySwap;
    private System.Func<EquipmentSlotType, ItemInstance> onBeginDrag;
    private System.Func<EquipmentSlotType, ItemInstance, bool> onReturnDrag;
    private ItemInstance currentItem;
    private ItemInstance draggingItem;
    private InventoryItemView dragItemInstance;
    private RectTransform dragRect;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private Vector2 pointerOffset = Vector2.zero;
    private IEquipmentDragHost dragHost;

    public EquipmentSlotType SlotType => slotType;

    public void BindCallbacks(
        System.Func<EquipmentSlotType, bool> tryTake,
        System.Func<EquipmentSlotType, bool> tryPlace,
        System.Func<EquipmentSlotType, EquipmentSlotType, bool> trySwap,
        System.Func<EquipmentSlotType, ItemInstance> beginDrag,
        System.Func<EquipmentSlotType, ItemInstance, bool> returnDrag)
    {
        onTryTake = tryTake;
        onTryPlace = tryPlace;
        onTrySwap = trySwap;
        onBeginDrag = beginDrag;
        onReturnDrag = returnDrag;
        if (dragRect == null && icon != null)
        {
            dragRect = icon.rectTransform;
        }
        if (canvasGroup == null && dragRect != null)
        {
            canvasGroup = dragRect.GetComponent<CanvasGroup>();
        }
    }

    public void Render(ItemInstance item)
    {
        if (draggingItem != null) return;
        currentItem = item;


        if (nameText != null)
        {
            nameText.text = item != null && item.Definition != null ? item.Definition.Name : "";
        }
        if (icon != null)
        {
            icon.sprite = item != null && item.Definition != null ? item.Definition.Icon : null;
        }

        //bgIcon.raycastTarget = item != null && item.Definition != null ? true : false;
    }

    public bool TryHandleDrop()
    {
        if (onTryPlace == null)
        {
            Debug.LogError("EquipmentSlotView onTryPlace is null");
            return false;
        }

        return onTryPlace.Invoke(slotType);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onTryTake == null) return;
        if (eventData.clickCount < 2) return;
        onTryTake.Invoke(slotType);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanBeginDrag(eventData)) return;
        if (currentItem == null || onBeginDrag == null) return;

        draggingItem = onBeginDrag.Invoke(slotType);
        if (draggingItem == null) return;

        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
        canvasCamera = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        if (rootCanvas != null && dragItemPrefab != null)
        {
            dragItemInstance = Instantiate(dragItemPrefab, rootCanvas.transform);
            dragItemInstance.BindDrag(draggingItem, dragCellSize, dragSpacing);
            dragRect = dragItemInstance.transform as RectTransform;
            if (dragRect != null)
            {
                dragRect.SetAsLastSibling();
                canvasGroup = dragItemInstance.GetComponent<CanvasGroup>();
            }
        }
        else if (dragRect != null && rootCanvas != null)
        {
            originalParent = dragRect.parent;
            originalAnchoredPos = dragRect.anchoredPosition;
            dragRect.SetParent(rootCanvas.transform, worldPositionStays: true);
            dragRect.SetAsLastSibling();
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }

        if (dragRect != null)
        {
            pointerOffset = CalculatePointerOffset(eventData);
            UpdatePositionToPointer(eventData);
        }
    }

    private bool CanBeginDrag(PointerEventData eventData)
    {
        if (iconBG == null) return true;
        var rect = iconBG.rectTransform;
        if (rect == null) return true;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.pressPosition, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingItem == null || dragRect == null) return;
        UpdatePositionToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingItem == null || dragRect == null) return;
        var item = draggingItem;
        draggingItem = null;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (item.Definition == null)
        {
            if (onReturnDrag != null) onReturnDrag.Invoke(slotType, item);
            RestoreParent();
            return;
        }

        var targetSlot = FindEquipmentSlotUnderPointer(eventData.position);
        if (targetSlot != null && targetSlot != this)
        {
            if (onTrySwap != null && onTrySwap.Invoke(slotType, targetSlot.slotType))
            {
                RestoreParent();
                return;
            }

            var placedInSlot = targetSlot.TryHandleDrop();
            if (placedInSlot)
            {
                RestoreParent();
                return;
            }
        }

        var targetGrid = FindGridViewUnderPointer(eventData.position, eventData.pressEventCamera);
        var placed = false;
        if (targetGrid != null && targetGrid.OnTryPlace != null)
        {
            var pos = ScreenToCell(targetGrid, eventData.position, eventData.pressEventCamera, item.Definition.Size);
            if (pos.x >= 0 && pos.y >= 0)
            {
                var window = GetDragHost();
                var containerView = targetGrid.GetComponentInParent<ContainerView>();
                var containerId = containerView != null && containerView.container != null
                    ? containerView.container.InstanceId
                    : null;
                if (window != null && !string.IsNullOrEmpty(containerId))
                {
                    placed = window.TryPlaceEquipItemToContainer(item, containerId, targetGrid.partIndex, pos, false);
                }
                else
                {
                    placed = targetGrid.OnTryPlace(targetGrid.partIndex, pos, false);
                }
            }
        }

        if (placed)
        {
            RestoreParent();
            return;
        }

        if (!placed && onReturnDrag != null && IsInsideAllowedArea(eventData.position, eventData.pressEventCamera))
        {
            onReturnDrag.Invoke(slotType, item);
        }
        else
        {
            DropItem(item);
            ClearDraggingState();
        }

        RestoreParent();
    }

    private void RestoreParent()
    {
        if (dragItemInstance != null)
        {
            Destroy(dragItemInstance.gameObject);
            dragItemInstance = null;
            dragRect = icon != null ? icon.rectTransform : transform as RectTransform;
            return;
        }
        if (dragRect == null) return;
        var targetParent = originalParent;
        if (targetParent != null)
        {
            dragRect.SetParent(targetParent, worldPositionStays: false);
            dragRect.anchoredPosition = originalAnchoredPos;
        }
    }

    private void UpdatePositionToPointer(PointerEventData e)
    {
        var targetRect = canvasRect != null ? canvasRect : dragRect;
        var cam = canvasCamera != null ? canvasCamera : e.pressEventCamera;
        if (targetRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, e.position, cam, out var localPoint))
        {
            dragRect.localPosition = localPoint + pointerOffset;
        }
    }

    private Vector2 CalculatePointerOffset(PointerEventData e)
    {
        var size = dragRect.rect.size;
        var pivot = dragRect.pivot;
        return new Vector2(
            -size.x * (0.5f - pivot.x),
            -size.y * (0.5f - pivot.y));
    }

    private InventoryGridView FindGridViewUnderPointer(Vector2 screenPos, Camera cam)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        var es = EventSystem.current;
        if (es == null) return null;

        var eventData = new PointerEventData(es)
        {
            position = screenPos
        };
        es.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var gv = r.gameObject.GetComponentInParent<InventoryGridView>();
            if (gv != null) return gv;
        }
        return null;
    }

    private EquipmentSlotView FindEquipmentSlotUnderPointer(Vector2 screenPos)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        var es = EventSystem.current;
        if (es == null) return null;

        var eventData = new PointerEventData(es)
        {
            position = screenPos
        };
        es.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponentInParent<EquipmentSlotView>();
            if (slot != null) return slot;
        }
        return null;
    }

    private Vector2Int ScreenToCell(InventoryGridView targetGrid, Vector2 screenPos, Camera cam, Vector2Int size)
    {
        if (targetGrid == null || targetGrid.layout == null || targetGrid.cellRoot == null)
            return new Vector2Int(-1, -1);

        var targetRect = targetGrid.cellRoot;
        var targetLayout = targetGrid.layout;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPos, cam, out var localPoint))
            return new Vector2Int(-1, -1);

        var padding = targetLayout.padding;
        var cell = targetLayout.cellSize;
        var spacing = targetLayout.spacing;
        var rectSize = targetRect.rect.size;
        var pivot = targetRect.pivot;

        float originX = localPoint.x + rectSize.x * pivot.x;
        float originY = -localPoint.y + rectSize.y * (1f - pivot.y);

        var cellSpan = new Vector2Int(size.x, size.y);
        float width = cell.x * cellSpan.x + spacing.x * Mathf.Max(0, cellSpan.x - 1);
        float height = cell.y * cellSpan.y + spacing.y * Mathf.Max(0, cellSpan.y - 1);

        float localX = originX - padding.left - width * 0.5f;
        float localY = originY - padding.top - height * 0.5f;

        float stepX = cell.x + spacing.x;
        float stepY = cell.y + spacing.y;

        int x = Mathf.RoundToInt(localX / stepX);
        int y = Mathf.RoundToInt(localY / stepY);

        if (x < 0 || y < 0 || x > targetGrid.GridWidth - cellSpan.x || y > targetGrid.GridHeight - cellSpan.y)
            return new Vector2Int(-1, -1);

        return new Vector2Int(x, y);
    }

    private bool IsInsideAllowedArea(Vector2 screenPos, Camera cam)
    {
        var playerView = GetComponentInParent<PlayerInventoryView>();
        if (playerView == null) return false;
        var rect = playerView.transform as RectTransform;
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
    }

    private void ClearDraggingState()
    {
        var window = GetDragHost();
        if (window != null)
        {
            window.ClearDraggingItem();
        }
    }

    private void DropItem(ItemInstance item)
    {
        var window = GetDragHost();
        if (window != null)
        {
            window.DropItem(item);
        }
    }

    private IEquipmentDragHost GetDragHost()
    {
        if (dragHost != null)
        {
            return dragHost;
        }

        MonoBehaviour[] parents = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < parents.Length; i++)
        {
            if (parents[i] is IEquipmentDragHost host)
            {
                dragHost = host;
                return dragHost;
            }
        }

        // ZMUI windows are WindowBase objects (not MonoBehaviour), so parent traversal cannot find them.
        if (UIModule.Instance.TryGetWindow<WarehouseWindow>(out var warehouseWindow) && warehouseWindow != null)
        {
            dragHost = warehouseWindow;
            return dragHost;
        }

        if (UIModule.Instance.TryGetWindow<InventoryWindow>(out var inventoryWindow) && inventoryWindow != null)
        {
            dragHost = inventoryWindow;
            return dragHost;
        }

        return null;
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
