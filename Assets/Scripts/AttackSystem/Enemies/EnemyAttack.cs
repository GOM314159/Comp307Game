using UnityEngine;
using UnityEngine.AI;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField]
    private float damage = 10f;

    [SerializeField]
    private float attackRange = 2f;

    [SerializeField]
    private float attackCooldown = 2f;

    [Header("Hardcore")]
    [SerializeField]
    private float hardcoreDamageMultiplier = 1.5f;

    [SerializeField]
    private float hardcoreCooldownMultiplier = 0.75f;

    [SerializeField]
    private bool increaseSpeedOnHardcore = true;

    [SerializeField]
    private float hardcoreSpeedMultiplier = 1.25f;

    [Header("Death Reason")]
    [Tooltip("Death reason recorded if this attack kills the player. Default is Doll for the standard doll/enemy attack.")]
    [SerializeField]
    private GameStateManager.DeathCause deathCause =
        GameStateManager.DeathCause.Doll;

    [Header("Player")]
    [SerializeField]
    private PlayerHealth playerHealth;

    [Header("Movement")]
    [SerializeField]
    private NavMeshAgent navMeshAgent;

    [Header("Debug")]
    [SerializeField]
    private bool debugAttack = true;

    private float attackTimer;

    private float baseDamage;
    private float baseAttackCooldown;
    private float baseSpeed;

    private void Awake()
    {
        baseDamage = damage;
        baseAttackCooldown = attackCooldown;

        if (navMeshAgent == null)
        {
            navMeshAgent =
                GetComponent<NavMeshAgent>();
        }

        if (navMeshAgent != null)
        {
            baseSpeed = navMeshAgent.speed;
        }
    }

    private void Start()
    {
        FindPlayer();
        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        damage = baseDamage;
        attackCooldown = baseAttackCooldown;

        if (navMeshAgent != null)
        {
            navMeshAgent.speed = baseSpeed;
        }

        if (
            GameStateManager.Instance == null ||
            !GameStateManager.Instance.IsHardcore()
        )
        {
            return;
        }

        damage =
            baseDamage *
            hardcoreDamageMultiplier;

        attackCooldown =
            baseAttackCooldown *
            hardcoreCooldownMultiplier;

        if (
            increaseSpeedOnHardcore &&
            navMeshAgent != null
        )
        {
            navMeshAgent.speed =
                baseSpeed *
                hardcoreSpeedMultiplier;
        }
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

        if (distance > attackRange)
        {
            attackTimer = 0f;

            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            Attack();

            attackTimer = 0f;
        }
    }

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

    public void ConfigureAttack(float newDamage, float newRange, float newCooldown)
    {
        damage = Mathf.Max(0f, newDamage); attackRange = Mathf.Max(0.1f, newRange); attackCooldown = Mathf.Max(0.1f, newCooldown);
        baseDamage = damage; baseAttackCooldown = attackCooldown; ApplyDifficulty();
    }

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

        Vector3 direction =
            playerHealth.transform.position
            - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        playerHealth.TakeDamage(
            damage,
            deathCause
        );

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