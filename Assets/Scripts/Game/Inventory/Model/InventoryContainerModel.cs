using System.Collections.Generic;
using QFramework;
using UnityEngine;

public enum InventoryContainerType
{
    Pocket,
    Backpack,
    ChestRig,
    Stash,
    LootBox
}

public class InventoryContainerModel : AbstractModel
{
    public Dictionary<string, InventoryContainer> Containers;
    public EquipmentContainer PlayerEquipment;


    protected override void OnInit()
    {
        Containers = new Dictionary<string, InventoryContainer>();
        PlayerEquipment = new EquipmentContainer();

        LoadContainerConfig(0);
    }

    public string GetPlayerContainerId(InventoryContainerType type)
    {
        return PlayerEquipment.GetContainerId(type);
    }

    public string GetPlayerContainerName(InventoryContainerType type)
    {
        return PlayerEquipment.GetContainerName(type);
    }

    public EquipmentContainer GetPlayerEquipment()
    {
        return PlayerEquipment;
    }

    public void LoadContainerConfig(int mapId)
    {
        SOInventoryContainerConfig config = this.GetUtility<IResLoader>().LoadSync<SOInventoryContainerConfig>($"ContainerConfig_{mapId}");
        if (config != null)
        {
            foreach (var container in config.containerConfigs)
            {
                InventoryContainer inventoryContainer = CreateInventoryContainer(container);
                Containers[inventoryContainer.InstanceId] = inventoryContainer;
            }
        }
    }

    public InventoryContainer GetFirstContainerByType(InventoryContainerType type)
    {
        foreach (var container in Containers.Values)
        {
            if (container.Type == type)
            {
                return container;
            }
        }

        return null;
    }

    private InventoryContainer CreateInventoryContainer(SOContainerConfig config)
    {
        InventoryContainer container = new InventoryContainer(config.containerType);
        container.InstanceId = config.containerId.ToString();
        container.ContainerName = config.containerName;
        foreach (var part in config.partGridDatas)
        {
            container.AddGrid(part.Size);
        }

        return container;
    }



}

