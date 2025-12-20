using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QFramework;

public class CmdTryPlaceItem : AbstractCommand
{
    private InventoryContainerType _type;
    private ItemInstance _item;
    private Vector2Int _pos;
    private bool _rotated;

    public CmdTryPlaceItem(
        InventoryContainerType type,
        ItemInstance item,
        Vector2Int pos,
        bool rotated)
    {
        _type = type;
        _item = item;
        _pos = pos;
        _rotated = rotated;
    }

    protected override void OnExecute()
    {
        var system = this.GetSystem<InventorySystem>();
        if (system.TryPlaceItem(_type, _item, _pos, _rotated))
        {
            this.SendEvent(new InventoryChangedEvent());
        }
    }
}

