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
    public Dictionary<InventoryContainerType, InventoryContainer> PlayerContainers;


    protected override void OnInit()
    {
        Containers = new Dictionary<string, InventoryContainer>();
        PlayerContainers = new Dictionary<InventoryContainerType, InventoryContainer>();

        LoadPlayerContainerConfig();
        LoadContainerConfig(0);
    }

    public void LoadPlayerContainerConfig()
    {
        SOPlayerInventoryConfig config =
            this.GetUtility<IResLoader>().LoadSync<SOPlayerInventoryConfig>("PlayerContainerConfig");
        if (config == null) return;

        foreach (var container in config.containerConfigs)
        {
            InventoryContainer inventoryContainer = CreateInventoryContainer(container);
            Containers[inventoryContainer.InstanceId] = inventoryContainer;
            if (PlayerContainers.ContainsKey(inventoryContainer.Type))
            {
                Debug.LogError($"InventoryContainerModel LoadPlayerContainerConfig 玩家背包数据已经存在相同的类型 {inventoryContainer.Type} {inventoryContainer.ContainerName}");
                continue;
            }
            PlayerContainers[inventoryContainer.Type] = inventoryContainer;
        }
    }

    public string GetPlayerContainerId(InventoryContainerType type)
    {
        return PlayerContainers.TryGetValue(type, out var container) ? container.InstanceId : null;
    }

    public string GetPlayerContainerName(InventoryContainerType type)
    {

        if (PlayerContainers.TryGetValue(type, out InventoryContainer container))
        {
            return container.ContainerName;
        }

        return null;
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

    private InventoryContainer CreateInventoryContainer(ContainerConfig config)
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
