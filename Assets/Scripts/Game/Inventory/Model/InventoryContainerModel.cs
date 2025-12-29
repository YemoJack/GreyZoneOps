using System.Collections.Generic;
using QFramework;
using UnityEngine;

public enum InventoryContainerType
{
    Backpack,
    ChestRig,
    Stash,
    LootBox
}

public class InventoryContainerModel : AbstractModel
{
    public Dictionary<string, InventoryContainer> Containers;

    public ItemInstance HoldingItem;

    protected override void OnInit()
    {
        Containers = new Dictionary<string, InventoryContainer>();

        LoadContainerConfig();
    }


    public void LoadContainerConfig()
    {
        SOInventoryContainerConfig config = this.GetUtility<IResLoader>().LoadSync<SOInventoryContainerConfig>("SOInventoryContainerConfig");
        if (config != null)
        {
            foreach (var container in config.containerConfigs)
            {
                InventoryContainer inventoryContainer = CreateInventoryContainer(container);
                Containers[inventoryContainer.InstanceId] = inventoryContainer;
            }
        }
    }


    private InventoryContainer CreateInventoryContainer(ContainerConfig config)
    {
        InventoryContainer container = new InventoryContainer(config.containerType);
        container.InstanceId = config.containerId.ToString();
        foreach (var part in config.partGridDatas)
        {
            container.AddGrid(part.Size);
        }

        return container;
    }



}
