using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectItemWindowRuntimeBridge : MonoBehaviour
{
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);
    private SelectItemWindow window;
    private Coroutine repositionCoroutine;

    public void Bind(SelectItemWindow targetWindow)
    {
        window = targetWindow;
    }

    public void RequestDeferredReposition()
    {
        if (repositionCoroutine != null)
        {
            StopCoroutine(repositionCoroutine);
        }

        repositionCoroutine = StartCoroutine(RepositionAtEndOfFrame());
    }

    private void LateUpdate()
    {
        if (window == null)
        {
            return;
        }

        if (!window.Visible || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        raycastResults.Clear();
        var pointer = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        eventSystem.RaycastAll(pointer, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            var go = raycastResults[i].gameObject;
            if (go == null)
            {
                continue;
            }

            if (go.GetComponentInParent<InventoryItemView>() != null)
            {
                return;
            }

            if (window.ContainsTarget(go.transform))
            {
                return;
            }
        }

        window.HideWindow();
    }

    private IEnumerator RepositionAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        repositionCoroutine = null;
        if (window == null || !window.Visible)
        {
            yield break;
        }

        window.TryRefreshSelectedItemPosition();
    }
}
