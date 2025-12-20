using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryGridView : MonoBehaviour
{
    [Header("Grid")]
    public RectTransform cellRoot;          // 格子父节点（挂 GridLayoutGroup）
    public RectTransform itemRoot;          // 物品父节点（与格子对齐）
    public GridLayoutGroup layout;          // 设定 cell 大小/间距
    public InventoryContainerType containerType;
    public GameObject cellPrefab;
    public GameObject itemPrefab;

    private readonly List<InventoryCellView> cells = new();
    private readonly List<InventoryItemView> items = new();
    private InventoryGrid gridData;

    // 外部注入的交互委托
    public System.Func<Vector2Int, bool, bool> OnTryPlace; // pos, rotated -> success
    public System.Func<Vector2Int, bool> OnTryTake;        // pos -> success
    public System.Action<Vector2Int> OnHover;              // 可用于高亮

    public void Render(InventoryContainerType type, InventoryGrid grid)
    {
        containerType = type;
        gridData = grid;
        BuildCells(grid.Width, grid.Height);
        RenderItems(grid);
    }

    private void BuildCells(int w, int h)
    {
        // 确保 GridLayoutGroup 以固定列数方式排布，避免一行塞满后才换行
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Max(1, w);
        }

        var need = w * h;
        EnsureCellPool(need);
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (i < need)
            {
                cell.gameObject.SetActive(true);
                var x = i % w;
                var y = i / w;
                cell.SetPos(new Vector2Int(x, y));
                cell.SetHoverCallback(OnHover);
                cell.SetClickCallback(OnCellClick);
            }
            else cell.gameObject.SetActive(false);
        }
    }

    private void RenderItems(InventoryGrid grid)
    {
        ClearItems();
        foreach (var placement in grid.GetAllPlacements())
        {
            var view = GetItemView();
            view.SetupGrid(layout, cellRoot, itemRoot, grid.Width, grid.Height);
            view.Bind(placement);
            view.SetDragCallbacks(
                onBegin: () => OnTryTake?.Invoke(placement.Pos) ?? false,
                onDrop: (pos, rotated) => OnTryPlace?.Invoke(pos, rotated) ?? false
            );
        }
    }

    private void OnCellClick(Vector2Int pos)
    {
        OnTryTake?.Invoke(pos);
        Debug.Log($"Click Cell pos :{pos}");
    }

    private void EnsureCellPool(int need)
    {
        while (cells.Count < need)
        {
            var parent = layout != null ? layout.transform : cellRoot;
            var cellObj = Instantiate(cellPrefab, parent);
            var cell = cellObj.GetComponent<InventoryCellView>();
            cells.Add(cell);
        }
    }

    private InventoryItemView GetItemView()
    {
        var parent = itemRoot != null ? itemRoot : cellRoot;
        var itemObj = Instantiate(itemPrefab, parent);
        var item = itemObj.GetComponent<InventoryItemView>();
        items.Add(item);
        return item;
    }

    private void ClearItems()
    {
        foreach (var v in items) Destroy(v.gameObject);
        items.Clear();
    }
}
