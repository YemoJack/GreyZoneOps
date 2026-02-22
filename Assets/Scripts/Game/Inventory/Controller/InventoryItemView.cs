using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private static string selectedItemInstanceId;
    private static SelectItemWindow activeSelectWindow;
    private static readonly Dictionary<string, RectTransform> itemRectLookup = new Dictionary<string, RectTransform>();

    public Image bgIcon;
    public Image icon;
    public Text countText;
    private CanvasGroup canvasGroup;
    private float iconBaseZ;
    private bool iconBaseRotationCaptured;

    private RectTransform rect;
    private RectTransform gridRect;
    private RectTransform itemRoot;
    private GridLayoutGroup layout;

    private InventoryGridView currentTargetGrid;

    private ItemPlacement placement;
    private System.Func<bool> onBegin;
    private System.Func<Vector2Int, bool, bool> onDrop;
    private bool rotated => currentItem != null && currentItem.Rotated;
    private bool dragging;
    private Vector2Int cellSpan = Vector2Int.one;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private Vector2 pointerOffset = Vector2.zero;
    private ContainerView originContainerView;
    private ItemInstance currentItem;
    private InventoryGridView highlightedGrid;
    private float draggingAlpha = 0.8f;

    private void Awake()
    {
        rect = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        CacheIconBaseRotation();
        ApplyGameConfig();
    }

    private void ApplyGameConfig()
    {
        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            return;
        }

        draggingAlpha = settings.Config.DraggingItemAlpha;
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
        currentItem = p.Item;
        countText.text = p.Item.Count > 1 ? p.Item.Count.ToString() : "";
        rect.anchoredPosition = CellToLocal(p.Pos);
        // Use definition size + rotated state to avoid applying rotation twice.
        SetSize(p.Item.Definition.Size, rotated);

        icon.sprite = p.Item.Definition.Icon;
        if (layout != null)
        {
            SetIconSizeToUnrotatedSize(p.Item.Definition.Size, layout.cellSize, layout.spacing);
        }
        ApplyIconRotation(rotated);
        RegisterItemRect();
        ApplyQualityColor(p.Item);
    }

    public void BindDrag(ItemInstance item, Vector2 cellSize, Vector2 spacing)
    {
        if (item == null || item.Definition == null) return;
        placement = null;
        currentItem = item;
        countText.text = item.Count > 1 ? item.Count.ToString() : "";
        var size = item.Definition.Size;
        var w = size.x;
        var h = size.y;
        var width = cellSize.x * w + spacing.x * Mathf.Max(0, w - 1);
        var height = cellSize.y * h + spacing.y * Mathf.Max(0, h - 1);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        cellSpan = new Vector2Int(item.Rotated ? h : w, item.Rotated ? w : h);
        if (icon != null) icon.enabled = true;

        icon.sprite = item.Definition.Icon;
        SetIconSizeToUnrotatedSize(item.Definition.Size, cellSize, spacing);
        ApplyIconRotation(item.Rotated);
        ApplyQualityColor(item);
    }

    private void ApplyQualityColor(ItemInstance item)
    {
        if (bgIcon == null || item == null || item.Definition == null)
        {
            return;
        }

        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            return;
        }

        var quality = item.Definition.Quality;
        var colors = settings.Config.ItemQualityColors;
        var color = Color.white;
        if (colors != null)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i].Quality == quality)
                {
                    color = colors[i].Color;
                    break;
                }
            }
        }

        bgIcon.color = color;
    }

    public void SetDragCallbacks(System.Func<bool> onBegin, System.Func<Vector2Int, bool, bool> onDrop)
    {
        this.onBegin = onBegin;
        this.onDrop = onDrop;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (dragging || eventData.dragging)
        {
            return;
        }

        if (placement == null || currentItem == null || currentItem.Definition == null)
        {
            return;
        }

        var clickedInstanceId = currentItem.InstanceId;
        var clickedSameItem = !string.IsNullOrEmpty(clickedInstanceId) &&
                              clickedInstanceId == selectedItemInstanceId &&
                              activeSelectWindow != null &&
                              activeSelectWindow.Visible;

        if (clickedSameItem)
        {
            CloseSelectionWindow();
            return;
        }

        var window = UIModule.Instance.PopUpWindow<SelectItemWindow>();
        if (window == null)
        {
            return;
        }

        activeSelectWindow = window;
        selectedItemInstanceId = clickedInstanceId;
        window.ShowForItem(currentItem, rect);
    }

    public static void NotifySelectionWindowHidden(SelectItemWindow window)
    {
        if (window == null)
        {
            return;
        }

        if (activeSelectWindow == window)
        {
            activeSelectWindow = null;
            selectedItemInstanceId = null;
        }
    }

    public static bool TryGetItemRect(string itemInstanceId, out RectTransform itemRect)
    {
        itemRect = null;
        if (string.IsNullOrEmpty(itemInstanceId))
        {
            return false;
        }

        if (!itemRectLookup.TryGetValue(itemInstanceId, out var rect) || rect == null)
        {
            itemRectLookup.Remove(itemInstanceId);
            return false;
        }

        itemRect = rect;
        return true;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (IsCurrentItemSelected())
        {
            CloseSelectionWindow();
        }

        if (onBegin != null && !onBegin.Invoke())
            return;

        ClearHoverHighlight();
        originContainerView = GetComponentInParent<ContainerView>();
        originalParent = rect.parent;
        originalAnchoredPos = rect.anchoredPosition;
        currentTargetGrid = null;
        rootCanvas = GetComponentInParent<Canvas>();
        canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
        canvasCamera = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        // 鎶婃嫋鎷界墿鎻愬崌鍒版渶椤跺眰锛岄伩鍏嶈鍏朵粬瀹瑰櫒閬尅
        if (rootCanvas != null)
        {
            rect.SetParent(rootCanvas.transform, worldPositionStays: true);
            rect.SetAsLastSibling();
        }

        dragging = true;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
        }
        Debug.Log($"Item OnBeginDrag id: {placement.Item.Definition.Id} Name: {placement.Item.Definition.Name} startPos{placement.Pos}\n InstanceId {placement.Item.InstanceId}  ");

        // 鍒濆鏃跺氨璁╃墿鍝佷腑蹇冨榻愭寚閽?
        pointerOffset = CalculatePointerOffset(e);
        UpdatePositionToPointer(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        UpdatePositionToPointer(e);
        UpdateHoverHighlight(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        ClearHoverHighlight();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        var gridPos = ScreenToCell(e.position, e.pressEventCamera, out var targetGrid);
        if (targetGrid == null)
        {
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
        }

        var isOutside = targetGrid == null;
        if (isOutside)
        {
            // 鐗规畩鏍囪锛氬畬鍏ㄦ嫋鍑轰换浣曠綉鏍硷紝浜ょ敱涓婂眰鍒ゅ畾涓轰涪寮?
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
            // targetGrid 涓虹┖瑙嗕负涓㈠純锛屼紶閫?(-1,-1) 璁╀笂灞傚喅瀹氬浣曞鐞?
            placed = onDrop.Invoke(gridPos, rotated);
        }

        Debug.Log($"Item OnEndDrag  id: {placement.Item.Definition.Id} Name: {placement.Item.Definition.Name} endPos {gridPos} placed:{placed} target:{target?.name}\n InstanceId {placement.Item.InstanceId} ");


        // 鎭㈠鐖惰妭鐐癸紝鍏蜂綋浣嶇疆浼氬湪鍚庣画鍒锋柊鏃剁敱缁戝畾鏇存柊
        RestoreParent();
    }

    private void UpdateHoverHighlight(PointerEventData e)
    {
        var gridPos = ScreenToCell(e.position, e.pressEventCamera, out var targetGrid);
        if (targetGrid != highlightedGrid)
        {
            highlightedGrid?.ClearPlacementPreview();
            highlightedGrid = targetGrid;
        }

        if (targetGrid == null)
        {
            return;
        }

        targetGrid.ShowPlacementPreview(currentItem, gridPos, rotated);
    }

    private void ClearHoverHighlight()
    {
        if (highlightedGrid != null)
        {
            highlightedGrid.ClearPlacementPreview();
            highlightedGrid = null;
        }
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

    /// <summary>璁＄畻褰撳墠榧犳爣浣嶇疆涓庣墿鍝佷腑蹇冪殑鍋忕Щ锛屼繚璇佹嫋鎷芥椂涓績瀵归綈銆?/summary>
    private Vector2 CalculatePointerOffset(PointerEventData e)
    {
        // 鐩存帴鐢ㄥ昂瀵镐笌 pivot 璁＄畻涓績鐩稿宸︿笂瑙掔殑鍋忕Щ
        var size = rect.rect.size;
        var pivot = rect.pivot;
        // 褰?pivot 涓?0.5,0.5)鏃跺亸绉讳负0锛屽惁鍒欐牴鎹?pivot 璁＄畻涓績鍋忕Щ
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

        // 椤堕儴宸︿晶涓?0,0)锛岃绠楃墿鍝佸崰鐢ㄥ尯鍩熺殑涓績浣嶇疆
        float width = cell.x * cellSpan.x + spacing.x * Mathf.Max(0, cellSpan.x - 1);
        float height = cell.y * cellSpan.y + spacing.y * Mathf.Max(0, cellSpan.y - 1);

        // 鑰冭檻鐖惰妭鐐?pivot/anchor锛屽師鐐瑰湪鐖惰妭鐐?pivot 澶?
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
        // 浼樺厛鍛戒腑鎸囬拡涓嬬殑缃戞牸锛涙湭鍛戒腑鍒欒涓烘嫋鍑虹綉鏍硷紙涓㈠純锛?
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

        // 灏嗕互 pivot 涓哄師鐐圭殑鍧愭爣锛岃浆鎹负浠ュ乏涓婅涓哄師鐐圭殑鍧愭爣绯?
        float originX = localPoint.x + size.x * pivot.x;
        float originY = -localPoint.y + size.y * (1f - pivot.y);

        // 鎸囬拡鍦ㄧ墿鍝佷腑蹇冿紝灏嗕腑蹇冩崲绠楁垚宸︿笂瑙掕捣鐐?
        float width = cell.x * cellSpan.x + spacing.x * Mathf.Max(0, cellSpan.x - 1);
        float height = cell.y * cellSpan.y + spacing.y * Mathf.Max(0, cellSpan.y - 1);

        float localX = originX - padding.left - width * 0.5f;
        float localY = originY - padding.top - height * 0.5f;

        float stepX = cell.x + spacing.x;
        float stepY = cell.y + spacing.y;

        // 灏嗘寚閽堜綅缃槧灏勫埌璺濈宸︿笂瑙掓渶杩戠殑鏍煎瓙锛堝洓鑸嶄簲鍏ヨ€岄潪鍚戜笅鍙栨暣锛?
        int x = Mathf.RoundToInt(localX / stepX);
        int y = Mathf.RoundToInt(localY / stepY);

        // 纭繚鍓╀綑绌洪棿瓒冲鏀句笅璇ョ墿鍝?
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

    private void OnDestroy()
    {
        UnregisterItemRect();
    }

    private bool IsCurrentItemSelected()
    {
        return currentItem != null &&
               !string.IsNullOrEmpty(currentItem.InstanceId) &&
               currentItem.InstanceId == selectedItemInstanceId;
    }

    private static void CloseSelectionWindow()
    {
        var window = activeSelectWindow;
        activeSelectWindow = null;
        selectedItemInstanceId = null;
        window?.HideWindow();
    }

    private void CacheIconBaseRotation()
    {
        if (icon == null)
        {
            return;
        }

        iconBaseZ = icon.rectTransform.localEulerAngles.z;
        iconBaseRotationCaptured = true;
    }

    private void ApplyIconRotation(bool isRotated)
    {
        if (icon == null)
        {
            return;
        }

        if (!iconBaseRotationCaptured)
        {
            CacheIconBaseRotation();
        }

        var targetZ = iconBaseZ + (isRotated ? -90f : 0f);
        icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);
    }

    private void SetIconSizeToUnrotatedSize(Vector2Int itemSize, Vector2 cellSize, Vector2 spacing)
    {
        if (icon == null)
        {
            return;
        }

        var w = Mathf.Max(1, itemSize.x);
        var h = Mathf.Max(1, itemSize.y);
        var width = cellSize.x * w + spacing.x * Mathf.Max(0, w - 1);
        var height = cellSize.y * h + spacing.y * Mathf.Max(0, h - 1);
        icon.rectTransform.sizeDelta = new Vector2(width, height);
    }

    private void RegisterItemRect()
    {
        if (rect == null || currentItem == null || string.IsNullOrEmpty(currentItem.InstanceId))
        {
            return;
        }

        itemRectLookup[currentItem.InstanceId] = rect;
    }

    private void UnregisterItemRect()
    {
        if (currentItem == null || string.IsNullOrEmpty(currentItem.InstanceId))
        {
            return;
        }

        if (itemRectLookup.TryGetValue(currentItem.InstanceId, out var mappedRect) && mappedRect == rect)
        {
            itemRectLookup.Remove(currentItem.InstanceId);
        }
    }
}
