using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class EquipmentView : MonoBehaviour, IController
{
    public List<EquipmentSlotView> slotViews = new List<EquipmentSlotView>();

    private EquipmentContainer equipment;

    public void InitEquipment()
    {
        var system = this.GetSystem<InventorySystem>();
        if (system == null) return;
        equipment = system.GetPlayerEquipment();
    }

    public void BindCallbacks(
        System.Func<EquipmentSlotType, bool> tryTake,
        System.Func<EquipmentSlotType, bool> tryPlace,
        System.Func<EquipmentSlotType, EquipmentSlotType, bool> trySwap,
        System.Func<EquipmentSlotType, ItemInstance> beginDrag,
        System.Func<EquipmentSlotType, ItemInstance, bool> returnDrag)
    {
        foreach (var slot in slotViews)
        {
            if (slot == null) continue;
            slot.BindCallbacks(tryTake, tryPlace, trySwap, beginDrag, returnDrag);
        }
    }

    public void RenderAll()
    {
        if (equipment == null) return;
        foreach (var slot in slotViews)
        {
            if (slot == null) continue;
            slot.Render(equipment.GetItem(slot.SlotType));
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
