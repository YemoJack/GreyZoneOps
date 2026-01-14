using QFramework;
using UnityEngine;

public class CmdPickupItem : AbstractCommand
{
    private readonly SOItemDefinition _definition;
    private readonly int _count;
    private readonly GameObject _source;

    public CmdPickupItem(SOItemDefinition definition, int count, GameObject source)
    {
        _definition = definition;
        _count = count;
        _source = source;
    }

    protected override void OnExecute()
    {
        if (_definition == null) return;

        var system = this.GetSystem<InventorySystem>();
        if (system == null) return;

        var item = new ItemInstance(_definition, Mathf.Max(1, _count));
        var placed = system.TryAutoPlaceToPlayerContainers(item);
        if (placed && _source != null)
        {
            Object.Destroy(_source);
        }
    }
}
