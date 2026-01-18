using System.Collections.Generic;
using UnityEngine;
using QFramework;


public struct EventGameInit
{

}


public class PlayerController : MonoBehaviour, IController
{
    public Transform WeaponRoot;
    private WeaponSystem weaponSystem;
    private InventoryContainerModel inventoryModel;
    private string weaponSignature;

    private void Start()
    {
        this.RegisterEvent<EventGameInit>(OnInit).UnRegisterWhenGameObjectDestroyed(this);
        this.RegisterEvent<InventoryChangedEvent>(OnInventoryChanged).UnRegisterWhenGameObjectDestroyed(this);
    }

    private void OnInit(EventGameInit e)
    {

        weaponSystem = this.GetSystem<WeaponSystem>();
        inventoryModel = this.GetModel<InventoryContainerModel>();
        RefreshWeaponLoadout(force: true);
    }

    private void OnInventoryChanged(InventoryChangedEvent e)
    {
        RefreshWeaponLoadout(force: false);
    }

    private void RefreshWeaponLoadout(bool force)
    {
        if (weaponSystem == null || inventoryModel == null || WeaponRoot == null)
        {
            return;
        }

        var equipment = inventoryModel.GetPlayerEquipment();
        if (equipment == null)
        {
            return;
        }

        var newSignature = BuildWeaponSignature(equipment);
        if (!force && newSignature == weaponSignature)
        {
            return;
        }

        weaponSignature = newSignature;
        weaponSystem.InitializeLoadout(WeaponRoot, equipment);
    }

    private string BuildWeaponSignature(EquipmentContainer equipment)
    {
        var w1 = equipment.GetItem(EquipmentSlotType.Weapon1)?.InstanceId ?? "null";
        var w2 = equipment.GetItem(EquipmentSlotType.Weapon2)?.InstanceId ?? "null";
        var w3 = equipment.GetItem(EquipmentSlotType.Weapon3)?.InstanceId ?? "null";
        var w4 = equipment.GetItem(EquipmentSlotType.Weapon4)?.InstanceId ?? "null";
        return $"{w1}|{w2}|{w3}|{w4}";
    }








    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
