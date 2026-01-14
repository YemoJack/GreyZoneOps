using QFramework;
using UnityEngine;

public class CmdPickupItem : AbstractCommand
{
    private readonly ItemInstance _item;
    private readonly GameObject _source;

    public CmdPickupItem(ItemInstance item, GameObject source)
    {
        _item = item;
        _source = source;
    }

    protected override void OnExecute()
    {
        if (_item == null || _item.Definition == null || _item.Count <= 0) return;

        var system = this.GetSystem<InventorySystem>();
        if (system == null) return;

        var placed = system.TryAutoPlaceToPlayerContainers(_item);
        if (placed && _source != null)
        {
            Object.Destroy(_source);
        }
    }
}
