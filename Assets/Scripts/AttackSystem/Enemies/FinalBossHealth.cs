using System;
using UnityEngine;

/// <summary>
/// Gargamel's custom three-phase health system.
/// This replaces the normal Health component on the final boss.
/// GargamelDeathScript listens to OnDied and owns Gargamel's actual death/cutscene.
/// </summary>
public class FinalBossHealth : MonoBehaviour, IDamageable
{
    [Header("Phase Health")]
    [SerializeField] private float phase1MaxHealth = 100f;
    [SerializeField] private float phase2MaxHealth = 150f;
    [SerializeField] private float phase3MaxHealth = 200f;

    [Header("Hardcore Health")]
    [SerializeField] private float hardcoreHealthMultiplier = 1.5f;

    [Header("Phase Damage")]
    [SerializeField] private float phase1Damage = 10f;
    [SerializeField] private float phase2Damage = 20f;
    [SerializeField] private float phase3Damage = 30f;

    [Header("Phase Dialogue")]
    [SerializeField] private AudioClip phase2Dialogue;
    [SerializeField] private AudioClip phase3Dialogue;

    [Header("Final Death")]
    [Tooltip("Leave disabled. GargamelDeathScript handles the death sequence.")]
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Debug")]
    [SerializeField] private bool debugHealth = true;

    private int currentPhase = 1;
    private float currentHealth;
    private bool isDead;

    public event Action<int> OnPhaseChanged;
    public event Action OnDied;

    private void Awake()
    {
        ResetBossHealth();
    }

    public bool TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
        {
            return false;
        }

        currentHealth -= damage;

        DebugHealth(
            gameObject.name +
            " Phase " + currentPhase +
            " took " + damage +
            " damage. Remaining phase health: " + currentHealth
        );

        if (currentHealth > 0f)
        {
            return false;
        }

        currentHealth = 0f;

        if (currentPhase < 3)
        {
            AdvancePhase();
            return false;
        }

        Die();
        return true;
    }

    private void AdvancePhase()
    {
        currentPhase++;
        currentHealth = GetPhaseMaxHealth(currentPhase);

        PlayPhaseDialogue(currentPhase);
        OnPhaseChanged?.Invoke(currentPhase);

        DebugHealth(
            "Advanced to Phase " + currentPhase +
            ". Health: " + currentHealth +
            ". Damage: " + GetCurrentDamage()
        );
    }

    private void PlayPhaseDialogue(int phase)
    {
        AudioClip dialogue = null;

        if (phase == 2)
        {
            dialogue = phase2Dialogue;
        }
        else if (phase == 3)
        {
            dialogue = phase3Dialogue;
        }

        if (dialogue == null)
        {
            Debug.LogWarning(
                "[FinalBossHealth] No dialogue assigned for Phase " + phase + "."
            );
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(dialogue);
        }
        else
        {
            Debug.LogWarning(
                "[FinalBossHealth] AudioManager is missing."
            );
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        DebugHealth("Gargamel final boss defeated.");

        // GargamelDeathScript subscribes to this event and owns the
        // normal/secret ending video and final completion flow.
        OnDied?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void ResetBossHealth()
    {
        currentPhase = 1;
        currentHealth = GetPhaseMaxHealth(1);
        isDead = false;

        DebugHealth(
            "Reset to Phase 1 with " + currentHealth + " health."
        );
    }

    public int GetCurrentPhase()
    {
        return currentPhase;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetCurrentMaxHealth()
    {
        return GetPhaseMaxHealth(currentPhase);
    }

    public float GetCurrentDamage()
    {
        switch (currentPhase)
        {
            case 1:
                return phase1Damage;
            case 2:
                return phase2Damage;
            case 3:
            default:
                return phase3Damage;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    private float GetPhaseMaxHealth(int phase)
    {
        float baseHealth;

        switch (phase)
        {
            case 1:
                baseHealth = phase1MaxHealth;
                break;
            case 2:
                baseHealth = phase2MaxHealth;
                break;
            case 3:
            default:
                baseHealth = phase3MaxHealth;
                break;
        }

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore())
        {
            return baseHealth * hardcoreHealthMultiplier;
        }

        return baseHealth;
    }

    private void DebugHealth(string message)
    {
        if (!debugHealth)
        {
            return;
        }

        Debug.Log("[FinalBossHealth] " + message, this);
    }
}
