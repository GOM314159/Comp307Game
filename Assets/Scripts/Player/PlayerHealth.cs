using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Player Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("References")]
    [SerializeField] private PlayerStateManager playerStateManager;

    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;

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
        {
            return;
        }

        isDead = true;

        Debug.Log("[PlayerHealth] Player died.");

        if (playerStateManager != null)
        {
            playerStateManager.SetRIP();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.HandlePlayerDeath();
        }
        else
        {
            Debug.LogError(
                "[PlayerHealth] GameStateManager.Instance "
                + "is missing."
            );
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        if (playerStateManager != null)
        {
            playerStateManager.ResetToDefault();
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