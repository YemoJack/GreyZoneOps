using QFramework;
using UnityEngine;

public class CmdPickupItem : AbstractCommand
{
    private readonly ItemInstance _item;
    private readonly GameObject _source;

    public CmdPickupItem(ItemInstance item, GameObject source)
    {
        _item = item;
        _source = source;
    }

    protected override void OnExecute()
    {
        if (_item == null || _item.Definition == null || _item.Count <= 0) return;

        var system = this.GetSystem<InventorySystem>();
        if (system == null) return;

        var placed = TryEquipIfEmpty(system, _item) || system.TryAutoPlaceToPlayerContainers(_item);
        if (placed && _source != null)
        {
            Object.Destroy(_source);
        }
        else
        {
            Debug.LogWarning($"Pick up {_item.Definition.Name} Fail.");
        }
    }

    private bool TryEquipIfEmpty(InventorySystem system, ItemInstance item)
    {
        if (system == null || item == null || item.Definition == null) return false;
        var equipment = system.GetPlayerEquipment();
        if (equipment == null) return false;

        switch (item.Definition.Category)
        {
            case ItemCategory.Weapon:
                return TryEquipWeapon(system, equipment, item);
            case ItemCategory.helmet:
                return TryEquipSlotIfEmpty(system, equipment, EquipmentSlotType.Helmet, item);
            case ItemCategory.Armor:
                return TryEquipSlotIfEmpty(system, equipment, EquipmentSlotType.Armor, item);
            case ItemCategory.ChestRig:
                return TryEquipSlotIfEmpty(system, equipment, EquipmentSlotType.ChestRig, item);
            case ItemCategory.Backpack:
                return TryEquipSlotIfEmpty(system, equipment, EquipmentSlotType.Backpack, item);
            default:
                return false;
        }
    }

    private bool TryEquipWeapon(InventorySystem system, EquipmentContainer equipment, ItemInstance item)
    {
        if (equipment.GetItem(EquipmentSlotType.Weapon1) == null)
        {
            return system.TryEquipItem(EquipmentSlotType.Weapon1, item, out _);
        }
        if (equipment.GetItem(EquipmentSlotType.Weapon2) == null)
        {
            return system.TryEquipItem(EquipmentSlotType.Weapon2, item, out _);
        }
        if (equipment.GetItem(EquipmentSlotType.Weapon3) == null)
        {
            return system.TryEquipItem(EquipmentSlotType.Weapon3, item, out _);
        }
        if (equipment.GetItem(EquipmentSlotType.Weapon4) == null)
        {
            return system.TryEquipItem(EquipmentSlotType.Weapon4, item, out _);
        }
        return false;
    }

    private bool TryEquipSlotIfEmpty(
        InventorySystem system,
        EquipmentContainer equipment,
        EquipmentSlotType slot,
        ItemInstance item)
    {
        if (equipment.GetItem(slot) != null) return false;
        return system.TryEquipItem(slot, item, out _);
    }
}
