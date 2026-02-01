using QFramework;
using UnityEngine;

public class HealthSystem : AbstractSystem
{
    private HealthComponent playerHealth;
    private InputSys inputSys;

    protected override void OnInit()
    {
        inputSys = this.GetSystem<InputSys>();
        this.RegisterEvent<EventPlayerSpawned>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(EventPlayerSpawned e)
    {
        if (e.PlayerTransform == null)
        {
            return;
        }

        var health = e.PlayerTransform.GetComponentInChildren<HealthComponent>();
        if (health == null)
        {
            health = e.PlayerTransform.gameObject.AddComponent<HealthComponent>();
        }

        BindPlayerHealth(health);
    }

    public HealthComponent GetPlayerHealth()
    {
        return playerHealth;
    }

    public void ApplyDamage(float amount)
    {
        playerHealth?.ApplyDamage(amount);
    }

    public void Heal(float amount)
    {
        playerHealth?.Heal(amount);
    }

    public void ResetHealth()
    {
        playerHealth?.ResetHealth();
        SetPlayerInputEnabled(true);
    }

    private void BindPlayerHealth(HealthComponent health)
    {
        if (playerHealth == health)
        {
            SendHealthChanged(playerHealth);
            return;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
            playerHealth.OnDeath -= OnPlayerDeath;
        }

        playerHealth = health;

        if (playerHealth != null)
        {
            playerHealth.DestroyOnDeath = false;
            playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            playerHealth.OnDeath += OnPlayerDeath;
            SendHealthChanged(playerHealth);
        }
    }

    private void OnPlayerHealthChanged(HealthComponent health)
    {
        SendHealthChanged(health);
    }

    private void OnPlayerDeath(HealthComponent health)
    {
        SetPlayerInputEnabled(false);
        this.SendEvent(new EventPlayerDeath
        {
            Health = health
        });
    }

    private void SendHealthChanged(HealthComponent health)
    {
        if (health == null)
        {
            return;
        }

        var max = Mathf.Max(1f, health.MaxHealth);
        var current = Mathf.Clamp(health.CurrentHealth, 0f, max);

        this.SendEvent(new EventPlayerHealthChanged
        {
            Health = health,
            Current = current,
            Max = max,
            Normalized = max <= 0f ? 0f : current / max
        });
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        if (inputSys == null)
        {
            inputSys = this.GetSystem<InputSys>();
        }

        inputSys?.SetInputEnabled(enabled);
    }
}
