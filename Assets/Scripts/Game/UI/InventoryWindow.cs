/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Author:ZM 铸梦
 *Date:2025/12/15 21:24:18
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，再次生成不会覆盖原有的代码，会在原有的代码上进行新增，可放心使用
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using ZMUIFrameWork;
using QFramework;

public class InventoryWindow : WindowBase, IController, ICanSendEvent
{
	public InventoryWindowDataComponent dataCompt;

	private InventorySystem inventorySystem;
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
	//调用机制与Mono Awake一致
	public override void OnAwake()
	{
		dataCompt = gameObject.GetComponent<InventoryWindowDataComponent>();
		dataCompt.InitComponent(this);
		inventorySystem = this.GetSystem<InventorySystem>();
		inputSys = this.GetSystem<InputSys>();
		if (inventorySystem == null)
		{
			Debug.LogError("InventorySystem is Null");
		}
		base.OnAwake();
	}

	//物体显示时执行
	public override void OnShow()
	{
		base.OnShow();
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

	//物体隐藏时执行
	public override void OnHide()
	{
		UnregisterInventoryEvents();
		if (inputSys != null)
		{
			inputSys.SetInputEnabled(true);
		}
		base.OnHide();
	}

	//物体销毁时执行
	public override void OnDestroy()
	{
		UnregisterInventoryEvents();
		if (inputSys != null)
		{
			inputSys.SetInputEnabled(true);
		}
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
		if (inventorySystem == null || dataCompt?.PlayerInventoryPlayerInventoryView == null) return;
		dataCompt.PlayerInventoryPlayerInventoryView.RenderAll();

	}

	private void RefreshInteract()
	{
		if (inventorySystem == null) return;
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
		// 已经有拖拽物，避免重复拿取
		if (draggingItem != null) return false;
		var grid = inventorySystem.GetGrid(id, partIndex);
		if (grid == null) return false;

		var itemAt = grid.GetItemAt(pos);
		if (itemAt == null) return false;

		// 记录原始位置，便于放置失败回滚
		draggingOriginPlacement = grid.GetPlacement(itemAt);
		draggingOriginId = id;
		draggingOriginPartIndex = partIndex;

		if (inventorySystem.TryTakeItemAt(id, pos, out var item, notify: false, partIndex: partIndex))
		{
			draggingItem = item;
			//Debug.Log($"draggingItem {draggingItem.Definition.Id}");
			return true;
		}

		draggingOriginPlacement = null;
		return false;
	}

	private bool HandleTryPlace(string id, int partIndex, Vector2Int pos, bool rotated)
	{
		if (draggingItem == null) return false;

		// int.MinValue 标记：完全拖出网格，丢弃
		if (pos.x == int.MinValue || pos.y == int.MinValue)
		{
			DropItem(draggingItem);
			draggingItem = null;
			draggingOriginPlacement = null;
			this.SendEvent(new InventoryChangedEvent());
			return true;
		}

		// 负数但非最小值：表示有网格命中但位置非法，回滚
		if (pos.x < 0 || pos.y < 0)
		{
			if (draggingOriginPlacement != null)
			{
				inventorySystem.TryPlaceItem(
					draggingOriginId,
					draggingItem,
					draggingOriginPlacement.Pos,
					draggingOriginPlacement.Rotated,
					partIndex: draggingOriginPartIndex);
				draggingOriginPlacement = null;
			}
			draggingItem = null;
			return false;
		}

		var placed = false;
		if (pos.x >= 0 && pos.y >= 0)
		{
			placed = inventorySystem.TryPlaceItem(id, draggingItem, pos, rotated, partIndex);
		}

		if (placed)
		{
			draggingItem = null;
			draggingOriginPlacement = null;
			draggingOriginEquipSlot = null;
			return true;
		}

		// 放置失败时回滚到原位置
		if (draggingOriginPlacement != null)
		{
			inventorySystem.TryPlaceItem(
				draggingOriginId,
				draggingItem,
				draggingOriginPlacement.Pos,
				draggingOriginPlacement.Rotated,
				partIndex: draggingOriginPartIndex);
			draggingOriginPlacement = null;
		}

		draggingItem = null;
		if (placed)
			return true;

		return false;
	}

	private bool HandleTryEquipTake(EquipmentSlotType slot)
	{
		if (draggingItem != null) return false;
		if (inventorySystem == null) return false;

		if (inventorySystem.TryUnequipItem(slot, out var item))
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
		if (inventorySystem == null) return false;

		var equipment = inventorySystem.GetPlayerEquipment();
		if (equipment != null && equipment.GetItem(slot) != null) return false;

		var placed = inventorySystem.TryEquipItem(slot, draggingItem, out _);
		if (placed)
		{
			draggingItem = null;
			draggingOriginPlacement = null;
			draggingOriginEquipSlot = null;
			return true;
		}

		if (draggingOriginEquipSlot.HasValue)
		{
			inventorySystem.TryEquipItem(draggingOriginEquipSlot.Value, draggingItem, out _);
			draggingOriginEquipSlot = null;
			draggingItem = null;
		}

		return false;
	}

	private bool HandleTryEquipSwap(EquipmentSlotType fromSlot, EquipmentSlotType toSlot)
	{
		if (inventorySystem == null) return false;
		var fromItem = draggingItem;
		var swapped = inventorySystem.TrySwapEquip(fromSlot, toSlot, fromItem);
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
		if (!string.IsNullOrEmpty(chestId) && inventorySystem.TryAutoPlace(chestId, item)) return true;

		var backpackId = model.GetPlayerContainerId(InventoryContainerType.Backpack);
		if (!string.IsNullOrEmpty(backpackId) && inventorySystem.TryAutoPlace(backpackId, item)) return true;

		var pocketId = model.GetPlayerContainerId(InventoryContainerType.Pocket);
		if (!string.IsNullOrEmpty(pocketId) && inventorySystem.TryAutoPlace(pocketId, item)) return true;

		return false;
	}

	private ItemInstance HandleEquipBeginDrag(EquipmentSlotType slot)
	{
		if (draggingItem != null) return null;
		if (inventorySystem == null) return null;

		if (inventorySystem.TryUnequipItem(slot, out var item))
		{
			draggingItem = item;
			draggingOriginEquipSlot = slot;
			return item;
		}

		return null;
	}

	private bool HandleEquipReturn(EquipmentSlotType slot, ItemInstance item)
	{
		if (inventorySystem == null || item == null)
		{
			draggingItem = null;
			draggingOriginEquipSlot = null;
			return false;
		}

		var placed = inventorySystem.TryEquipItem(slot, item, out _);
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

	public void DropItem(ItemInstance item)
	{
		if (inventorySystem == null || item == null) return;
		inventorySystem.DropItem(item);
	}

	public bool TryPlaceEquipItemToContainer(ItemInstance item, string containerId, int partIndex, Vector2Int pos, bool rotated)
	{
		if (inventorySystem == null || item == null) return false;
		if (string.IsNullOrEmpty(containerId)) return false;
		var placed = inventorySystem.TryPlaceItem(containerId, item, pos, rotated, partIndex);
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
	#endregion

}
