using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class SceneContainerView : MonoBehaviour, IController
{
    public RectTransform containerRoot;

    private ContainerView containerView;
    private string currentContainerId;
    private System.Func<string, int, Vector2Int, bool> onTryTake;
    private System.Func<string, int, Vector2Int, bool, bool> onTryPlace;

    public bool IsVisible => gameObject.activeSelf;

    public void InitSceneContainer()
    {
        if (!IsVisible) return;
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
        onTryTake = tryTake;
        onTryPlace = tryPlace;
        ApplyCallbacks();
    }

    public void RenderAll()
    {
        if (!IsVisible) return;
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
            ApplyCallbacks();
        }
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf == visible) return;
        gameObject.SetActive(visible);
    }

    private void ApplyCallbacks()
    {
        if (containerView != null && onTryTake != null && onTryPlace != null)
        {
            containerView.BindCallbacks(onTryTake, onTryPlace);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
