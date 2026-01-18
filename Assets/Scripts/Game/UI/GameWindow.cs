/*---------------------------------
 *Title:UI琛ㄧ幇灞傝剼鏈嚜鍔ㄥ寲鐢熸垚宸ュ叿
 *Author:ZM 閾告ⅵ
 *Date:2025/12/14 20:10:50
 *Description:UI 琛ㄧ幇灞傦紝璇ュ眰鍙礋璐ｇ晫闈㈢殑浜や簰銆佽〃鐜扮浉鍏崇殑鏇存柊锛屼笉鍏佽缂栧啓浠讳綍涓氬姟閫昏緫浠ｇ爜
 *娉ㄦ剰:浠ヤ笅鏂囦欢鏄嚜鍔ㄧ敓鎴愮殑锛屽啀娆＄敓鎴愪笉浼氳鐩栧師鏈夌殑浠ｇ爜锛屼細鍦ㄥ師鏈夌殑浠ｇ爜涓婅繘琛屾柊澧烇紝鍙斁蹇冧娇鐢?
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using ZMUIFrameWork;
using QFramework;
using System;

public class GameWindow : WindowBase
{

	public GameWindowDataComponent dataCompt;



	private WeaponSystem weaponSystem;
	private WeaponInventoryModel weaponInventoryModel;

	private IUnRegister weaponChangeUnregister;
	private IUnRegister ammoChangeUnregister;

	private IUnRegister interacttargetChangeUnregister;
	private IUnRegister openContainerUnregister;
	private IUnRegister openInventoryUnregister;


	#region 澹版槑鍛ㄦ湡鍑芥暟
	//璋冪敤鏈哄埗涓嶮ono Awake涓€鑷?
	public override void OnAwake()
	{
		dataCompt = gameObject.GetComponent<GameWindowDataComponent>();
		dataCompt.InitComponent(this);

		base.OnAwake();

		weaponSystem = this.GetSystem<WeaponSystem>();
		weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
	}
	//鐗╀綋鏄剧ず鏃舵墽琛?
	public override void OnShow()
	{
		base.OnShow();
		RegisterEvents();
		RefreshCurrentWeaponUI();
	}
	//鐗╀綋闅愯棌鏃舵墽琛?
	public override void OnHide()
	{
		base.OnHide();
		UnregisterEvents();
	}
	//鐗╀綋閿€姣佹椂鎵ц
	public override void OnDestroy()
	{
		base.OnDestroy();
	}
	#endregion
	#region API Function


	private void RegisterEvents()
	{
		weaponChangeUnregister = this.RegisterEvent<EventPlayerChangeWeapon>(OnWeaponChanged);
		ammoChangeUnregister = this.RegisterEvent<EventWeaponAmmoChanged>(OnAmmoChanged);
		interacttargetChangeUnregister = this.RegisterEvent<EventInteractTargetChanged>(OnInteracttargetChange);
		openContainerUnregister = this.RegisterEvent<EventOpenContainer>(OnOpenContainer);
		openInventoryUnregister = this.RegisterEvent<EventOpenInventory>(OnOpenInventory);

	}

	private void UnregisterEvents()
	{
		weaponChangeUnregister?.UnRegister();
		weaponChangeUnregister = null;

		ammoChangeUnregister?.UnRegister();
		ammoChangeUnregister = null;

		interacttargetChangeUnregister?.UnRegister();
		interacttargetChangeUnregister = null;

		openContainerUnregister?.UnRegister();
		openContainerUnregister = null;

		openInventoryUnregister?.UnRegister();
		openInventoryUnregister = null;
	}

	private void OnInteracttargetChange(EventInteractTargetChanged e)
	{
		dataCompt.InteractPromptText.text = e.Info.Prompt;
	}

	private void OnOpenContainer(EventOpenContainer e)
	{
		var context = ResolveOpenContext(e);
		if (context.Source != InventoryOpenSource.ContainerInteraction || string.IsNullOrEmpty(context.ContainerId))
		{
			return;
		}
		UIModule.Instance.PopUpWindow<InventoryWindow>();
		var window = UIModule.Instance.GetWindow<InventoryWindow>();
		if (window != null)
		{
			window.ApplyOpenContext(context);
		}
	}

	private void OnOpenInventory(EventOpenInventory e)
	{
		OnPlayerInventoryButtonClick();
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
		if (dataCompt != null)
		{
			dataCompt.WeaponNameText.text = string.IsNullOrEmpty(weaponName) ? string.Empty : weaponName;
		}

		if (dataCompt.AmmoNumText != null)
		{
			if (string.IsNullOrEmpty(weaponName))
			{
				dataCompt.AmmoNumText.text = string.Empty;
			}
			else if (totalAmmo > 0)
			{
				dataCompt.AmmoNumText.text = $"{currentAmmo}/{totalAmmo}";
			}
			else
			{
				dataCompt.AmmoNumText.text = "--/--";
			}
		}
	}




	#endregion
	#region UI缁勪欢浜嬩欢
	public void OnPlayerInventoryButtonClick()
	{
		UIModule.Instance.PopUpWindow<InventoryWindow>();
		var window = UIModule.Instance.GetWindow<InventoryWindow>();
		if (window != null)
		{
			window.ApplyOpenContext(InventoryOpenContext.FromBackpack());
		}

	}

	private InventoryOpenContext ResolveOpenContext(EventOpenContainer e)
	{
		var context = e.OpenContext;
		if (!string.IsNullOrEmpty(e.ContainerId) && string.IsNullOrEmpty(context.ContainerId))
		{
			context = InventoryOpenContext.FromContainer(e.ContainerId);
		}
		if (context.Source == InventoryOpenSource.BackpackButton && !string.IsNullOrEmpty(context.ContainerId))
		{
			context = context.WithContainer(context.ContainerId);
		}
		return context;
	}
	#endregion
}

