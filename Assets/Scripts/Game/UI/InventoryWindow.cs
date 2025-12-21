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

public class InventoryWindow : WindowBase, IController
{
	public InventoryWindowDataComponent dataCompt;

	[Header("容器类型")]
	public InventoryContainerType playerContainer = InventoryContainerType.Backpack;
	public InventoryContainerType interactContainer = InventoryContainerType.LootBox;

	private InventorySystem inventorySystem;

	private ItemInstance draggingItem;
	private InventoryContainerType draggingOriginType;
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
		if (dataCompt?.PlayerBagInventoryGridView != null)
		{
			dataCompt.PlayerBagInventoryGridView.OnTryTake = pos => HandleTryTake(playerContainer, pos);
			dataCompt.PlayerBagInventoryGridView.OnTryPlace = (pos, rotated) => HandleTryPlace(playerContainer, pos, rotated);
		}

		if (dataCompt?.ContainerInventoryGridView != null)
		{
			dataCompt.ContainerInventoryGridView.OnTryTake = pos => HandleTryTake(interactContainer, pos);
			dataCompt.ContainerInventoryGridView.OnTryPlace = (pos, rotated) => HandleTryPlace(interactContainer, pos, rotated);
		}
	}

	private void RefreshAll()
	{
		RefreshPlayer();
		RefreshInteract();
	}

	private void RefreshPlayer()
	{
		if (inventorySystem == null || dataCompt?.PlayerBagInventoryGridView == null) return;
		var grid = inventorySystem.GetGrid(playerContainer);
		if (grid != null)
			dataCompt.PlayerBagInventoryGridView.Render(playerContainer, grid);
	}

	private void RefreshInteract()
	{
		if (inventorySystem == null || dataCompt?.ContainerInventoryGridView == null) return;
		var grid = inventorySystem.GetGrid(interactContainer);
		if (grid != null)
			dataCompt.ContainerInventoryGridView.Render(interactContainer, grid);
	}

	private bool HandleTryTake(InventoryContainerType type, Vector2Int pos)
	{
		// 已经有拖拽物，避免重复拿取
		if (draggingItem != null) return false;
		var grid = inventorySystem.GetGrid(type);
		if (grid == null) return false;

		var itemAt = grid.GetItemAt(pos);
		if (itemAt == null) return false;

		// 记录原始位置，便于放置失败回滚
		draggingOriginPlacement = grid.GetPlacement(itemAt);
		draggingOriginType = type;

		if (inventorySystem.TryTakeItemAt(type, pos, out var item, notify: false))
		{
			draggingItem = item;
			Debug.Log($"draggingItem {draggingItem.Definition.Id}");
			return true;
		}

		draggingOriginPlacement = null;
		return false;
	}

	private bool HandleTryPlace(InventoryContainerType type, Vector2Int pos, bool rotated)
	{
		if (draggingItem == null) return false;

		var placed = false;
		if (pos.x >= 0 && pos.y >= 0)
		{
			placed = inventorySystem.TryPlaceItem(type, draggingItem, pos, rotated);
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
				draggingOriginType,
				draggingItem,
				draggingOriginPlacement.Pos,
				draggingOriginPlacement.Rotated);
			draggingOriginPlacement = null;
		}

		draggingItem = null;
		if (placed)
			return true;

		return false;
	}
	#endregion

}
