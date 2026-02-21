using System.Collections.Generic;
using QFramework;
using UnityEngine;

/// <summary>
/// 一个容器下的多分格视图集合（例如玩家背包含多个分格）。
/// </summary>
public class ContainerView : MonoBehaviour, IController
{
    public InventoryContainer container;

    [Tooltip("容器下的分格视图列表，partIndex 与配置中的 partIndex 对应。")]
    public List<InventoryGridView> gridViewList = new List<InventoryGridView>();

    private System.Func<string, int, Vector2Int, bool> onTryTake;
    private System.Func<string, int, Vector2Int, bool, bool> onTryPlace;

    /// <summary>绑定容器内所有分格的交互回调。</summary>
    public void BindCallbacks(
        System.Func<string, int, Vector2Int, bool> tryTake,
        System.Func<string, int, Vector2Int, bool, bool> tryPlace)
    {
        onTryTake = tryTake;
        onTryPlace = tryPlace;

        if (gridViewList == null)
        {
            return;
        }

        foreach (var gv in gridViewList)
        {
            if (gv == null) continue;
            int partIndex = gv.partIndex;

            gv.OnTryTake = (part, pos) =>
            {
                if (container == null || string.IsNullOrEmpty(container.InstanceId))
                {
                    return false;
                }

                return onTryTake?.Invoke(container.InstanceId, partIndex, pos) ?? false;
            };

            gv.OnTryPlace = (part, pos, rotated) =>
            {
                if (container == null || string.IsNullOrEmpty(container.InstanceId))
                {
                    return false;
                }

                return onTryPlace?.Invoke(container.InstanceId, partIndex, pos, rotated) ?? false;
            };
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }

    /// <summary>刷新容器内所有分格。</summary>
    public void RenderAll(InventoryContainer runtimeContainer)
    {
        var currentContainer = runtimeContainer ?? container;
        if (currentContainer == null || gridViewList == null)
        {
            return;
        }

        container = currentContainer;

        foreach (var gv in gridViewList)
        {
            if (gv == null) continue;

            InventoryGrid grid = currentContainer.GetGrid(gv.partIndex);
            if (grid != null)
            {
                gv.Render(grid);
            }
        }
    }
}
