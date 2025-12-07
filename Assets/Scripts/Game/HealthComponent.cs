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

    public float MaxHealth => maxHealth;

    public float CurrentHealth => currentHealth;

    public bool IsDead { get; private set; }

    public event Action<HealthComponent> OnDeath;

    private void Awake()
    {
        currentHealth = Mathf.Max(0f, maxHealth);
    }

    public void ApplyDamage(float amount)
    {
        if (IsDead)
        {
            return;
        }

        float clampedDamage = Mathf.Max(0f, amount);
        currentHealth = Mathf.Max(0f, currentHealth - clampedDamage);

        if (currentHealth <= 0f)
        {
            HandleDeath();
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
        Destroy(gameObject,1f);
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
}
