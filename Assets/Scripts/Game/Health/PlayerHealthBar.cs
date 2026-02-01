using QFramework;
using UnityEngine;

public class PlayerHealthBar : MonoBehaviour, IController
{
    [SerializeField] private HealthBarView barView;
    [SerializeField] private bool hideWhenDead;

    private IUnRegister healthChangedUnregister;
    private IUnRegister healthDeathUnregister;

    private void Awake()
    {
        if (barView == null)
        {
            barView = GetComponent<HealthBarView>();
        }
    }

    private void OnEnable()
    {
        healthChangedUnregister = this.RegisterEvent<EventPlayerHealthChanged>(OnHealthChanged);
        healthDeathUnregister = this.RegisterEvent<EventPlayerDeath>(OnPlayerDeath);
        RefreshFromSystem();
    }

    private void OnDisable()
    {
        healthChangedUnregister?.UnRegister();
        healthChangedUnregister = null;

        healthDeathUnregister?.UnRegister();
        healthDeathUnregister = null;
    }

    private void RefreshFromSystem()
    {
        if (barView == null)
        {
            return;
        }

        var health = this.GetSystem<HealthSystem>()?.GetPlayerHealth();
        if (health == null)
        {
            return;
        }

        barView.SetValue(health.CurrentHealth, health.MaxHealth);
    }

    private void OnHealthChanged(EventPlayerHealthChanged e)
    {
        if (barView == null)
        {
            return;
        }

        barView.SetValue(e.Current, e.Max);
    }

    private void OnPlayerDeath(EventPlayerDeath e)
    {
        if (barView == null)
        {
            return;
        }

        var max = e.Health != null ? e.Health.MaxHealth : 1f;
        barView.SetValue(0f, max);

        if (hideWhenDead)
        {
            gameObject.SetActive(false);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
