public interface IInteractable
{
    bool CanInteract(InteractContext ctx);
    InteractInfo GetInfo(InteractContext ctx);
    void Interact(InteractContext ctx);
}
