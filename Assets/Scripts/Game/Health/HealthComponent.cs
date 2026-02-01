using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private float currentHealth;

    [SerializeField]
    private ParticleSystem deathEffect;

    [SerializeField]
    private Collider[] collidersToDisable;

    [SerializeField]
    private bool destroyOnDeath = true;

    [SerializeField]
    private float destroyDelay = 1f;

    public float MaxHealth => maxHealth;

    public float CurrentHealth => currentHealth;

    public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

    public bool IsDead { get; private set; }

    public bool DestroyOnDeath
    {
        get => destroyOnDeath;
        set => destroyOnDeath = value;
    }

    public event Action<HealthComponent> OnHealthChanged;

    public event Action<HealthComponent> OnDeath;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    public void ApplyDamage(float amount)
    {
        if (IsDead)
        {
            return;
        }

        float clampedDamage = Mathf.Max(0f, amount);
        if (clampedDamage <= 0f)
        {
            return;
        }
        currentHealth = Mathf.Max(0f, currentHealth - clampedDamage);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead)
        {
            return;
        }

        float clampedHeal = Mathf.Max(0f, amount);
        if (clampedHeal <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + clampedHeal);
        NotifyHealthChanged();
    }

    public void SetMaxHealth(float value, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, value);
        if (refill)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        NotifyHealthChanged();
    }

    public void ResetHealth(bool notify = true)
    {
        if (IsDead)
        {
            EnableColliders();
            StopDeathEffect();
            IsDead = false;
        }

        currentHealth = maxHealth;
        if (notify)
        {
            NotifyHealthChanged();
        }
    }

    private void HandleDeath()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        DisableColliders();
        PlayDeathEffect();

        OnDeath?.Invoke(this);
        if (destroyOnDeath)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    private void DisableColliders()
    {
        if (collidersToDisable == null || collidersToDisable.Length == 0)
        {
            collidersToDisable = GetComponents<Collider>();
        }

        foreach (var col in collidersToDisable)
        {
            if (col)
            {
                col.enabled = false;
            }
        }

        var controller = GetComponent<CharacterController>();
        if (controller)
        {
            controller.enabled = false;
        }
    }

    private void PlayDeathEffect()
    {
        if (deathEffect)
        {
            deathEffect.Play();
        }
    }

    private void StopDeathEffect()
    {
        if (deathEffect)
        {
            deathEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void EnableColliders()
    {
        if (collidersToDisable == null || collidersToDisable.Length == 0)
        {
            collidersToDisable = GetComponents<Collider>();
        }

        foreach (var col in collidersToDisable)
        {
            if (col)
            {
                col.enabled = true;
            }
        }

        var controller = GetComponent<CharacterController>();
        if (controller)
        {
            controller.enabled = true;
        }
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(this);
    }
}
