using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // needed for NavMeshAgent
using EnemyAI_BT;      // our behaviour tree framework from BehaviourTree.cs

/// <summary>
/// ==========================================================================
///  ENEMY AI — "Girl's Room" Enemy
/// ==========================================================================
/// Behaviour summary (matches your design):
///   1. WAIT  -> Enemy sits on the bed upstairs, doing nothing, until the
///               player has picked up the axe downstairs. - implement head
///   2. CHASE -> Once the player has the axe AND comes upstairs near the
///               enemy, the enemy gets up off the bed and starts chasing.
///   3. ATTACK-> If the enemy catches the player, it attacks.
///   4. DIE   -> If the player hits the enemy enough times with the axe
///               before the level timer runs out, the enemy dies and the
///               player wins. - Run away if health is too low
///
/// This script BUILDS a behaviour tree out of the generic Selector /
/// Sequence / ConditionNode / ActionNode pieces defined in
/// BehaviourTree.cs, then evaluates that tree once every frame in Update().
/// ==========================================================================
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Player's Transform here.")]
    [SerializeField] private Transform player;

    [Tooltip("The exact spot on the bed the enemy sits/lies at while waiting.")]
    [SerializeField] private Transform bedPosition;

    [Tooltip("Optional: reference to an Animator for idle/chase/attack/die animations.")]
    [SerializeField] private Animator animator;

    [Header("Chase / Attack Settings")]
    [Tooltip("How close the player must be (while holding the axe) before the enemy wakes up and gives chase.")]
    [SerializeField] private float detectionRange = 8f;

    [Tooltip("How close the enemy must be to the player to actually attack.")]
    [SerializeField] private float attackRange = 1.5f;

    [Tooltip("Movement speed while chasing the player.")]
    [SerializeField] private float chaseSpeed = 4f;

    [Tooltip("Time (seconds) the enemy must wait between attacks.")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3; // e.g. 3 axe hits to kill
    private int currentHealth;

    // ----- Internal state flags -----
    // These are the "memory" the behaviour tree conditions read from.
    // They get changed by outside scripts (the axe pickup script, the
    // axe-swing/hit script, etc.) calling the public methods further down.
    private bool playerHasAxe = false;   // set true when player picks up the axe
    private bool hasLeftBed = false;     // becomes true the first time enemy stands up
    private bool isDead = false;         // becomes true once health reaches 0
    private float lastAttackTime = -999f;

    private NavMeshAgent agent;
    private BTNode behaviourTreeRoot; // the root node of our tree — evaluated every frame

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Enemy starts sitting on the bed and shouldn't move until triggered.
        agent.isStopped = true;
        agent.speed = chaseSpeed;

        // Build the behaviour tree ONCE at the start.
        // We store it as a field so Update() can just call Evaluate() on it
        // every frame without rebuilding it each time.
        behaviourTreeRoot = BuildBehaviourTree();
    }

    private void Update()
    {
        // A dead enemy doesn't need to think anymore.
        if (isDead) return;

        // This single line drives the ENTIRE enemy AI for this frame.
        // The Selector inside decides, in priority order, what the enemy
        // should be doing right now (die > attack > chase > idle-on-bed).
        behaviourTreeRoot.Evaluate();
    }

    /// <summary>
    /// Builds the actual tree structure using the generic node classes.
    /// Read this top-down — it reads almost like plain English:
    ///
    /// Selector (try each branch in priority order, top = highest priority):
    ///   1. Sequence: [IsDead?]                  -> Die()
    ///   2. Sequence: [PlayerInAttackRange?]      -> AttackPlayer()
    ///   3. Sequence: [PlayerHasAxeAndDetected?]  -> ChasePlayer()
    ///   4. Sequence: [otherwise]                 -> IdleOnBed()
    /// </summary>
    private BTNode BuildBehaviourTree()
    {
        // --- Branch 1: Dying takes priority over everything else ---
        BTNode dieBranch = new Sequence(new List<BTNode>
        {
            new ConditionNode(() => isDead),
            new ActionNode(Die)
        });

        // --- Branch 2: If the player is within attack range, attack ---
        BTNode attackBranch = new Sequence(new List<BTNode>
        {
            new ConditionNode(() => hasLeftBed && IsPlayerInRange(attackRange)),
            new ActionNode(AttackPlayer)
        });

        // --- Branch 3: If the player has the axe and is close enough, chase ---
        BTNode chaseBranch = new Sequence(new List<BTNode>
        {
            new ConditionNode(ShouldChasePlayer),
            new ActionNode(ChasePlayer)
        });

        // --- Branch 4: Default fallback — just wait on the bed ---
        BTNode idleBranch = new ActionNode(IdleOnBed);

        // The Selector tries these top-to-bottom every frame and runs
        // whichever one is currently valid.
        return new Selector(new List<BTNode>
        {
            dieBranch,
            attackBranch,
            chaseBranch,
            idleBranch
        });
    }

    // ======================================================================
    //  CONDITION HELPERS
    //  These are plain functions that just return true/false — they get
    //  wrapped in ConditionNode(...) up above.
    // ======================================================================

    /// <summary>
    /// Decides whether the enemy should start (or keep) chasing:
    /// the player must actually have picked up the axe, AND either the
    /// enemy has already left the bed (already chasing), or the player has
    /// come close enough upstairs to "wake" the enemy for the first time.
    /// </summary>
    private bool ShouldChasePlayer()
    {
        if (!playerHasAxe) return false;

        // Already up and chasing -> keep chasing regardless of distance,
        // so the enemy doesn't awkwardly stop mid-chase.
        if (hasLeftBed) return true;

        // Not up yet -> only leave the bed once the armed player gets close.
        return IsPlayerInRange(detectionRange);
    }

    private bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= range;
    }

    // ======================================================================
    //  ACTION NODES
    //  Each of these matches one leaf in the tree and returns a NodeState.
    // ======================================================================

    /// <summary>
    /// Enemy just sits/lies on the bed, doing nothing, waiting for the
    /// player to become a threat (i.e. grab the axe and get close).
    /// Returns Running because this is an ongoing, non-finishing behaviour.
    /// </summary>
    private NodeState IdleOnBed()
    {
        // Make sure the enemy is actually parked at the bed position
        // and not drifting (in case it was nudged by physics, etc).
        if (!agent.isStopped)
        {
            agent.isStopped = true;
        }

        animator?.SetBool("IsChasing", false);
        animator?.SetTrigger("Idle");

        return NodeState.Running;
    }

    /// <summary>
    /// The enemy gets up (first time only) and chases the player using
    /// the NavMeshAgent. Returns Running while chasing, since "chasing"
    /// doesn't really have a natural finish point — it only stops when
    /// the tree switches to the Attack or Die branch instead.
    /// </summary>
    private NodeState ChasePlayer()
    {
        // First-time trigger: the enemy "wakes up" off the bed.
        if (!hasLeftBed)
        {
            hasLeftBed = true;
            animator?.SetTrigger("StandUp"); // e.g. get-off-bed animation
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);
        animator?.SetBool("IsChasing", true);

        return NodeState.Running;
    }

    /// <summary>
    /// Enemy attacks the player. Uses a cooldown so it doesn't attack
    /// every single frame while standing next to the player.
    /// </summary>
    private NodeState AttackPlayer()
    {
        // Stop moving while attacking so it doesn't slide into the player.
        agent.isStopped = true;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            animator?.SetTrigger("Attack");

            // TODO: hook this up to your player health / damage script, e.g:
            // player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
        }

        return NodeState.Running;
    }

    /// <summary>
    /// Plays the death behaviour once and disables the enemy.
    /// This only ever needs to fully "complete" once, so after it runs
    /// we just return Success — although since isDead never goes back to
    /// false, the dieBranch will keep matching first every frame anyway
    /// (which is fine, it just no-ops safely).
    /// </summary>
    private NodeState Die()
    {
        agent.isStopped = true;
        agent.enabled = false;
        animator?.SetTrigger("Die");

        // TODO: notify your GameManager that the player has won, e.g:
        // GameManager.Instance.OnEnemyDefeated();

        // Disable this script's "thinking" — Update() also early-returns
        // when isDead is true, so this is just extra safety.
        enabled = false;

        return NodeState.Success;
    }

    // ======================================================================
    //  PUBLIC HOOKS — call these from OTHER scripts
    //  (These are the "wires" connecting this enemy to the rest of your game.)
    // ======================================================================

    /// <summary>
    /// Call this from your Axe pickup script the moment the player grabs
    /// the axe downstairs, e.g.:
    ///     enemy.OnPlayerGrabbedAxe();
    /// From this point on, the behaviour tree's chase condition becomes
    /// possible to satisfy.
    /// </summary>
    public void OnPlayerGrabbedAxe()
    {
        playerHasAxe = true;
    }

    /// <summary>
    /// Call this from your Axe swing/hit-detection script whenever the
    /// axe successfully hits the enemy, e.g.:
    ///     enemy.TakeDamage(1);
    /// Once currentHealth hits 0, the tree's Die branch takes over next frame.
    /// </summary>
    /// <param name="damage">How much health to remove.</param>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            isDead = true;
        }
    }

    // ======================================================================
    //  DESIGN NOTE ABOUT THE LEVEL TIMER
    // ======================================================================
    // You mentioned the player can only win if they defeat the enemy ON
    // TIME because of a level timer. That win/lose "clock check" is
    // deliberately NOT handled inside this enemy script — it belongs to
    // whatever script is tracking the timer (e.g. a LevelTimer / GameManager).
    //
    // Recommended pattern:
    //   - LevelTimer counts down independently.
    //   - When it hits 0, LevelTimer calls something like
    //     GameManager.Instance.OnTimeUp() -> triggers a "player loses" state,
    //     REGARDLESS of what this enemy's behaviour tree is doing.
    //   - When Die() above runs (enemy health <= 0), THIS script calls
    //     GameManager.Instance.OnEnemyDefeated() -> which should check
    //     "was there still time left?" before declaring a win.
    //
    // Keeping the timer logic out of the enemy AI keeps this script focused
    // on just "how the enemy behaves," which is exactly what a behaviour
    // tree is meant to describe.
    // ======================================================================

    // Optional: visualize detection/attack ranges in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
