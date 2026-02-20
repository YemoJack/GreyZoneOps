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
	private IUnRegister openInventoryUnregister;
	private IUnRegister healthChangedUnregister;
	private IUnRegister healthDeathUnregister;
	private IUnRegister extractionStartedUnregister;
	private IUnRegister extractionProgressUnregister;
	private IUnRegister extractionCancelledUnregister;
	private IUnRegister extractionSucceededUnregister;
	private bool gameOverShown;


	#region 生命周期函数
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
		gameOverShown = false;
		RegisterEvents();
		RefreshCurrentWeaponUI();
		RefreshHealthUI();
		ClearExtractionCountdownText();
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
		openInventoryUnregister = this.RegisterEvent<EventOpenInventory>(OnOpenInventory);
		healthChangedUnregister = this.RegisterEvent<EventPlayerHealthChanged>(OnPlayerHealthChanged);
		healthDeathUnregister = this.RegisterEvent<EventPlayerDeath>(OnPlayerDeath);
		extractionStartedUnregister = this.RegisterEvent<EventExtractionStarted>(OnExtractionStarted);
		extractionProgressUnregister = this.RegisterEvent<EventExtractionProgress>(OnExtractionProgress);
		extractionCancelledUnregister = this.RegisterEvent<EventExtractionCancelled>(OnExtractionCancelled);
		extractionSucceededUnregister = this.RegisterEvent<EventExtractionSucceeded>(OnExtractionSucceeded);

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

		healthChangedUnregister?.UnRegister();
		healthChangedUnregister = null;

		healthDeathUnregister?.UnRegister();
		healthDeathUnregister = null;

		extractionStartedUnregister?.UnRegister();
		extractionStartedUnregister = null;

		extractionProgressUnregister?.UnRegister();
		extractionProgressUnregister = null;

		extractionCancelledUnregister?.UnRegister();
		extractionCancelledUnregister = null;

		extractionSucceededUnregister?.UnRegister();
		extractionSucceededUnregister = null;
	}

	private void OnInteracttargetChange(EventInteractTargetChanged e)
	{
		if (dataCompt.InteractPromptText != null)
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

	private void RefreshHealthUI()
	{
		if (dataCompt == null || dataCompt.HealthHealthBarView == null)
		{
			return;
		}

		var health = this.GetSystem<HealthSystem>()?.GetPlayerHealth();
		if (health == null)
		{
			return;
		}

		dataCompt.HealthHealthBarView.SetValue(health.CurrentHealth, health.MaxHealth);
	}

	private void OnPlayerHealthChanged(EventPlayerHealthChanged e)
	{
		if (dataCompt == null || dataCompt.HealthHealthBarView == null)
		{
			return;
		}

		dataCompt.HealthHealthBarView.SetValue(e.Current, e.Max);
	}

	private void OnPlayerDeath(EventPlayerDeath e)
	{
		if (dataCompt != null && dataCompt.HealthHealthBarView != null)
		{
			var max = e.Health != null ? e.Health.MaxHealth : 1f;
			dataCompt.HealthHealthBarView.SetValue(0f, max);
		}
		ShowGameOver(false);
	}

	private void OnExtractionStarted(EventExtractionStarted e)
	{
		SetExtractionCountdownText(e.Duration);
	}

	private void OnExtractionProgress(EventExtractionProgress e)
	{
		SetExtractionCountdownText(e.Remaining);
	}

	private void OnExtractionCancelled(EventExtractionCancelled e)
	{
		ClearExtractionCountdownText();
	}

	private void OnExtractionSucceeded(EventExtractionSucceeded e)
	{
		ClearExtractionCountdownText();
		ShowGameOver(true);
	}

	private void SetExtractionCountdownText(float seconds)
	{
		if (dataCompt == null || dataCompt.ExtractionCountdownText == null)
		{
			return;
		}

		var remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
		dataCompt.ExtractionCountdownText.text = remainingSeconds.ToString();
	}

	private void ClearExtractionCountdownText()
	{
		if (dataCompt == null || dataCompt.ExtractionCountdownText == null)
		{
			return;
		}

		dataCompt.ExtractionCountdownText.text = string.Empty;
	}

	private void ShowGameOver(bool extractedSuccessfully)
	{
		if (gameOverShown)
		{
			return;
		}

		gameOverShown = true;
		var income = 0;
		if (extractedSuccessfully)
		{
			income = this.GetSystem<InventorySystem>()?.GetCurrentRaidIncome() ?? 0;
		}

		var gameOverWindow = UIModule.Instance.PopUpWindow<GameOverWindow>();
		if (gameOverWindow != null)
		{
			gameOverWindow.SetResult(extractedSuccessfully, income);
		}

		UIModule.Instance.HideWindow<GameWindow>();
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


