using QFramework;
using UnityEngine;



public struct EventInteractTargetChanged
{
    public IInteractable Target;
    public InteractInfo Info;
}

public class InteractionSystem : AbstractSystem, IUpdateSystem, ICanSendEvent
{
    private InputSys _inputSys;
    private IGameLoop _updateScheduler;
    private InteractorView _interactor;

    private IInteractable _currentTarget;
    private InteractInfo _currentInfo;

    protected override void OnInit()
    {
        _inputSys = this.GetSystem<InputSys>();
        _updateScheduler = this.GetUtility<IGameLoop>();
        _updateScheduler.Register(this);
    }

    public void SetInteractor(InteractorView interactor)
    {
        _interactor = interactor;
        ClearTarget();
    }

    public void ClearInteractor(InteractorView interactor)
    {
        if (_interactor == interactor)
        {
            _interactor = null;
            ClearTarget();
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (_interactor == null)
        {
            return;
        }

        if (!TryGetTarget(out var target, out var ctx, out var info))
        {
            if (_currentTarget != null)
            {
                ClearTarget();
            }
            return;
        }

        if (target != _currentTarget || !IsSameInfo(info, _currentInfo))
        {
            _currentTarget = target;
            _currentInfo = info;
            this.SendEvent(new EventInteractTargetChanged
            {
                Target = _currentTarget,
                Info = _currentInfo
            });
        }

        if (_inputSys != null && _inputSys.InteractPressed)
        {
            if (target != null && target.CanInteract(ctx))
            {
                target.Interact(ctx);
                var refreshed = target.GetInfo(ctx);
                if (!IsSameInfo(refreshed, _currentInfo))
                {
                    _currentInfo = refreshed;
                    this.SendEvent(new EventInteractTargetChanged
                    {
                        Target = _currentTarget,
                        Info = _currentInfo
                    });
                }
            }
        }
    }

    private bool TryGetTarget(out IInteractable target, out InteractContext ctx, out InteractInfo info)
    {
        target = null;
        ctx = default;
        info = default;

        var ray = _interactor.GetRay();
        if (!Physics.Raycast(ray, out var hit, _interactor.Range, _interactor.InteractableLayers, _interactor.TriggerInteraction))
        {
            return false;
        }

        target = hit.collider.GetComponentInParent<IInteractable>();
        if (target == null)
        {
            return false;
        }

        var origin = _interactor.GetOriginTransform();
        ctx = new InteractContext
        {
            Interactor = _interactor.gameObject,
            InteractorTransform = origin,
            Camera = _interactor.ViewCamera,
            HitPoint = hit.point,
            Distance = hit.distance
        };

        info = target.GetInfo(ctx);
        if (info.Prompt == null)
        {
            info.Prompt = string.Empty;
        }
        return true;
    }

    private void ClearTarget()
    {
        _currentTarget = null;
        _currentInfo = default;
        this.SendEvent(new EventInteractTargetChanged
        {
            Target = null,
            Info = default
        });
    }

    private bool IsSameInfo(InteractInfo a, InteractInfo b)
    {
        return a.CanInteract == b.CanInteract && a.Icon == b.Icon && a.Prompt == b.Prompt;
    }
}
