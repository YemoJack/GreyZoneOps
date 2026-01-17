public enum InventoryOpenSource
{
    BackpackButton,
    ContainerInteraction
}

public struct InventoryOpenContext
{
    public InventoryOpenSource Source;
    public string ContainerId;

    public bool ShowSceneContainer =>
        Source == InventoryOpenSource.ContainerInteraction && !string.IsNullOrEmpty(ContainerId);

    public static InventoryOpenContext FromBackpack()
    {
        return new InventoryOpenContext { Source = InventoryOpenSource.BackpackButton };
    }

    public static InventoryOpenContext FromContainer(string containerId)
    {
        return new InventoryOpenContext
        {
            Source = InventoryOpenSource.ContainerInteraction,
            ContainerId = containerId
        };
    }

    public InventoryOpenContext WithContainer(string containerId)
    {
        ContainerId = containerId;
        if (!string.IsNullOrEmpty(containerId))
        {
            Source = InventoryOpenSource.ContainerInteraction;
        }
        return this;
    }
}
