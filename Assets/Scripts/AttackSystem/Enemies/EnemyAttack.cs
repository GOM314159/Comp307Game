using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // =========================================================
    // ATTACK SETTINGS
    // =========================================================

    [Header("Attack")]

    [Tooltip("Damage dealt to the player per attack.")]
    [SerializeField]
    private float damage = 10f;

    [Tooltip("Maximum distance at which the enemy can attack.")]
    [SerializeField]
    private float attackRange = 2f;

    [Tooltip("Time between attacks.")]
    [SerializeField]
    private float attackCooldown = 2f;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [Tooltip(
        "Leave empty to automatically find the Player "
        + "by tag."
    )]
    [SerializeField]
    private PlayerHealth playerHealth;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugAttack = true;


    // =========================================================
    // STATE
    // =========================================================

    private float attackTimer;


    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        FindPlayer();
    }


    private void Update()
    {
        if (playerHealth == null)
        {
            FindPlayer();

            return;
        }

        if (playerHealth.IsDead())
        {
            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                playerHealth.transform.position
            );


        // -----------------------------------------------------
        // OUTSIDE RANGE
        // -----------------------------------------------------

        if (distance > attackRange)
        {
            attackTimer = 0f;

            return;
        }


        // -----------------------------------------------------
        // ATTACK TIMER
        // -----------------------------------------------------

        attackTimer += Time.deltaTime;


        if (attackTimer >= attackCooldown)
        {
            Attack();

            attackTimer = 0f;
        }
    }


    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayer()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return;
        }

        playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "[EnemyAttack] PlayerHealth not found."
            );
        }
    }


    // =========================================================
    // ATTACK
    // =========================================================

    private void Attack()
    {
        if (playerHealth == null)
        {
            return;
        }

        if (playerHealth.IsDead())
        {
            return;
        }


        // -----------------------------------------------------
        // FACE PLAYER
        // -----------------------------------------------------

        Vector3 direction =
            playerHealth.transform.position
            - transform.position;

        direction.y = 0f;


        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }


        // -----------------------------------------------------
        // DAMAGE
        // -----------------------------------------------------

        playerHealth.TakeDamage(damage);


        if (debugAttack)
        {
            Debug.Log(
                "[EnemyAttack] "
                + gameObject.name
                + " attacked player for "
                + damage
                + " damage."
            );
        }
    }
}