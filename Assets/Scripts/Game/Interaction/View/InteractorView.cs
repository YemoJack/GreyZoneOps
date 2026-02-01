using QFramework;
using UnityEngine;

public class InteractorView : MonoBehaviour, IController
{
    public Camera ViewCamera;
    public Transform RayOrigin;
    public float Range = 2.5f;
    public LayerMask InteractableLayers = ~0;
    //默认我设置为 Collide，这样如果你的可交互物体是 Trigger（比如只有触发体的拾取物），依然可以被射线检测到。你如果只想命中实体碰撞体，就改成 Ignore
    public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Collide;

    private InteractionSystem _system;

    private void Awake()
    {
        if (ViewCamera == null)
        {
            ViewCamera = Camera.main;
        }

        ApplyGameConfig();
        this.RegisterEvent<EventPlayerSpawned>(OnPlayerSpawned).UnRegisterWhenGameObjectDestroyed(this);
    }

    private void ApplyGameConfig()
    {
        var settings = GameSettingManager.Instance;
        if (settings == null || settings.Config == null)
        {
            return;
        }

        Range = settings.Config.InteractRange;
        InteractableLayers = settings.Config.InteractableLayers;
        TriggerInteraction = settings.Config.InteractTriggerInteraction;
    }

    private void OnPlayerSpawned(EventPlayerSpawned e)
    {
        if (!IsTargetPlayer(e.PlayerTransform))
        {
            return;
        }

        _system = this.GetSystem<InteractionSystem>();
        _system?.SetInteractor(this);
    }


    private void OnDisable()
    {
        _system?.ClearInteractor(this);
        _system = null;
    }

    public Ray GetRay()
    {
        var origin = GetOriginTransform();
        return new Ray(origin.position, origin.forward);
    }

    public Transform GetOriginTransform()
    {
        if (RayOrigin != null) return RayOrigin;
        if (ViewCamera != null) return ViewCamera.transform;
        return transform;
    }

    private bool IsTargetPlayer(Transform playerRoot)
    {
        if (playerRoot == null)
        {
            return false;
        }

        return playerRoot == transform || playerRoot == transform.root;
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
