using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private float currentHealth;

    [Header("Hardcore")]
    [SerializeField]
    private float hardcoreHealthMultiplier = 2f;

    [Header("Death")]
    [SerializeField]
    private bool destroyOnDeath = false;

    private bool isDead;
    private float baseMaxHealth;

    public event Action OnDied;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        ApplyDifficulty();

        currentHealth = maxHealth;
        isDead = false;
    }

    private void Start()
    {
        ApplyDifficulty();

        if (!isDead)
        {
            currentHealth = maxHealth;
        }
    }

    private void ApplyDifficulty()
    {
        maxHealth = baseMaxHealth;

        if (
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore()
        )
        {
            maxHealth =
                baseMaxHealth *
                hardcoreHealthMultiplier;
        }
    }

    public bool TakeDamage(float damage)
    {
        if (isDead)
        {
            return false;
        }

        if (damage <= 0f)
        {
            return false;
        }

        currentHealth -= damage;

        SuccessfulHitRedFlash hitFlash =
            GetComponent<SuccessfulHitRedFlash>();
        if (hitFlash == null)
            hitFlash = GetComponentInChildren<SuccessfulHitRedFlash>(true);
        if (hitFlash != null)
            hitFlash.HighlightSuccessfulHit();

        Debug.Log(
            "[Health] "
            + gameObject.name
            + " took "
            + damage
            + " damage. Remaining health: "
            + currentHealth
        );

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;

            Die();

            return true;
        }

        return false;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        Debug.Log(
            "[Health] "
            + gameObject.name
            + " died."
        );

        OnDied?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void ResetHealth()
    {
        ApplyDifficulty();

        currentHealth = maxHealth;
        isDead = false;

        Debug.Log(
            "[Health] "
            + gameObject.name
            + " health reset to "
            + maxHealth
        );
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}