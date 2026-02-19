using UnityEngine;
using QFramework;

public class CmdDropItem : AbstractCommand
{
    private readonly ItemInstance _item;

    public CmdDropItem(ItemInstance item)
    {
        _item = item;
    }

    protected override void OnExecute()
    {
        if (_item == null || _item.Definition == null)
        {
            Debug.LogWarning("CmdDropItem: item or definition is null.");
            return;
        }
        var pos = GetPlayerPosition();
        Debug.Log($"CmdDropItem {_item.Definition.Id} {_item.InstanceId}");
        SpawnFallbackWorldItem(pos);
    }

    private Vector3 GetPlayerPosition()
    {
        var player = Object.FindObjectOfType<PlayerController>();
        if (player != null) return player.transform.position;

        var cam = Camera.main;
        return cam != null ? cam.transform.position : Vector3.zero;
    }

    private async void SpawnFallbackWorldItem(Vector3 pos)
    {
        if (string.IsNullOrEmpty(_item.Definition.ResName))
        {
            Debug.LogError($"CmdDropItem: ResName is empty, item={_item.Definition.Name} id={_item.Definition.Id}");
            return;
        }

        IResLoader resLoader = this.GetUtility<IResLoader>();
        GameObject obj = await resLoader.LoadAsync<GameObject>(_item.Definition.ResName);
        if (obj == null)
        {
            Debug.LogError($"SpawnFallbackWorldItem is Null, item={_item.Definition.Name}, id={_item.Definition.Id}, res={_item.Definition.ResName}. Create primitive fallback.");
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.transform.position = pos;
            fallback.transform.localScale = Vector3.one * 0.3f;
            fallback.name = "DroppedItemFallback";
            fallback.layer = LayerMask.NameToLayer("Interactable");
            var fallbackInteractable = fallback.AddComponent<WorldItemInteractable>();
            fallbackInteractable.Item = _item;
            return;
        }
        var worldItem = GameObject.Instantiate(obj, pos, Quaternion.identity);
        worldItem.name = "DroppedItem";
        worldItem.transform.position = pos;
        worldItem.transform.localScale = Vector3.one;
        worldItem.layer = LayerMask.NameToLayer("Interactable");
        WorldItemInteractable itemInteractable = worldItem.AddComponent<WorldItemInteractable>();
        itemInteractable.Item = _item;
    }
}
