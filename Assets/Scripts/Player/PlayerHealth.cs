using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Player Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("References")]
    [SerializeField] private PlayerStateManager playerStateManager;

    private bool isDead;
    private GameStateManager.DeathCause pendingDeathCause =
        GameStateManager.DeathCause.Other;

    [SerializeField]
    private PlayerMovement playerMovement;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        pendingDeathCause = GameStateManager.DeathCause.Other;

        if (playerStateManager == null)
        {
            playerStateManager =
                GetComponent<PlayerStateManager>();
        }

        if (playerStateManager == null)
        {
            Debug.LogError(
                "[PlayerHealth] PlayerStateManager "
                + "not found on Player."
            );
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    public bool TakeDamage(float damage)
    {
        return TakeDamage(
            damage,
            GameStateManager.DeathCause.Other
        );
    }


    public bool TakeDamage(
        float damage,
        GameStateManager.DeathCause deathCause)
    {
        if (isDead)
        {
            return false;
        }

        if (damage <= 0f)
        {
            return false;
        }

        pendingDeathCause = deathCause;
        currentHealth -= damage;

        Debug.Log(
            "[PlayerHealth] Player took "
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
            return;

        isDead = true;

        Debug.Log("[PlayerHealth] Player died.");

        // Play one of the three death animations
        if (playerMovement != null)
        {
            playerMovement.PlayDeathAnimation();
        }

        // Preserve the existing player death state
        if (playerStateManager != null)
        {
            playerStateManager.ClearNoAttackRestriction();
            playerStateManager.SetRIP();
        }

        // Preserve the existing GameStateManager flow
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.HandlePlayerDeath(
                pendingDeathCause
            );
        }
        else
        {
            Debug.LogError(
                "[PlayerHealth] GameStateManager.Instance is missing."
            );
        }
    }


    /// <summary>Restores health without exceeding maxHealth.</summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log("[PlayerHealth] Healed " + (currentHealth - previousHealth) +
                  " health. Current health: " + currentHealth + "/" + maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        pendingDeathCause = GameStateManager.DeathCause.Other;

        if (playerStateManager != null)
        {
            playerStateManager.ResetToDefault();
        }

        if (playerMovement != null)
        {
            playerMovement.ResetDeathAnimation();
        }

        Debug.Log("[PlayerHealth] Player health reset.");
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
