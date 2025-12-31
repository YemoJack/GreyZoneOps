using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PlayerInventoryView : MonoBehaviour, IController
{
    public ContainerView chest;
    public ContainerView backpack;


    public void BindCallbacks(System.Func<string, int, Vector2Int, bool> tryTake,
        System.Func<string, int, Vector2Int, bool, bool> tryPlace)
    {
        if (chest != null)
        {
            chest.BindCallbacks(tryTake, tryPlace);
        }
        if (backpack != null)
        {
            backpack.BindCallbacks(tryTake, tryPlace);
        }

    }


    /// <summary>刷新容器内所有分格。</summary>
    public void RenderAll()
    {
        if (chest != null)
        {
            chest.RenderAll();
        }
        if (backpack != null)
        {
            backpack.RenderAll();
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
