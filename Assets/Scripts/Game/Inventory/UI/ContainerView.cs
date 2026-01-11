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

        foreach (var gv in gridViewList)
        {
            if (gv == null) continue;
            gv.OnTryTake = (part, pos) => onTryTake?.Invoke(container.InstanceId, gv.partIndex, pos) ?? false;
            gv.OnTryPlace = (part, pos, rotated) => onTryPlace?.Invoke(container.InstanceId, gv.partIndex, pos, rotated) ?? false;
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }


    /// <summary>刷新容器内所有分格。</summary>
    public void RenderAll(InventoryContainer container)
    {
        //container = this.GetSystem<InventorySystem>().GetContainer(containerId);

        foreach (var gv in gridViewList)
        {
            if (gv == null) continue;

            InventoryGrid grid = container.GetGrid(gv.partIndex);

            if (container != null && grid != null)
            {
                gv.Render(grid);
            }
        }
    }
}

