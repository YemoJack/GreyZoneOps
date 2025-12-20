using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image icon;
    public Text countText;
    public CanvasGroup canvasGroup;

    private RectTransform rect;
    private RectTransform gridRect;
    private RectTransform itemRoot;
    private GridLayoutGroup layout;
    private int gridWidth;
    private int gridHeight;

    private ItemPlacement placement;
    private System.Func<bool> onBegin;
    private System.Func<Vector2Int, bool, bool> onDrop;
    private bool rotated => placement.Rotated;
    private bool dragging;
    private Vector2Int cellSpan = Vector2Int.one;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetupGrid(
        GridLayoutGroup gridLayout,
        RectTransform gridSpace,
        RectTransform itemParent,
        int width,
        int height)
    {
        layout = gridLayout;
        gridRect = gridSpace;
        itemRoot = itemParent;
        if (itemRoot != null && rect != null && rect.parent != itemRoot)
        {
            rect.SetParent(itemRoot, worldPositionStays: false);
        }
        gridWidth = width;
        gridHeight = height;
    }

    public void Bind(ItemPlacement p)
    {
        placement = p;
        countText.text = p.Item.Count > 1 ? p.Item.Count.ToString() : "";
        rect.anchoredPosition = CellToLocal(p.Pos);
        SetSize(p.Size, rotated);
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

        dragging = true;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, e.position, e.pressEventCamera, out var localPoint))
        {
            rect.anchoredPosition = localPoint;
        }
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

        var gridPos = ScreenToCell(e.position, e.pressEventCamera);
        onDrop?.Invoke(gridPos, rotated);
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

    private Vector2Int ScreenToCell(Vector2 screenPos, Camera cam)
    {
        if (layout == null || gridRect == null) return new Vector2Int(-1, -1);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPos, cam, out var localPoint))
            return new Vector2Int(-1, -1);

        var padding = layout.padding;
        var cell = layout.cellSize;
        var spacing = layout.spacing;

        float localX = localPoint.x - padding.left;
        float localY = -(localPoint.y) - padding.top;

        if (localX < 0 || localY < 0) return new Vector2Int(-1, -1);

        int x = Mathf.FloorToInt(localX / (cell.x + spacing.x));
        int y = Mathf.FloorToInt(localY / (cell.y + spacing.y));

        if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
            return new Vector2Int(-1, -1);

        return new Vector2Int(x, y);
    }
}
