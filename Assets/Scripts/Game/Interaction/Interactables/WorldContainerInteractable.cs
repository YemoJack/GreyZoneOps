using QFramework;
using UnityEngine;

public class WorldContainerInteractable : MonoBehaviour, IInteractable, IController, ICanSendEvent
{
    public string ContainerId;
    public InventoryContainerType FallbackType = InventoryContainerType.LootBox;
    public string PromptOverride;

    public bool CanInteract(InteractContext ctx)
    {
        return ResolveContainer() != null;
    }

    public InteractInfo GetInfo(InteractContext ctx)
    {
        var container = ResolveContainer();
        var name = container != null ? container.ContainerName : "Container";
        var prompt = string.IsNullOrEmpty(PromptOverride) ? $"Open {name}" : PromptOverride;
        return new InteractInfo
        {
            Prompt = prompt,
            CanInteract = container != null,
            Icon = null
        };
    }

    public void Interact(InteractContext ctx)
    {
        var container = ResolveContainer();
        if (container == null) return;
        this.SendEvent(new EventOpenContainer { ContainerId = container.InstanceId });
    }

    private InventoryContainer ResolveContainer()
    {
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null) return null;

        if (!string.IsNullOrEmpty(ContainerId))
        {
            return model.GetContainer(ContainerId);
        }

        return model.GetFirstContainerByType(FallbackType);
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
