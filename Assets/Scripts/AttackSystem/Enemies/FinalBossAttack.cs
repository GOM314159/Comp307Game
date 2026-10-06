using UnityEngine;

/// <summary>
/// Gargamel's attack. PlayerHealth is discovered at runtime so this component
/// does not keep a scene-local player reference across resets/scene loads.
/// </summary>
public class FinalBossAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FinalBossHealth finalBossHealth;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Hardcore")]
    [SerializeField] private float hardcoreCooldownMultiplier = 0.75f;

    [Header("Debug")]
    [SerializeField] private bool debugAttack = true;

    private PlayerHealth playerHealth;
    private float attackTimer;
    private float effectiveAttackCooldown;

    private void Awake()
    {
        if (finalBossHealth == null)
        {
            finalBossHealth = GetComponent<FinalBossHealth>();
        }

        ApplyDifficulty();
    }

    private void Start()
    {
        FindPlayerHealth();
        ApplyDifficulty();
    }

    private void Update()
    {
        if (finalBossHealth == null || finalBossHealth.IsDead())
        {
            return;
        }

        if (playerHealth == null || playerHealth.IsDead())
        {
            FindPlayerHealth();

            if (playerHealth == null || playerHealth.IsDead())
            {
                return;
            }
        }

        float distance = Vector3.Distance(
            transform.position,
            playerHealth.transform.position
        );

        if (distance > attackRange)
        {
            attackTimer = 0f;
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= effectiveAttackCooldown)
        {
            Attack();
            attackTimer = 0f;
        }
    }

    private void FindPlayerHealth()
    {
        // FindAnyObjectByType avoids a serialized player reference that can
        // become invalid when the player is recreated by a scene reset.
        playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            PlayerHealth[] players = FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            if (players.Length > 0)
            {
                playerHealth = players[0];
            }
        }
    }

    private void ApplyDifficulty()
    {
        effectiveAttackCooldown = attackCooldown;

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore())
        {
            effectiveAttackCooldown =
                attackCooldown * hardcoreCooldownMultiplier;
        }
    }

    private void Attack()
    {
        if (finalBossHealth == null ||
            finalBossHealth.IsDead() ||
            playerHealth == null ||
            playerHealth.IsDead())
        {
            return;
        }

        Vector3 direction =
            playerHealth.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        float damage = finalBossHealth.GetCurrentDamage();
        playerHealth.TakeDamage(damage);

        if (debugAttack)
        {
            Debug.Log(
                "[FinalBossAttack] Phase " +
                finalBossHealth.GetCurrentPhase() +
                " attacked player for " +
                damage + " damage."
            );
        }
    }
}
