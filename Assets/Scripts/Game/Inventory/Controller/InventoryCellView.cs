using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector2Int pos;
    private System.Action<Vector2Int> onHover;
    private System.Action<Vector2Int> onClick;
    [SerializeField] private Image bgImage;
    private Color defaultColor;
    private bool hasDefaultColor;

    public void SetPos(Vector2Int p) => pos = p;
    public void SetHoverCallback(System.Action<Vector2Int> cb) => onHover = cb;
    public void SetClickCallback(System.Action<Vector2Int> cb) => onClick = cb;

    public void Init()
    {
        if (bgImage == null)
        {
            bgImage = GetComponent<Image>();
        }
        if (bgImage != null)
        {
            defaultColor = bgImage.color;
            hasDefaultColor = true;
        }
    }

    public void SetColor(Color color)
    {
        if (bgImage != null)
        {
            bgImage.color = color;
        }
    }

    public void ResetColor()
    {
        if (bgImage != null && hasDefaultColor)
        {
            bgImage.color = defaultColor;
        }
    }

    public void OnPointerEnter(PointerEventData e) => onHover?.Invoke(pos);
    public void OnPointerExit(PointerEventData e) => onHover?.Invoke(new Vector2Int(-1, -1));
    public void OnPointerClick(PointerEventData e) => onClick?.Invoke(pos);
}

