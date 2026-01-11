using System.Collections.Generic;
using System.Numerics;
using QFramework;
using UnityEngine;

public class EquipmentContainer
{
    private readonly Dictionary<EquipmentSlotType, ItemInstance> _slots =
        new Dictionary<EquipmentSlotType, ItemInstance>();
    private readonly Dictionary<InventoryContainerType, InventoryContainer> _containers =
        new Dictionary<InventoryContainerType, InventoryContainer>();

    public IReadOnlyDictionary<EquipmentSlotType, ItemInstance> Slots => _slots;
    public IReadOnlyDictionary<InventoryContainerType, InventoryContainer> Containers => _containers;


    public EquipmentContainer()
    {
        _slots[EquipmentSlotType.Weapon1] = null;
        _slots[EquipmentSlotType.Weapon2] = null;
        _slots[EquipmentSlotType.Weapon3] = null;
        _slots[EquipmentSlotType.Weapon4] = null;
        _slots[EquipmentSlotType.Helmet] = null;
        _slots[EquipmentSlotType.Armor] = null;
        _slots[EquipmentSlotType.ChestRig] = null;
        _slots[EquipmentSlotType.Backpack] = null;

        InitEquipment();
    }


    private void InitEquipment()
    {
        //初始化口袋容器
        InventoryContainer container = new InventoryContainer(InventoryContainerType.Pocket);
        container.ContainerName = "PlayerPocket";
        for (int i = 0; i < 5; i++)
        {
            container.AddGrid(Vector2Int.one);
        }
        TryAddContainer(container);
    }



    public ItemInstance GetItem(EquipmentSlotType slot)
    {
        return _slots.TryGetValue(slot, out var item) ? item : null;
    }

    public bool TryAddContainer(InventoryContainer container)
    {
        if (container == null) return false;
        if (_containers.ContainsKey(container.Type)) return false;
        _containers[container.Type] = container;
        InventoryContainerModel model = GameArchitecture.Interface.GetModel<InventoryContainerModel>();
        if (!model.Containers.TryGetValue(container.InstanceId, out InventoryContainer container1))
        {
            model.Containers[container.InstanceId] = container;
        }
        return true;
    }

    public bool TryRemoveContainer(InventoryContainer container)
    {
        if (container == null) return false;
        if (!_containers.ContainsKey(container.Type)) return false;
        _containers.Remove(container.Type);
        InventoryContainerModel model = GameArchitecture.Interface.GetModel<InventoryContainerModel>();
        if (!model.Containers.TryGetValue(container.InstanceId, out InventoryContainer container1))
        {
            model.Containers.Remove(container.InstanceId);
        }
        return true;
    }




    public InventoryContainer GetContainer(InventoryContainerType type)
    {
        return _containers.TryGetValue(type, out var container) ? container : null;
    }

    public string GetContainerId(InventoryContainerType type)
    {
        return _containers.TryGetValue(type, out var container) ? container.InstanceId : null;
    }

    public string GetContainerName(InventoryContainerType type)
    {
        return _containers.TryGetValue(type, out var container) ? container.ContainerName : null;
    }

    public bool TryEquip(EquipmentSlotType slot, ItemInstance item, out ItemInstance replaced)
    {
        replaced = null;
        if (!_slots.ContainsKey(slot)) return false;
        replaced = _slots[slot];
        _slots[slot] = item;
        EquipItem(slot, item);
        return true;
    }

    public bool TryUnequip(EquipmentSlotType slot, out ItemInstance item)
    {
        if (!_slots.TryGetValue(slot, out item) || item == null) return false;
        _slots[slot] = null;
        UnequipItem(slot, item);
        return true;
    }


    private void EquipItem(EquipmentSlotType solt, ItemInstance item)
    {
        switch (solt)
        {
            case EquipmentSlotType.Weapon1:
            case EquipmentSlotType.Weapon2:
            case EquipmentSlotType.Weapon3:
            case EquipmentSlotType.Weapon4:
                break;
            case EquipmentSlotType.Helmet:
                break;
            case EquipmentSlotType.Armor:
                break;
            case EquipmentSlotType.ChestRig:
                InventoryContainer chest = GetContainer(InventoryContainerType.ChestRig);
                if (chest != null)
                {
                    TryRemoveContainer(chest);
                }
                chest = new InventoryContainer(InventoryContainerType.ChestRig);
                SOContainerItemDefinition config = item.Definition as SOContainerItemDefinition;
                chest.ContainerName = config.containerConfig.containerName;
                foreach (var part in config.containerConfig.partGridDatas)
                {
                    chest.AddGrid(part.Size);
                }
                TryAddContainer(chest);
                break;
            case EquipmentSlotType.Backpack:
                InventoryContainer backpack = GetContainer(InventoryContainerType.ChestRig);
                if (backpack != null)
                {
                    TryRemoveContainer(backpack);
                }
                backpack = new InventoryContainer(InventoryContainerType.ChestRig);
                SOContainerItemDefinition config1 = item.Definition as SOContainerItemDefinition;
                backpack.ContainerName = config1.containerConfig.containerName;
                foreach (var part in config1.containerConfig.partGridDatas)
                {
                    backpack.AddGrid(part.Size);
                }
                TryAddContainer(backpack);
                break;
            default:
                break;
        }

    }



    private void UnequipItem(EquipmentSlotType solt, ItemInstance item)
    {
        switch (solt)
        {
            case EquipmentSlotType.Weapon1:
            case EquipmentSlotType.Weapon2:
            case EquipmentSlotType.Weapon3:
            case EquipmentSlotType.Weapon4:
                break;
            case EquipmentSlotType.Helmet:
                break;
            case EquipmentSlotType.Armor:
                break;
            case EquipmentSlotType.ChestRig:
                InventoryContainer chest = GetContainer(InventoryContainerType.ChestRig);
                if (chest != null)
                {
                    TryRemoveContainer(chest);
                }
                break;
            case EquipmentSlotType.Backpack:
                InventoryContainer backpack = GetContainer(InventoryContainerType.ChestRig);
                if (backpack != null)
                {
                    TryRemoveContainer(backpack);
                }
                break;
            default:
                break;
        }

    }




}
