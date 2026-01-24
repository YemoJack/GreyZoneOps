using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    public bool UseAnimator = true;
    public Animator DoorAnimator;
    public string AnimatorParam = "Open";
    public float OpenAngle = 90f;
    public string OpenPrompt = "Open";
    public string ClosePrompt = "Close";

    private bool _isOpen;
    private Quaternion _closedRotation;

    private void Awake()
    {
        if (DoorAnimator == null && UseAnimator)
        {
            DoorAnimator = GetComponentInChildren<Animator>();
        }
        _closedRotation = transform.localRotation;
        ApplyGameConfig();
    }

    private void ApplyGameConfig()
    {
        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(settings.Config.DefaultDoorOpenPrompt))
        {
            OpenPrompt = settings.Config.DefaultDoorOpenPrompt;
        }
        if (!string.IsNullOrEmpty(settings.Config.DefaultDoorClosePrompt))
        {
            ClosePrompt = settings.Config.DefaultDoorClosePrompt;
        }
    }

    public bool CanInteract(InteractContext ctx)
    {
        return true;
    }

    public InteractInfo GetInfo(InteractContext ctx)
    {
        return new InteractInfo
        {
            Prompt = _isOpen ? ClosePrompt : OpenPrompt,
            CanInteract = true,
            Icon = null
        };
    }

    public void Interact(InteractContext ctx)
    {
        _isOpen = !_isOpen;
        if (UseAnimator && DoorAnimator != null)
        {
            DoorAnimator.SetBool(AnimatorParam, _isOpen);
            return;
        }

        var targetRotation = _isOpen
            ? _closedRotation * Quaternion.Euler(0f, OpenAngle, 0f)
            : _closedRotation;
        transform.localRotation = targetRotation;
    }
}
