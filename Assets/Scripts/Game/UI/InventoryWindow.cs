/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Author:ZM 铸梦
 *Date:2025/12/15 21:24:18
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，再次生成不会覆盖原有的代码，会在原有的代码上进行新增，可放心使�?
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using ZMUIFrameWork;
using QFramework;

public class InventoryWindow : WindowBase, IController, ICanSendEvent, IEquipmentDragHost
{
	public InventoryWindowDataComponent dataCompt;

	private InventorySystem raidInventorySystem;
	private InputSys inputSys;

	private ItemInstance draggingItem;
	private string draggingOriginId;
	private int draggingOriginPartIndex;
	private ItemPlacement draggingOriginPlacement;
	private EquipmentSlotType? draggingOriginEquipSlot;
	private IUnRegister inventoryChangedUnreg;
	private IUnRegister openContainerUnreg;
	private InventoryOpenContext openContext = InventoryOpenContext.FromBackpack();

	#region 声明周期函数
	//调用机制与Mono Awake一�?
	public override void OnAwake()
	{
		dataCompt = gameObject.GetComponent<InventoryWindowDataComponent>();
		dataCompt.InitComponent(this);
		raidInventorySystem = this.GetSystem<InventorySystem>();
		inputSys = this.GetSystem<InputSys>();
		if (raidInventorySystem == null)
		{
			Debug.LogError("RaidInventorySystem is Null");
		}
		base.OnAwake();
	}

	//物体显示时执�?
	public override void OnShow()
	{
		base.OnShow();
		ClearDraggingItem();
		SetCursorVisible(true);
		if (inputSys != null)
		{
			inputSys.SetInputEnabled(false);
		}
		InitPlayerInventory();
		BindGridCallbacks();
		ApplyOpenContext(openContext);
		RegisterInventoryEvents();
		RefreshAll();
	}

	//物体隐藏时执�?
	public override void OnHide()
	{
		UnregisterInventoryEvents();
		ClearDraggingItem();
		UIModule.Instance.HideWindow<SelectItemWindow>();
		if (inputSys != null)
		{
			inputSys.SetInputEnabled(true);
		}
		SetCursorVisible(false);
		base.OnHide();
	}

	//物体销毁时执行
	public override void OnDestroy()
	{
		UnregisterInventoryEvents();
		ClearDraggingItem();
		UIModule.Instance.HideWindow<SelectItemWindow>();
		if (inputSys != null)
		{
			inputSys.SetInputEnabled(true);
		}
		SetCursorVisible(false);
		base.OnDestroy();
	}
	#endregion

	#region UI组件事件
	public void OnCloseButtonClick()
	{
		HideWindow();
	}
	#endregion

	#region Public API

	#endregion

	#region Internal
	private void RegisterInventoryEvents()
	{
		inventoryChangedUnreg = this.RegisterEvent<InventoryChangedEvent>(_ => RefreshAll());
		openContainerUnreg = this.RegisterEvent<EventOpenContainer>(OnOpenContainer);
	}

	private void UnregisterInventoryEvents()
	{
		inventoryChangedUnreg?.UnRegister();
		inventoryChangedUnreg = null;
		openContainerUnreg?.UnRegister();
		openContainerUnreg = null;
	}

	private void InitPlayerInventory()
	{
		if (dataCompt?.PlayerInventoryPlayerInventoryView != null)
		{
			dataCompt.PlayerInventoryPlayerInventoryView.InitPlayerInventory();
		}
	}

	private void InitSceneContainer()
	{
		if (dataCompt?.SceneInventorySceneContainerView != null)
		{
			dataCompt?.SceneInventorySceneContainerView.InitSceneContainer();
		}
	}



	private void BindGridCallbacks()
	{
		if (dataCompt?.PlayerInventoryPlayerInventoryView != null)
		{
			dataCompt.PlayerInventoryPlayerInventoryView.BindCallbacks(
				(containerId, part, pos) => HandleTryTake(containerId, part, pos),
				(containerId, part, pos, rotated) => HandleTryPlace(containerId, part, pos, rotated),
				(equipType) => HandleTryEquipTake(equipType),
				(equipType) => HandleTryEquipPlace(equipType),
				(fromSlot, toSlot) => HandleTryEquipSwap(fromSlot, toSlot),
				(equipType) => HandleEquipBeginDrag(equipType),
				(equipType, item) => HandleEquipReturn(equipType, item));
		}



		if (dataCompt?.SceneInventorySceneContainerView != null)
		{
			dataCompt?.SceneInventorySceneContainerView.BindCallbacks(
				(containerId, part, pos) => HandleTryTake(containerId, part, pos),
				(containerId, part, pos, rotated) => HandleTryPlace(containerId, part, pos, rotated));
		}

	}

	private void RefreshAll()
	{
		RefreshPlayer();
		RefreshInteract();

	}

	private void RefreshPlayer()
	{
		if (raidInventorySystem == null || dataCompt?.PlayerInventoryPlayerInventoryView == null) return;
		dataCompt.PlayerInventoryPlayerInventoryView.RenderAll();

	}

	private void RefreshInteract()
	{
		if (raidInventorySystem == null) return;
		if (dataCompt?.SceneInventorySceneContainerView == null) return;
		if (!dataCompt.SceneInventorySceneContainerView.IsVisible) return;
		dataCompt.SceneInventorySceneContainerView.RenderAll();

	}

	private void OnOpenContainer(EventOpenContainer e)
	{
		ApplyOpenContext(ResolveOpenContext(e));
	}



	private bool HandleTryTake(string id, int partIndex, Vector2Int pos)
	{
		// 已经有拖拽物，避免重复拿�?
		if (draggingItem != null) return false;
		var grid = raidInventorySystem.GetGrid(id, partIndex);
		if (grid == null) return false;

		var itemAt = grid.GetItemAt(pos);
		if (itemAt == null) return false;

		// 记录原始位置，便于放置失败回�?
		draggingOriginPlacement = grid.GetPlacement(itemAt);
		draggingOriginId = id;
		draggingOriginPartIndex = partIndex;
		draggingOriginEquipSlot = null;

		if (raidInventorySystem.TryTakeItemAt(id, pos, out var item, notify: false, partIndex: partIndex))
		{
			draggingItem = item;
			//Debug.Log($"draggingItem {draggingItem.Definition.Id}");
			return true;
		}

		draggingOriginPlacement = null;
		draggingOriginId = null;
		return false;
	}

	private bool HandleTryPlace(string id, int partIndex, Vector2Int pos, bool rotated)
	{
		if (draggingItem == null) return false;

		// int.MinValue 标记：完全拖出网格，丢弃
		if (pos.x == int.MinValue || pos.y == int.MinValue)
		{
			DropItem(draggingItem);
			ClearDraggingItem();
			RefreshAll();
			return true;
		}

		// 负数但非最小值：表示有网格命中但位置非法，回�?
		if (pos.x < 0 || pos.y < 0)
		{
			ReturnDraggingItemToOrigin();
			return false;
		}

		var placed = false;
		if (pos.x >= 0 && pos.y >= 0)
		{
			placed = raidInventorySystem.TryPlaceItem(id, draggingItem, pos, rotated, partIndex);
		}

		if (placed)
		{
			draggingItem = null;
			draggingOriginPlacement = null;
			draggingOriginEquipSlot = null;
			return true;
		}

		// 放置失败时回滚到原位�?
		if (draggingOriginPlacement != null)
		{
			raidInventorySystem.TryPlaceItem(
				draggingOriginId,
				draggingItem,
				draggingOriginPlacement.Pos,
				draggingOriginPlacement.Rotated,
				partIndex: draggingOriginPartIndex);
		}

		ClearDraggingItem();
		if (placed)
			return true;

		return false;
	}

	private bool HandleTryEquipTake(EquipmentSlotType slot)
	{
		if (draggingItem != null) return false;
		if (raidInventorySystem == null) return false;

		if (raidInventorySystem.TryUnequipItem(slot, out var item))
		{
			if (TryAutoPlaceToPlayerContainers(item))
			{
				return true;
			}

			// No space in player containers: discard the item.
			DropItem(item);
			draggingItem = null;
			draggingOriginEquipSlot = null;
			return true;
		}

		return false;
	}

	private bool HandleTryEquipPlace(EquipmentSlotType slot)
	{
		if (draggingItem == null) return false;
		if (raidInventorySystem == null) return false;

		var equipment = raidInventorySystem.GetPlayerEquipment();
		if (equipment != null && equipment.GetItem(slot) != null) return false;

		var placed = raidInventorySystem.TryEquipItem(slot, draggingItem, out _);
		if (placed)
		{
			draggingItem = null;
			draggingOriginPlacement = null;
			draggingOriginEquipSlot = null;
			return true;
		}

		if (draggingOriginEquipSlot.HasValue)
		{
			raidInventorySystem.TryEquipItem(draggingOriginEquipSlot.Value, draggingItem, out _);
			draggingOriginEquipSlot = null;
			draggingItem = null;
		}

		return false;
	}

	private bool HandleTryEquipSwap(EquipmentSlotType fromSlot, EquipmentSlotType toSlot)
	{
		if (raidInventorySystem == null) return false;
		var fromItem = draggingItem;
		var swapped = raidInventorySystem.TrySwapEquip(fromSlot, toSlot, fromItem);
		if (swapped)
		{
			draggingItem = null;
			draggingOriginEquipSlot = null;
		}
		return swapped;
	}

	private bool TryAutoPlaceToPlayerContainers(ItemInstance item)
	{
		if (item == null) return false;
		var model = this.GetModel<InventoryContainerModel>();
		if (model == null) return false;

		var chestId = model.GetPlayerContainerId(InventoryContainerType.ChestRig);
		if (!string.IsNullOrEmpty(chestId) && raidInventorySystem.TryAutoPlace(chestId, item)) return true;

		var backpackId = model.GetPlayerContainerId(InventoryContainerType.Backpack);
		if (!string.IsNullOrEmpty(backpackId) && raidInventorySystem.TryAutoPlace(backpackId, item)) return true;

		var pocketId = model.GetPlayerContainerId(InventoryContainerType.Pocket);
		if (!string.IsNullOrEmpty(pocketId) && raidInventorySystem.TryAutoPlace(pocketId, item)) return true;

		return false;
	}

	private ItemInstance HandleEquipBeginDrag(EquipmentSlotType slot)
	{
		if (draggingItem != null) return null;
		if (raidInventorySystem == null) return null;

		if (raidInventorySystem.TryUnequipItem(slot, out var item))
		{
			draggingItem = item;
			draggingOriginEquipSlot = slot;
			return item;
		}

		return null;
	}

	private bool HandleEquipReturn(EquipmentSlotType slot, ItemInstance item)
	{
		if (raidInventorySystem == null || item == null)
		{
			draggingItem = null;
			draggingOriginEquipSlot = null;
			return false;
		}

		var placed = raidInventorySystem.TryEquipItem(slot, item, out _);
		draggingItem = null;
		draggingOriginEquipSlot = null;
		return placed;
	}

	public void ClearDraggingItem()
	{
		draggingItem = null;
		draggingOriginEquipSlot = null;
		draggingOriginPlacement = null;
		draggingOriginId = null;
		draggingOriginPartIndex = 0;
	}

	private void ReturnDraggingItemToOrigin()
	{
		if (draggingItem == null)
		{
			ClearDraggingItem();
			return;
		}

		if (draggingOriginPlacement != null && !string.IsNullOrEmpty(draggingOriginId))
		{
			raidInventorySystem?.TryPlaceItem(
				draggingOriginId,
				draggingItem,
				draggingOriginPlacement.Pos,
				draggingOriginPlacement.Rotated,
				partIndex: draggingOriginPartIndex);
		}
		else if (draggingOriginEquipSlot.HasValue)
		{
			if (raidInventorySystem != null)
			{
				raidInventorySystem.TryEquipItem(draggingOriginEquipSlot.Value, draggingItem, out _);
			}
		}

		ClearDraggingItem();
	}

	public void DropItem(ItemInstance item)
	{
		if (raidInventorySystem == null || item == null) return;
		raidInventorySystem.DropItem(item);
	}

	public bool TryPlaceEquipItemToContainer(ItemInstance item, string containerId, int partIndex, Vector2Int pos, bool rotated)
	{
		if (raidInventorySystem == null || item == null) return false;
		if (string.IsNullOrEmpty(containerId)) return false;
		var placed = raidInventorySystem.TryPlaceItem(containerId, item, pos, rotated, partIndex);
		if (placed)
		{
			draggingItem = null;
			draggingOriginPlacement = null;
			draggingOriginEquipSlot = null;
		}
		return placed;
	}

	public void SetSceneContainer(string containerId)
	{
		ApplyOpenContext(InventoryOpenContext.FromContainer(containerId));
	}

	public void ApplyOpenContext(InventoryOpenContext context)
	{
		openContext = context;

		if (dataCompt?.SceneInventorySceneContainerView == null) return;
		var showScene = openContext.ShowSceneContainer;
		dataCompt.SceneInventorySceneContainerView.SetVisible(showScene);

		if (!showScene) return;

		if (!string.IsNullOrEmpty(openContext.ContainerId))
		{
			dataCompt.SceneInventorySceneContainerView.SetContainerById(openContext.ContainerId);
		}

		InitSceneContainer();
		RefreshInteract();
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

	private void SetCursorVisible(bool visible)
	{
		Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
		Cursor.visible = visible;
	}
	#endregion

}

