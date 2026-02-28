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
        var pos = GetDropSpawnPosition();
        Debug.Log($"CmdDropItem {_item.Definition.Id} {_item.InstanceId}");
        SpawnFallbackWorldItem(pos);
    }

    private Vector3 GetDropSpawnPosition()
    {
        var player = Object.FindObjectOfType<PlayerController>();
        var playerPos = player != null ? player.transform.position : Vector3.zero;

        var cam = Camera.main;
        if (cam != null)
        {
            var origin = cam.transform.position;
            var forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = cam.transform.forward;
            }
            forward.Normalize();

            var candidate = origin + forward * 1.2f;
            candidate.y = player != null ? player.transform.position.y + 0.2f : candidate.y;

            if (Physics.Raycast(candidate + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                candidate = hit.point + Vector3.up * 0.1f;
            }

            return candidate;
        }

        return playerPos;
    }

    private async void SpawnFallbackWorldItem(Vector3 pos)
    {
        var weaponPrefab = _item.Definition.ResolveWeaponPrefab();
        if (weaponPrefab != null)
        {
            SpawnWorldItemFromPrefab(weaponPrefab, pos, "DroppedWeapon");
            return;
        }

        if (string.IsNullOrEmpty(_item.Definition.ResName))
        {
            Debug.LogWarning($"CmdDropItem: ResName is empty, use primitive fallback. item={_item.Definition.Name} id={_item.Definition.Id}");
            CreatePrimitiveFallback(pos);
            return;
        }

        IResLoader resLoader = this.GetUtility<IResLoader>();
        GameObject obj = await resLoader.LoadAsync<GameObject>(_item.Definition.ResName);
        if (obj == null)
        {
            Debug.LogError($"SpawnFallbackWorldItem is Null, item={_item.Definition.Name}, id={_item.Definition.Id}, res={_item.Definition.ResName}. Create primitive fallback.");
            CreatePrimitiveFallback(pos);
            return;
        }
        SpawnWorldItemFromPrefab(obj, pos, "DroppedItem");
    }

    private void CreatePrimitiveFallback(Vector3 pos)
    {
        var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.transform.position = pos;
        fallback.transform.localScale = Vector3.one * 0.3f;
        fallback.name = "DroppedItemFallback";
        SetLayerRecursively(fallback, LayerMask.NameToLayer("Interactable"));
        var fallbackInteractable = fallback.GetComponent<WorldItemInteractable>();
        if (fallbackInteractable == null)
        {
            fallbackInteractable = fallback.AddComponent<WorldItemInteractable>();
        }
        fallbackInteractable.Item = _item;
    }

    private void SpawnWorldItemFromPrefab(GameObject prefab, Vector3 pos, string objectName)
    {
        if (prefab == null)
        {
            CreatePrimitiveFallback(pos);
            return;
        }

        var worldItem = GameObject.Instantiate(prefab, pos, Quaternion.identity);
        worldItem.name = objectName;
        worldItem.transform.position = pos;
        worldItem.transform.localScale = Vector3.one;

        PrepareDroppedWorldItem(worldItem);
    }

    private void PrepareDroppedWorldItem(GameObject worldItem)
    {
        if (worldItem == null)
        {
            return;
        }

        SetLayerRecursively(worldItem, LayerMask.NameToLayer("Interactable"));
        EnsureAnyColliderOnRoot(worldItem);
        DisableWeaponRuntimeBehaviours(worldItem);

        var itemInteractable = worldItem.GetComponent<WorldItemInteractable>();
        if (itemInteractable == null)
        {
            itemInteractable = worldItem.AddComponent<WorldItemInteractable>();
        }
        itemInteractable.Item = _item;
    }

    private static void DisableWeaponRuntimeBehaviours(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (var weapon in root.GetComponentsInChildren<WeaponBase>(true))
        {
            if (weapon != null)
            {
                weapon.enabled = false;
            }
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    private static void EnsureAnyColliderOnRoot(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        if (root.GetComponentInChildren<Collider>(true) == null)
        {
            var collider = root.AddComponent<SphereCollider>();
            collider.radius = 0.2f;
        }
    }
}
