using System.Collections.Generic;

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
        return true;
    }

    public bool TryUnequip(EquipmentSlotType slot, out ItemInstance item)
    {
        if (!_slots.TryGetValue(slot, out item) || item == null) return false;
        _slots[slot] = null;
        return true;
    }
}
