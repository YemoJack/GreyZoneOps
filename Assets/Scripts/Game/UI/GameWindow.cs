/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Author:ZM 铸梦
 *Date:2025/12/14 20:10:50
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，再次生成不会覆盖原有的代码，会在原有的代码上进行新增，可放心使用
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


	#region 声明周期函数
	//调用机制与Mono Awake一致
	public override void OnAwake()
	{
		dataCompt = gameObject.GetComponent<GameWindowDataComponent>();
		dataCompt.InitComponent(this);

		base.OnAwake();

		weaponSystem = this.GetSystem<WeaponSystem>();
		weaponInventoryModel = this.GetModel<WeaponInventoryModel>();
	}
	//物体显示时执行
	public override void OnShow()
	{
		base.OnShow();
		RegisterEvents();
		RefreshCurrentWeaponUI();
	}
	//物体隐藏时执行
	public override void OnHide()
	{
		base.OnHide();
		UnregisterEvents();
	}
	//物体销毁时执行
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
			dataCompt.WeaponNameText.text = string.IsNullOrEmpty(weaponName) ? "无武器" : weaponName;
		}

		if (dataCompt.AmmoNumText != null)
		{
			if (totalAmmo > 0)
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
	#region UI组件事件
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
