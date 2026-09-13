using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    // =========================================================
    // HEALTH
    // =========================================================

    [Header("Health")]

    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private float currentHealth;


    // =========================================================
    // DEATH SETTINGS
    // =========================================================

    [Header("Death")]

    [Tooltip(
        "If true, the GameObject is destroyed when health "
        + "reaches zero."
        + "\n\n"
        + "For bosses, leave this FALSE so the reset system "
        + "can restore their health."
    )]
    [SerializeField]
    private bool destroyOnDeath = false;


    // =========================================================
    // STATE
    // =========================================================

    private bool isDead;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnDied;


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
    }


    // =========================================================
    // DAMAGE
    // =========================================================

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


    // =========================================================
    // DEATH
    // =========================================================

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


        // -----------------------------------------------------
        // NOTIFY ALL LISTENERS
        // -----------------------------------------------------

        OnDied?.Invoke();


        // -----------------------------------------------------
        // OPTIONAL DESTRUCTION
        // -----------------------------------------------------

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        Debug.Log(
            "[Health] "
            + gameObject.name
            + " health reset to "
            + maxHealth
        );
    }


    // =========================================================
    // GETTERS
    // =========================================================

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