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
        if (_item == null) return;
        var pos = GetPlayerPosition();
        Debug.Log($"CmdDropItem {_item.Definition.Id} {_item.InstanceId}");
        SpawnFallbackCube(pos);
    }

    private Vector3 GetPlayerPosition()
    {
        var player = Object.FindObjectOfType<PlayerController>();
        if (player != null) return player.transform.position;

        var cam = Camera.main;
        return cam != null ? cam.transform.position : Vector3.zero;
    }

    private void SpawnFallbackCube(Vector3 pos)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "DroppedItem";
        cube.transform.position = pos + new Vector3(0f, 0.2f, 0f);
        cube.transform.localScale = Vector3.one * 0.3f;
    }
}
