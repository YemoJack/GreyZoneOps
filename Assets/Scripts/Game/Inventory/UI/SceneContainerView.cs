using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class SceneContainerView : MonoBehaviour, IController
{
    public RectTransform containerRoot;

    private ContainerView containerView;
    private string currentContainerId;

    public void InitSceneContainer()
    {
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return;

        InventoryContainer container = null;
        if (!string.IsNullOrEmpty(currentContainerId))
        {
            container = model.GetContainer(currentContainerId);
        }
        if (container == null)
        {
            container = model.GetFirstContainerByType(InventoryContainerType.LootBox);
        }

        SetContainer(container);
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

    public void SetContainerById(string containerId)
    {
        if (string.IsNullOrEmpty(containerId)) return;
        if (currentContainerId == containerId) return;
        currentContainerId = containerId;

        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return;
        var container = model.GetContainer(containerId);
        SetContainer(container);
    }

    private void SetContainer(InventoryContainer container)
    {
        if (container == null || containerRoot == null)
        {
            return;
        }

        if (containerView == null || containerView.container == null ||
            containerView.container.ContainerName != container.ContainerName)
        {
            if (containerView != null)
            {
                Destroy(containerView.gameObject);
            }
            containerView = CreateContainerView(container.ContainerName, containerRoot);
        }

        if (containerView != null)
        {
            containerView.container = container;
            currentContainerId = container.InstanceId;
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
