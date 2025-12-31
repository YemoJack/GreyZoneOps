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

	private ItemInstance draggingItem;
	private string draggingOriginId;
	private int draggingOriginPartIndex;
	private ItemPlacement draggingOriginPlacement;
	private IUnRegister inventoryChangedUnreg;

	#region 声明周期函数
	//调用机制与Mono Awake一致
	public override void OnAwake()
	{
		dataCompt = gameObject.GetComponent<InventoryWindowDataComponent>();
		dataCompt.InitComponent(this);
		inventorySystem = this.GetSystem<InventorySystem>();
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
		BindGridCallbacks();
		RegisterInventoryEvents();
		RefreshAll();
	}

	//物体隐藏时执行
	public override void OnHide()
	{
		UnregisterInventoryEvents();
		base.OnHide();
	}

	//物体销毁时执行
	public override void OnDestroy()
	{
		UnregisterInventoryEvents();
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
	}

	private void UnregisterInventoryEvents()
	{
		inventoryChangedUnreg?.UnRegister();
		inventoryChangedUnreg = null;
	}

	private void BindGridCallbacks()
	{
		if (dataCompt?.PlayerInventoryPlayerInventoryView != null)
		{
			dataCompt.PlayerInventoryPlayerInventoryView.BindCallbacks(
				(containerId, part, pos) => HandleTryTake(containerId, part, pos),
				(containerId, part, pos, rotated) => HandleTryPlace(containerId, part, pos, rotated));
		}

		if (dataCompt?.BoxContainerView != null)
		{
			dataCompt.BoxContainerView.BindCallbacks(
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
		if (inventorySystem == null || dataCompt?.BoxContainerView == null) return;
		dataCompt.BoxContainerView.RenderAll();

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
			Debug.Log($"draggingItem {draggingItem.Definition.Id}");
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
	#endregion

}
