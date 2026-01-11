using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class SceneContainerView : MonoBehaviour, IController
{
    public RectTransform containerRoot;

    private ContainerView containerView;

    public void InitSceneContainer()
    {
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return;

        var container = model.GetFirstContainerByType(InventoryContainerType.LootBox);
        if (container == null) return;

        if (containerView == null)
        {
            containerView = CreateContainerView(container.ContainerName, containerRoot);
        }

        if (containerView != null)
        {
            containerView.container = container;
        }
    }

    private ContainerView CreateContainerView(string containerName, RectTransform root)
    {
        if (string.IsNullOrEmpty(containerName) || root == null) return null;
        var prefab = this.GetUtility<IResLoader>().LoadSync<GameObject>(containerName);
        if (prefab == null) return null;

        var instance = Instantiate(prefab, root);
        return instance.GetComponent<ContainerView>();
    }

    public void BindCallbacks(System.Func<string, int, Vector2Int, bool> tryTake,
        System.Func<string, int, Vector2Int, bool, bool> tryPlace)
    {
        if (containerView != null)
        {
            containerView.BindCallbacks(tryTake, tryPlace);
        }
    }

    public void RenderAll()
    {
        if (containerView != null)
        {
            containerView.RenderAll(containerView.container);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
