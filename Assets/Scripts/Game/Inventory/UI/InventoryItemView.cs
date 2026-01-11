using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image bgIcon;
    public Image icon;
    public Text countText;
    private CanvasGroup canvasGroup;

    private RectTransform rect;
    private RectTransform gridRect;
    private RectTransform itemRoot;
    private GridLayoutGroup layout;

    private InventoryGridView currentTargetGrid;

    private ItemPlacement placement;
    private System.Func<bool> onBegin;
    private System.Func<Vector2Int, bool, bool> onDrop;
    private bool rotated => placement.Rotated;
    private bool dragging;
    private Vector2Int cellSpan = Vector2Int.one;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private Vector2 pointerOffset = Vector2.zero;
    private ContainerView originContainerView;

    private void Awake()
    {
        rect = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetupGrid(
        GridLayoutGroup gridLayout,
        RectTransform gridSpace,
        RectTransform itemParent)
    {
        layout = gridLayout;
        gridRect = gridSpace;
        itemRoot = itemParent;
        if (itemRoot != null && rect != null && rect.parent != itemRoot)
        {
            rect.SetParent(itemRoot, worldPositionStays: false);
        }

    }

    public void Bind(ItemPlacement p)
    {
        placement = p;
        countText.text = p.Item.Count > 1 ? p.Item.Count.ToString() : "";
        rect.anchoredPosition = CellToLocal(p.Pos);
        SetSize(p.Size, rotated);

        icon.sprite = p.Item.Definition.icon;
    }

    public void BindDrag(ItemInstance item, Vector2 cellSize, Vector2 spacing)
    {
        if (item == null || item.Definition == null) return;
        placement = null;
        countText.text = item.Count > 1 ? item.Count.ToString() : "";
        var size = item.Definition.Size;
        var w = size.x;
        var h = size.y;
        var width = cellSize.x * w + spacing.x * Mathf.Max(0, w - 1);
        var height = cellSize.y * h + spacing.y * Mathf.Max(0, h - 1);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        if (icon != null) icon.enabled = true;

        icon.sprite = item.Definition.icon;
    }

    public void SetDragCallbacks(System.Func<bool> onBegin, System.Func<Vector2Int, bool, bool> onDrop)
    {
        this.onBegin = onBegin;
        this.onDrop = onDrop;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (onBegin != null && !onBegin.Invoke())
            return;

        originContainerView = GetComponentInParent<ContainerView>();
        originalParent = rect.parent;
        originalAnchoredPos = rect.anchoredPosition;
        currentTargetGrid = null;
        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
        canvasCamera = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        // 把拖拽物提升到最顶层，避免被其他容器遮挡
        if (rootCanvas != null)
        {
            rect.SetParent(rootCanvas.transform, worldPositionStays: true);
            rect.SetAsLastSibling();
        }

        dragging = true;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }
        Debug.Log($"Item OnBeginDrag id: {placement.Item.Definition.Id} Name: {placement.Item.Definition.Name} startPos{placement.Pos}\n InstanceId {placement.Item.InstanceId}  ");

        // 初始时就让物品中心对齐指针
        pointerOffset = CalculatePointerOffset(e);
        UpdatePositionToPointer(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        UpdatePositionToPointer(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        var equipSlot = FindEquipmentSlotUnderPointer(e.position);
        if (equipSlot != null)
        {
            var handled = equipSlot.TryHandleDrop();
            RestoreParent();
            if (handled) return;
            if (onDrop != null)
            {
                onDrop.Invoke(new Vector2Int(-1, -1), rotated);
            }
            return;
        }

        var gridPos = ScreenToCell(e.position, e.pressEventCamera, out var targetGrid);
        var isOutside = targetGrid == null;
        if (isOutside)
        {
            // 特殊标记：完全拖出任何网格，交由上层判定为丢弃
            gridPos = IsInsideAllowedArea(e.position, e.pressEventCamera)
                ? new Vector2Int(-1, -1)
                : new Vector2Int(int.MinValue, int.MinValue);
        }

        var target = targetGrid;
        var placed = false;
        if (target != null && target.OnTryPlace != null && gridPos.x >= 0 && gridPos.y >= 0)
        {
            placed = target.OnTryPlace(target.partIndex, gridPos, rotated);
        }
        else if (onDrop != null)
        {
            // targetGrid 为空视为丢弃，传递 (-1,-1) 让上层决定如何处理
            placed = onDrop.Invoke(gridPos, rotated);
        }

        Debug.Log($"Item OnEndDrag  id: {placement.Item.Definition.Id} Name: {placement.Item.Definition.Name} endPos {gridPos} placed:{placed} target:{target?.name}\n InstanceId {placement.Item.InstanceId} ");


        // 恢复父节点，具体位置会在后续刷新时由绑定更新
        RestoreParent();
    }

    private void RestoreParent()
    {
        var targetParent = itemRoot != null ? (Transform)itemRoot : originalParent;
        if (targetParent != null)
        {
            rect.SetParent(targetParent, worldPositionStays: false);
            rect.anchoredPosition = originalAnchoredPos;
        }
    }

    private void UpdatePositionToPointer(PointerEventData e)
    {
        var targetRect = canvasRect != null ? canvasRect : gridRect;
        var cam = canvasCamera != null ? canvasCamera : e.pressEventCamera;
        if (targetRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, e.position, cam, out var localPoint))
        {
            rect.localPosition = localPoint + pointerOffset;
        }
    }

    /// <summary>计算当前鼠标位置与物品中心的偏移，保证拖拽时中心对齐。</summary>
    private Vector2 CalculatePointerOffset(PointerEventData e)
    {
        // 直接用尺寸与 pivot 计算中心相对左上角的偏移
        var size = rect.rect.size;
        var pivot = rect.pivot;
        // 当 pivot 为(0.5,0.5)时偏移为0，否则根据 pivot 计算中心偏移
        return new Vector2(
            -size.x * (0.5f - pivot.x),
            -size.y * (0.5f - pivot.y));
    }

    private void SetSize(Vector2Int size, bool isRotated)
    {
        var cell = layout.cellSize;
        var spacing = layout.spacing;
        var w = isRotated ? size.y : size.x;
        var h = isRotated ? size.x : size.y;
        cellSpan = new Vector2Int(w, h);
        var width = cell.x * w + spacing.x * Mathf.Max(0, w - 1);
        var height = cell.y * h + spacing.y * Mathf.Max(0, h - 1);
        rect.sizeDelta = new Vector2(width, height);
    }

    private Vector2 CellToLocal(Vector2Int pos)
    {
        var cell = layout.cellSize;
        var spacing = layout.spacing;
        var padding = layout.padding;
        var parentRect = gridRect != null ? gridRect : rect.parent as RectTransform;

        // 顶部左侧为(0,0)，计算物品占用区域的中心位置
        float width = cell.x * cellSpan.x + spacing.x * Mathf.Max(0, cellSpan.x - 1);
        float height = cell.y * cellSpan.y + spacing.y * Mathf.Max(0, cellSpan.y - 1);

        // 考虑父节点 pivot/anchor，原点在父节点 pivot 处
        float originX = 0f;
        float originY = 0f;
        if (parentRect != null)
        {
            var rectSize = parentRect.rect.size;
            var pivot = parentRect.pivot;
            originX = -rectSize.x * pivot.x;
            originY = rectSize.y * (1f - pivot.y);
        }

        float startX = originX + padding.left + pos.x * (cell.x + spacing.x);
        float startY = originY - padding.top - pos.y * (cell.y + spacing.y);

        float x = startX + width * 0.5f;
        float y = startY - height * 0.5f;
        return new Vector2(x, y);
    }

    private Vector2Int ScreenToCell(Vector2 screenPos, Camera cam, out InventoryGridView targetGrid)
    {
        // 优先命中指针下的网格；未命中则视为拖出网格（丢弃）
        currentTargetGrid = FindGridViewUnderPointer(screenPos, cam);
        targetGrid = currentTargetGrid;
        if (targetGrid == null || targetGrid.layout == null || targetGrid.cellRoot == null)
            return new Vector2Int(-1, -1);

        var targetRect = targetGrid.cellRoot;
        var targetLayout = targetGrid.layout;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPos, cam, out var localPoint))
            return new Vector2Int(-1, -1);

        var padding = targetLayout.padding;
        var cell = targetLayout.cellSize;
        var spacing = targetLayout.spacing;
        var size = targetRect.rect.size;
        var pivot = targetRect.pivot;

        // 将以 pivot 为原点的坐标，转换为以左上角为原点的坐标系
        float originX = localPoint.x + size.x * pivot.x;
        float originY = -localPoint.y + size.y * (1f - pivot.y);

        // 指针在物品中心，将中心换算成左上角起点
        float width = cell.x * cellSpan.x + spacing.x * Mathf.Max(0, cellSpan.x - 1);
        float height = cell.y * cellSpan.y + spacing.y * Mathf.Max(0, cellSpan.y - 1);

        float localX = originX - padding.left - width * 0.5f;
        float localY = originY - padding.top - height * 0.5f;

        float stepX = cell.x + spacing.x;
        float stepY = cell.y + spacing.y;

        // 将指针位置映射到距离左上角最近的格子（四舍五入而非向下取整）
        int x = Mathf.RoundToInt(localX / stepX);
        int y = Mathf.RoundToInt(localY / stepY);

        // 确保剩余空间足够放下该物品
        if (x < 0 || y < 0 || x > targetGrid.GridWidth - cellSpan.x || y > targetGrid.GridHeight - cellSpan.y)
            return new Vector2Int(-1, -1);

        return new Vector2Int(x, y);
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

    private bool IsInsideAllowedArea(Vector2 screenPos, Camera cam)
    {
        if (originContainerView == null) return false;
        var playerView = originContainerView.GetComponentInParent<PlayerInventoryView>();
        if (playerView != null)
        {
            var rect = playerView.transform as RectTransform;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
        }

        var sceneView = originContainerView.GetComponentInParent<SceneContainerView>();
        if (sceneView != null)
        {
            var rect = sceneView.transform as RectTransform;
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
        }

        return false;
    }
}
