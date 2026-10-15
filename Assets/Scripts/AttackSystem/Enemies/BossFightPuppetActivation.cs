using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps a theater puppet harmless until the boss fight has been running for a
/// while, and shows its mini health bar the moment the boss fight starts.
/// </summary>
public class BossFightPuppetActivation : MonoBehaviour
{
    [SerializeField, Min(0)] private float delayAfterBossFightStarts = 10f;
    [SerializeField] private Health puppetHealth;
    [SerializeField] private EnemyAttack puppetAttack;
    [SerializeField] private float attackDamage = 10;
    [SerializeField] private float attackRange = 2;
    [SerializeField] private float attackCooldown = 2;
    [SerializeField] private bool activateOnlyOnce = true;

    private bool activated;
    private Coroutine routine;
    private bool barsShownThisFight;
    [SerializeField] private bool debugActivation = true;

    private void Awake()
    {
        if (puppetHealth == null) puppetHealth = GetComponent<Health>();
        if (puppetAttack == null) puppetAttack = GetComponent<EnemyAttack>();
        if (puppetHealth != null) puppetHealth.enabled = false;
        if (puppetAttack != null) puppetAttack.enabled = false;
    }

    private void Update()
    {
        bool bossFight =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.GetGameMode() == GameStateManager.GameMode.BossFight;

        // Mini health bars: show them as soon as the boss fight begins, even
        // though the puppet itself is still waiting for its activation delay.
        if (bossFight && !barsShownThisFight)
        {
            ShowHealthBars();
            barsShownThisFight = true;
            if (debugActivation)
            {
                Debug.Log(
                    "[BossFightPuppetActivation] Boss fight started; " +
                    "showing health bars for " + name + ".",
                    this
                );
            }
        }
        else if (!bossFight)
        {
            barsShownThisFight = false;
        }

        if (activated && activateOnlyOnce) return;

        if (routine == null && bossFight)
            routine = StartCoroutine(Activate());
    }

    public void ShowHealthBarsNow()
    {
        ShowHealthBars();
        barsShownThisFight = true;
    }

    private void ShowHealthBars()
    {
        EnemyHealthBar[] bars = FindObjectsByType<EnemyHealthBar>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int matchedBars = 0;
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] != null && bars[i].BelongsTo(puppetHealth, transform))
            {
                bars[i].ShowForBossFight();
                matchedBars++;
                if (debugActivation)
                {
                    Debug.Log(
                        "[BossFightPuppetActivation] Requested mini health bar " +
                        "visible: " + bars[i].name + ".",
                        bars[i]
                    );
                }
            }
        }

        if (debugActivation && matchedBars == 0)
        {
            Debug.LogWarning(
                "[BossFightPuppetActivation] No EnemyHealthBar found for " +
                name + ". Check the Health reference and slider canvas parent.",
                this
            );
        }
    }

    private IEnumerator Activate()
    {
        yield return new WaitForSeconds(delayAfterBossFightStarts);

        if (puppetHealth != null)
        {
            puppetHealth.enabled = true;
            puppetHealth.ResetHealth();
        }

        if (puppetAttack != null)
        {
            puppetAttack.ConfigureAttack(attackDamage, attackRange, attackCooldown);
            puppetAttack.enabled = true;
        }

        activated = true;
        routine = null;
        if (debugActivation)
        {
            Debug.Log(
                "[BossFightPuppetActivation] Puppet activated: " + name + ".",
                this
            );
        }
    }

    public void ResetPuppetForNextPlay()
    {
        activated = false;
        barsShownThisFight = false;

        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (puppetHealth != null) puppetHealth.enabled = false;
        if (puppetAttack != null) puppetAttack.enabled = false;
    }
}
