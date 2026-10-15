using System;
using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip(
        "AudioSource used for phase dialogue. Leave empty to find/create one " +
        "on this object. AudioManager.PlayVoice is also used when available."
    )]
    [SerializeField] private AudioSource dialogueAudioSource;

    [SerializeField] [Range(0f, 1f)] private float dialogueVolume = 1f;

    [Header("Hitbox")]
    [Tooltip(
        "If Gargamel has no collider, create a BoxCollider so player " +
        "attack raycasts on the Damage layer can land hits."
    )]
    [SerializeField] private bool autoCreateHitboxIfMissing = true;

    [SerializeField] private Vector3 hitboxCenter = new Vector3(0f, 1.0f, 0f);
    [SerializeField] private Vector3 hitboxSize = new Vector3(1.2f, 2.2f, 1.2f);

    [Header("Final Death")]
    [Tooltip("Leave disabled. GargamelDeathScript handles the death sequence.")]
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Debug")]
    [SerializeField] private bool debugHealth = true;

    private int currentPhase = 1;
    private float currentHealth;
    private bool isDead;
    [Header("Phase Health Bar UI")]
    [Tooltip("Optional direct references. If empty, the script searches loaded scenes for HealthBar_phase1, HealthBar_phase2 and HealthBar_phase3.")]
    [SerializeField] private GameObject phase1HealthBar;
    [SerializeField] private GameObject phase2HealthBar;
    [SerializeField] private GameObject phase3HealthBar;

    private GameObject[] phaseBarObjects = new GameObject[3];
    private Slider[] phaseBarSliders = new Slider[3];
    private Image[] phaseBarFillImages = new Image[3];
    private float nextPhaseBarResolveTime;
    private int lastLoggedVisiblePhase;
    private bool phaseBarBindingsLogged;
    private bool phaseBarsAllowed = true;

    public event Action<int> OnPhaseChanged;
    public event Action OnDied;

    private void Awake()
    {
        EnsureDialogueAudioSource();
        EnsureDamageHitbox();
        ResetBossHealth();
        ResolvePhaseHealthBars();
    }

    private void OnEnable()
    {
        EnsureDialogueAudioSource();
        EnsureDamageHitbox();
        ResolvePhaseHealthBars();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextPhaseBarResolveTime &&
            (phaseBarObjects[0] == null || phaseBarObjects[1] == null || phaseBarObjects[2] == null))
        {
            nextPhaseBarResolveTime = Time.unscaledTime + 0.5f;
            ResolvePhaseHealthBars();
        }
        RefreshPhaseHealthBars();
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
            RefreshPhaseHealthBars();
            return false;
        }

        currentHealth = 0f;
        RefreshPhaseHealthBars();

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
        RefreshPhaseHealthBars();

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
                "[FinalBossHealth] No dialogue AudioClip assigned for Phase " +
                phase +
                ". Assign Phase 2/3 Dialogue on Gargamel's FinalBossHealth.",
                this
            );
            return;
        }

        EnsureDialogueAudioSource();

        bool played = false;

        // Prefer the local source so dialogue is heard even if AudioManager
        // voice volume/routing is muted or not ready.
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = dialogue;
            dialogueAudioSource.volume = dialogueVolume;
            dialogueAudioSource.Play();
            played = true;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(dialogue);
            played = true;
        }

        if (!played)
        {
            AudioSource.PlayClipAtPoint(
                dialogue,
                transform.position,
                dialogueVolume
            );
        }

        DebugHealth(
            "Playing Phase " + phase + " dialogue: " + dialogue.name
        );
    }

    private void EnsureDialogueAudioSource()
    {
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.playOnAwake = false;
            dialogueAudioSource.loop = false;
            dialogueAudioSource.spatialBlend = 0f;
            return;
        }

        dialogueAudioSource = GetComponent<AudioSource>();

        if (dialogueAudioSource == null)
        {
            dialogueAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        dialogueAudioSource.playOnAwake = false;
        dialogueAudioSource.loop = false;
        dialogueAudioSource.spatialBlend = 0f;
        dialogueAudioSource.volume = dialogueVolume;
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
        phaseBarsAllowed = true;
        currentPhase = 1;
        currentHealth = GetPhaseMaxHealth(1);
        isDead = false;
        RefreshPhaseHealthBars();

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

    public void HidePhaseHealthBars()
    {
        phaseBarsAllowed = false;
        ResolvePhaseHealthBars();

        for (int i = 0; i < phaseBarObjects.Length; i++)
        {
            GameObject bar = phaseBarObjects[i];
            if (bar == null) continue;

            if (bar.activeSelf)
            {
                bar.SetActive(false);
                DebugHealth("Made phase " + (i + 1) +
                            " health bar invisible while the boss is despawned: " +
                            bar.name + ".");
            }
        }

        lastLoggedVisiblePhase = 0;
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

    private void ResolvePhaseHealthBars()
    {
        phaseBarObjects[0] = phase1HealthBar;
        phaseBarObjects[1] = phase2HealthBar;
        phaseBarObjects[2] = phase3HealthBar;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                !candidate.gameObject.scene.isLoaded)
                continue;

            for (int phase = 1; phase <= 3; phase++)
            {
                int index = phase - 1;
                if (phaseBarObjects[index] == null &&
                    candidate.name == "HealthBar_phase" + phase)
                {
                    phaseBarObjects[index] = candidate.gameObject;
                }
            }
        }

        for (int i = 0; i < phaseBarObjects.Length; i++)
        {
            if (phaseBarObjects[i] == null)
            {
                phaseBarSliders[i] = null;
                phaseBarFillImages[i] = null;
                continue;
            }

            phaseBarSliders[i] =
                phaseBarObjects[i].GetComponentInChildren<Slider>(true);

            phaseBarFillImages[i] = null;
            Image[] images =
                phaseBarObjects[i].GetComponentsInChildren<Image>(true);

            for (int j = 0; j < images.Length; j++)
            {
                if (images[j] != null &&
                    images[j].type == Image.Type.Filled)
                {
                    phaseBarFillImages[i] = images[j];
                    break;
                }
            }
        }

        if (debugHealth && !phaseBarBindingsLogged &&
            phaseBarObjects[0] != null &&
            phaseBarObjects[1] != null &&
            phaseBarObjects[2] != null)
        {
            phaseBarBindingsLogged = true;
            DebugHealth(
                "Resolved phase bars: phase 1=" + phaseBarObjects[0].name +
                " (slider=" + (phaseBarSliders[0] != null) + "), phase 2=" +
                phaseBarObjects[1].name + " (slider=" +
                (phaseBarSliders[1] != null) + "), phase 3=" +
                phaseBarObjects[2].name + " (slider=" +
                (phaseBarSliders[2] != null) + ")."
            );
        }

        RefreshPhaseHealthBars();
    }

    private void EnsurePhaseBarVisible(GameObject bar)
    {
        if (bar == null) return;

        // The three phase health bars may sit under canvases or parents that
        // were left disabled by a scene transition or loading screen. Allow
        // the currently active phase's bar to actually be shown.
        Transform node = bar.transform;
        while (node != null)
        {
            if (!node.gameObject.activeSelf)
                node.gameObject.SetActive(true);

            Canvas canvas = node.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (LoadingScreenManager.Instance == null ||
                    !LoadingScreenManager.Instance.AreGameplayCanvasesHidden)
                {
                    if (!canvas.enabled)
                        canvas.enabled = true;
                }

                if (canvas.isRootCanvas)
                    break;
            }

            node = node.parent;
        }
    }

    private void RefreshPhaseHealthBars()
    {
        for (int i = 0; i < phaseBarObjects.Length; i++)
        {
            GameObject bar = phaseBarObjects[i];
            if (bar == null)
                continue;

            bool active = phaseBarsAllowed && !isDead && i == currentPhase - 1;

            if (active && lastLoggedVisiblePhase != currentPhase)
            {
                lastLoggedVisiblePhase = currentPhase;
                DebugHealth(
                    "Showing phase " + currentPhase + " health bar: " +
                    bar.name + "."
                );
            }

            if (active)
                EnsurePhaseBarVisible(bar);

            if (bar.activeSelf != active)
            {
                bar.SetActive(active);
                DebugHealth(
                    "Made phase " + (i + 1) + " health bar " +
                    (active ? "visible" : "invisible") + ": " + bar.name + "."
                );
            }

            if (active)
                EnsurePhaseBarVisible(bar);

            if (!active)
                continue;

            float maxHealth = GetPhaseMaxHealth(currentPhase);
            if (maxHealth <= 0f)
                continue;

            float normalizedHealth =
                Mathf.Clamp01(currentHealth / maxHealth);

            Slider slider = phaseBarSliders[i];
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = normalizedHealth;
            }
            else if (phaseBarFillImages[i] != null)
            {
                phaseBarFillImages[i].fillAmount = normalizedHealth;
            }
        }
    }

    private void EnsureDamageHitbox()
    {
        int damageLayer = LayerMask.NameToLayer("Damage");

        Collider existing = GetComponent<Collider>();

        if (existing == null)
        {
            existing = GetComponentInChildren<Collider>(true);
        }

        if (existing != null)
        {
            existing.enabled = true;
            existing.isTrigger = false;

            if (damageLayer >= 0 &&
                existing.gameObject.layer != damageLayer)
            {
                existing.gameObject.layer = damageLayer;
            }

            return;
        }

        if (!autoCreateHitboxIfMissing)
        {
            Debug.LogWarning(
                "[FinalBossHealth] No collider found on " +
                gameObject.name +
                ". Player attacks cannot hit Gargamel.",
                this
            );
            return;
        }

        BoxCollider hitbox = gameObject.AddComponent<BoxCollider>();
        hitbox.center = hitboxCenter;
        hitbox.size = hitboxSize;
        hitbox.isTrigger = false;
        hitbox.enabled = true;

        if (damageLayer >= 0)
        {
            gameObject.layer = damageLayer;
        }

        DebugHealth(
            "Auto-created Damage hitbox on " +
            gameObject.name +
            " (size " + hitboxSize + ", center " + hitboxCenter + ")."
        );
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
