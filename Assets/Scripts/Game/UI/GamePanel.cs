using UnityEngine;
using QFramework;
using UnityEngine.UI;

public class GamePanel : MonoBehaviour, IController
{
    public Text WeaponNameText;
    public Text AmmoNumText;

    private WeaponSystem weaponSystem;
    private WeaponInventoryModel weaponInventoryModel;

    private IUnRegister weaponChangeUnregister;
    private IUnRegister ammoChangeUnregister;

    private void Awake()
    {
        weaponSystem = this.GetSystem<WeaponSystem>();
        weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
    }

    private void OnEnable()
    {
        RegisterEvents();
        RefreshCurrentWeaponUI();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void RegisterEvents()
    {
        weaponChangeUnregister = this.RegisterEvent<EventPlayerChangeWeapon>(OnWeaponChanged);
        ammoChangeUnregister = this.RegisterEvent<EventWeaponAmmoChanged>(OnAmmoChanged);
    }

    private void UnregisterEvents()
    {
        weaponChangeUnregister?.UnRegister();
        weaponChangeUnregister = null;

        ammoChangeUnregister?.UnRegister();
        ammoChangeUnregister = null;
    }

    private void RefreshCurrentWeaponUI()
    {
        if (weaponSystem == null || weaponInventoryModel == null)
        {
            UpdateUI("", 0, 0);
            return;
        }

        var currentWeapon = weaponSystem.GetCurrentWeapon();
        var currentSlot = weaponInventoryModel.CurrentSlot;

        UpdateUI(
            currentSlot?.Config?.WeaponName,
            currentWeapon is FirearmWeapon firearm ? firearm.CurrentAmmo : 0,
            currentWeapon is FirearmWeapon firearmWeapon ? firearmWeapon.TotalAmmo : 0);
    }

    private void OnWeaponChanged(EventPlayerChangeWeapon evt)
    {
        var slotName = evt.Slot?.Config?.WeaponName;
        if (evt.WeaponInstance is FirearmWeapon firearmWeapon)
        {
            UpdateUI(slotName, firearmWeapon.CurrentAmmo, firearmWeapon.TotalAmmo);
        }
        else
        {
            UpdateUI(slotName, 0, 0);
        }
    }

    private void OnAmmoChanged(EventWeaponAmmoChanged evt)
    {
        if (weaponInventoryModel?.CurrentSlot == null || weaponInventoryModel.CurrentSlot.WeaponId != evt.WeaponId)
        {
            return;
        }

        UpdateUI(evt.WeaponName, evt.CurrentAmmo, evt.TotalAmmo);
    }

    private void UpdateUI(string weaponName, int currentAmmo, int totalAmmo)
    {
        if (WeaponNameText != null)
        {
            WeaponNameText.text = string.IsNullOrEmpty(weaponName) ? "无武器" : weaponName;
        }

        if (AmmoNumText != null)
        {
            if (totalAmmo > 0)
            {
                AmmoNumText.text = $"{currentAmmo}/{totalAmmo}";
            }
            else
            {
                AmmoNumText.text = "--/--";
            }
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
