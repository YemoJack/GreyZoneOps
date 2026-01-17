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
    public SOInventoryContainerConfig CurrentMapConfig { get; private set; }


    protected override void OnInit()
    {
        Containers = new Dictionary<string, InventoryContainer>();
        PlayerEquipment = new EquipmentContainer();

        LoadMapConfig(0);
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

    public void LoadMapConfig(int mapId)
    {
        CurrentMapConfig = this.GetUtility<IResLoader>().LoadSync<SOInventoryContainerConfig>($"Cfg_MapConfig_{mapId}");
    }

    public InventoryContainer EnsureContainer(SOContainerConfig config, string overrideInstanceId = null)
    {
        if (config == null) return null;
        var id = !string.IsNullOrEmpty(overrideInstanceId)
            ? overrideInstanceId
            : config.containerId.ToString();
        if (Containers.TryGetValue(id, out var existing))
        {
            return existing;
        }

        var container = CreateInventoryContainer(config, overrideInstanceId);
        Containers[container.InstanceId] = container;
        return container;
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

    public InventoryContainer GetContainer(string id)
    {
        return Containers.TryGetValue(id, out var container) ? container : null;
    }

    private InventoryContainer CreateInventoryContainer(SOContainerConfig config, string overrideInstanceId = null)
    {
        InventoryContainer container = new InventoryContainer(config.containerType);
        container.InstanceId = !string.IsNullOrEmpty(overrideInstanceId)
            ? overrideInstanceId
            : config.containerId.ToString();
        container.ContainerName = config.containerName;
        foreach (var part in config.partGridDatas)
        {
            container.AddGrid(part.Size);
        }

        return container;
    }



}
