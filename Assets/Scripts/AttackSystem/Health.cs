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

    private bool isDead;


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        currentHealth = maxHealth;
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
        // Actual death behaviour will be added later.
        // Play death animation here.
        Destroy( gameObject );
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    } 

    public bool IsDead()
    {
        return isDead;
    }

}