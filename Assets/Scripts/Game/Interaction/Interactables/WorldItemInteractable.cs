using QFramework;
using UnityEngine;

public class WorldItemInteractable : MonoBehaviour, IInteractable, IController, ICanSendCommand
{
    [HideInInspector]
    public ItemInstance Item;
    public string PromptOverride;

    public bool CanInteract(InteractContext ctx)
    {

        return Item != null && Item.Definition != null;
    }

    public InteractInfo GetInfo(InteractContext ctx)
    {

        var def = Item != null ? Item.Definition : null;
        var name = def != null ? def.Name : "Item";
        var prompt = string.IsNullOrEmpty(PromptOverride) ? $"Pick up {name}" : PromptOverride;
        return new InteractInfo
        {
            Prompt = prompt,
            CanInteract = def != null,
            Icon = def != null ? def.icon : null
        };
    }

    public void Interact(InteractContext ctx)
    {
        if (Item == null || Item.Definition == null || Item.Count <= 0) return;
        this.SendCommand(new CmdPickupItem(Item, gameObject));
    }

    private void Start()
    {
        if (Item == null || Item.Definition == null)
        {
            Debug.LogError("WorldItem is Null");
        }
    }



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
