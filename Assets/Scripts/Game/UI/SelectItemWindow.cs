/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Author:ZM 铸梦
 *Date:2026/2/22 9:11:29
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
---------------------------------*/
using UnityEngine;
using UnityEngine.UI;
using ZMUIFrameWork;
using QFramework;

public class SelectItemWindow : WindowBase
{
	public SelectItemWindowDataComponent dataCompt;

	private SelectItemWindowRuntimeBridge runtimeBridge;
	private ItemInstance selectedItem;
	private const float SelectBoxOffsetX = 24f;
	private const float SelectBoxPadding = 8f;

	#region 生命周期函数
	public override void OnAwake()
	{
		dataCompt = gameObject.GetComponent<SelectItemWindowDataComponent>();
		dataCompt.InitComponent(this);
		mDisableAnim = true;
		EnsureRuntimeBridge();
		base.OnAwake();
	}

	public override void OnShow()
	{
		EnsureRuntimeBridge();
		base.OnShow();
	}

	public override void OnHide()
	{
		selectedItem = null;
		InventoryItemView.NotifySelectionWindowHidden(this);
		base.OnHide();
	}

	public override void OnDestroy()
	{
		selectedItem = null;
		InventoryItemView.NotifySelectionWindowHidden(this);
		base.OnDestroy();
	}
	#endregion

	#region API Function
	public void ShowForItem(ItemInstance item, RectTransform itemRect)
	{
		if (item == null || item.Definition == null || itemRect == null)
		{
			return;
		}

		selectedItem = item;

		if (dataCompt == null)
		{
			dataCompt = gameObject.GetComponent<SelectItemWindowDataComponent>();
		}

		if (dataCompt == null || dataCompt.SelectBoxImage == null)
		{
			return;
		}

		if (dataCompt.ItemNameText != null)
		{
			dataCompt.ItemNameText.text = item.Definition.Name ?? string.Empty;
		}

		if (dataCompt.ItemInfoText != null)
		{
			dataCompt.ItemInfoText.text = $"数量: {item.Count}\n类型: {item.Definition.Category}";
		}

		if (dataCompt.RotateBtnButton != null)
		{
			dataCompt.RotateBtnButton.gameObject.SetActive(IsItemRotatable(item));
		}

		if (runtimeBridge != null)
		{
			runtimeBridge.RequestDeferredReposition();
		}
		else
		{
			ForceRefreshLayoutBeforePositioning();
			UpdateSelectBoxPosition(itemRect);
		}
	}

	public bool ContainsTarget(Transform target)
	{
		if (target == null || gameObject == null)
		{
			return false;
		}

		return target.IsChildOf(gameObject.transform);
	}

	public void TryRefreshSelectedItemPosition()
	{
		if (selectedItem == null || string.IsNullOrEmpty(selectedItem.InstanceId))
		{
			return;
		}

		if (!InventoryItemView.TryGetItemRect(selectedItem.InstanceId, out var itemRect))
		{
			return;
		}

		ForceRefreshLayoutBeforePositioning();
		UpdateSelectBoxPosition(itemRect);
	}
	#endregion

	#region UI组件事件
	public void OnRotateBtnButtonClick()
	{
		if (selectedItem == null || selectedItem.Definition == null || !IsItemRotatable(selectedItem))
		{
			return;
		}

		var selectedItemId = selectedItem.InstanceId;

		var rotated = false;
		var inventorySystem = this.GetSystem<InventorySystem>();
		if (inventorySystem != null)
		{
			rotated = inventorySystem.TryRotateItemInPlace(selectedItem);
		}

		if (!rotated)
		{
			var warehouseViews = Object.FindObjectsOfType<WarehouseContainerView>(true);
			for (int i = 0; i < warehouseViews.Length; i++)
			{
				var view = warehouseViews[i];
				if (view == null)
				{
					continue;
				}

				if (view.TryRotateItemInPlace(selectedItem))
				{
					rotated = true;
					break;
				}
			}
		}

		if (!rotated)
		{
			return;
		}

		if (!string.IsNullOrEmpty(selectedItemId) &&
			InventoryItemView.TryGetItemRect(selectedItemId, out var itemRect))
		{
			ShowForItem(selectedItem, itemRect);
		}
	}
	#endregion

	private void EnsureRuntimeBridge()
	{
		if (gameObject == null)
		{
			return;
		}

		if (runtimeBridge == null)
		{
			runtimeBridge = gameObject.GetComponent<SelectItemWindowRuntimeBridge>();
		}

		if (runtimeBridge == null)
		{
			runtimeBridge = gameObject.AddComponent<SelectItemWindowRuntimeBridge>();
		}

		runtimeBridge.Bind(this);
	}

	private void UpdateSelectBoxPosition(RectTransform itemRect)
	{
		var selectBoxRect = dataCompt?.SelectBoxImage != null ? dataCompt.SelectBoxImage.rectTransform : null;
		var parentRect = selectBoxRect != null ? selectBoxRect.parent as RectTransform : null;
		if (selectBoxRect == null || parentRect == null)
		{
			return;
		}

		var itemScreenPoint = GetItemRightCenterScreenPoint(itemRect);
		var windowCamera = GetWindowCamera();
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, itemScreenPoint, windowCamera, out var localPoint))
		{
			return;
		}

		var boxSize = selectBoxRect.rect.size;
		var boxPivot = selectBoxRect.pivot;
		var parentSize = parentRect.rect.size;
		var parentPivot = parentRect.pivot;

		var left = -parentSize.x * parentPivot.x;
		var right = left + parentSize.x;
		var bottom = -parentSize.y * parentPivot.y;
		var top = bottom + parentSize.y;

		var minX = left + boxSize.x * boxPivot.x + SelectBoxPadding;
		var maxX = right - boxSize.x * (1f - boxPivot.x) - SelectBoxPadding;
		var minY = bottom + boxSize.y * boxPivot.y + SelectBoxPadding;
		var maxY = top - boxSize.y * (1f - boxPivot.y) - SelectBoxPadding;

		var placeRightX = localPoint.x + SelectBoxOffsetX + boxSize.x * boxPivot.x;
		var placeLeftX = localPoint.x - SelectBoxOffsetX - boxSize.x * (1f - boxPivot.x);
		var targetX = placeRightX;

		if (targetX > maxX && placeLeftX >= minX)
		{
			targetX = placeLeftX;
		}

		targetX = Mathf.Clamp(targetX, minX, maxX);
		var targetY = Mathf.Clamp(localPoint.y, minY, maxY);

		selectBoxRect.anchoredPosition = new Vector2(targetX, targetY);
		Debug.Log($"UpdateSelectBoxPosition  {selectBoxRect.anchoredPosition}");
	}

	private Vector2 GetItemRightCenterScreenPoint(RectTransform itemRect)
	{
		var corners = new Vector3[4];
		itemRect.GetWorldCorners(corners);
		var rightCenterWorld = (corners[2] + corners[3]) * 0.5f;

		var itemCanvas = itemRect.GetComponentInParent<Canvas>();
		var itemCamera = itemCanvas != null && itemCanvas.renderMode != RenderMode.ScreenSpaceOverlay
			? itemCanvas.worldCamera
			: null;

		return RectTransformUtility.WorldToScreenPoint(itemCamera, rightCenterWorld);
	}

	private Camera GetWindowCamera()
	{
		var canvas = gameObject != null ? gameObject.GetComponent<Canvas>() : null;
		if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			return null;
		}

		return canvas.worldCamera;
	}

	private void ForceRefreshLayoutBeforePositioning()
	{
		Canvas.ForceUpdateCanvases();

		var rootRect = gameObject != null ? gameObject.transform as RectTransform : null;
		if (rootRect != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
		}

		if (dataCompt?.SelectBoxImage != null)
		{
			var parentRect = dataCompt.SelectBoxImage.rectTransform.parent as RectTransform;
			if (parentRect != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
			}
		}

		Canvas.ForceUpdateCanvases();
	}

	private static bool IsItemRotatable(ItemInstance item)
	{
		return item != null && item.Definition != null && item.Definition.IsRotatable;
	}
}
