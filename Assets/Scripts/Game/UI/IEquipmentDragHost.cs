using UnityEngine;

public interface IEquipmentDragHost
{
    bool TryPlaceEquipItemToContainer(ItemInstance item, string containerId, int partIndex, Vector2Int pos, bool rotated);
    void ClearDraggingItem();
    void DropItem(ItemInstance item);
}
