using QFramework;
using UnityEngine;

public class WorldItemInteractable : MonoBehaviour, IInteractable, IController, ICanSendCommand
{
    public SOItemDefinition Definition;
    public int Count = 1;
    public string PromptOverride;

    public bool CanInteract(InteractContext ctx)
    {
        return Definition != null;
    }

    public InteractInfo GetInfo(InteractContext ctx)
    {
        var name = Definition != null ? Definition.Name : "Item";
        var prompt = string.IsNullOrEmpty(PromptOverride) ? $"Pick up {name}" : PromptOverride;
        return new InteractInfo
        {
            Prompt = prompt,
            CanInteract = Definition != null,
            Icon = Definition != null ? Definition.icon : null
        };
    }

    public void Interact(InteractContext ctx)
    {
        if (Definition == null) return;
        var amount = Mathf.Max(1, Count);
        this.SendCommand(new CmdPickupItem(Definition, amount, gameObject));
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
