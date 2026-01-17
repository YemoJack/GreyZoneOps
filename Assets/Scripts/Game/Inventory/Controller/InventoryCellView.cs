using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector2Int pos;
    private System.Action<Vector2Int> onHover;
    private System.Action<Vector2Int> onClick;

    public void SetPos(Vector2Int p) => pos = p;
    public void SetHoverCallback(System.Action<Vector2Int> cb) => onHover = cb;
    public void SetClickCallback(System.Action<Vector2Int> cb) => onClick = cb;

    public void OnPointerEnter(PointerEventData e) => onHover?.Invoke(pos);
    public void OnPointerExit(PointerEventData e) => onHover?.Invoke(new Vector2Int(-1, -1));
    public void OnPointerClick(PointerEventData e) => onClick?.Invoke(pos);
}

