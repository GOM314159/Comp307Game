using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gargamel's custom three-phase health system with phase dialogues and hurt/intro audio.
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

    [Header("Boss Audio & Dialogue")]
    [Tooltip("Plays when the boss encounter starts ('Come play with me').")]
    [SerializeField] private AudioClip introDialogue;

    [Tooltip("Plays on entering Phase 2 ('Did you think you could defeat me').")]
    [SerializeField] private AudioClip phase2Dialogue;

    [Tooltip("Plays on entering Phase 3 ('You will fall to the master of puppets').")]
    [SerializeField] private AudioClip phase3Dialogue;

    [Tooltip("Plays when taking damage ('huuuh').")]
    [SerializeField] private AudioClip hurtAudioClip;

    [Tooltip("AudioSource used for boss voice lines and SFX.")]
    [SerializeField] private AudioSource dialogueAudioSource;

    [SerializeField][Range(0f, 1f)] private float dialogueVolume = 1f;

    [Header("Hitbox")]
    [SerializeField] private bool autoCreateHitboxIfMissing = true;
    [SerializeField] private Vector3 hitboxCenter = new Vector3(0f, 1.0f, 0f);
    [SerializeField] private Vector3 hitboxSize = new Vector3(1.2f, 2.2f, 1.2f);

    [Header("Final Death")]
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Debug")]
    [SerializeField] private bool debugHealth = true;

    private int currentPhase = 1;
    private float currentHealth;
    private bool isDead;

    [Header("Phase Health Bar UI")]
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
        PlayAudioClip(introDialogue);
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

        DebugHealth(gameObject.name + " Phase " + currentPhase + " took " + damage + " damage. Remaining: " + currentHealth);

        if (currentHealth > 0f)
        {
            PlayAudioClip(hurtAudioClip);
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
    }

    private void PlayPhaseDialogue(int phase)
    {
        AudioClip dialogue = (phase == 2) ? phase2Dialogue : (phase == 3 ? phase3Dialogue : null);

        if (dialogue != null)
        {
            PlayAudioClip(dialogue);
        }
    }

    private void PlayAudioClip(AudioClip clip)
    {
        if (clip == null) return;

        EnsureDialogueAudioSource();

        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = clip;
            dialogueAudioSource.volume = dialogueVolume;
            dialogueAudioSource.Play();
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, dialogueVolume);
        }
    }

    private void EnsureDialogueAudioSource()
    {
        if (dialogueAudioSource == null)
        {
            dialogueAudioSource = GetComponent<AudioSource>();
            if (dialogueAudioSource == null)
            {
                dialogueAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        dialogueAudioSource.playOnAwake = false;
        dialogueAudioSource.loop = false;
        dialogueAudioSource.spatialBlend = 0f;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        DebugHealth("Gargamel final boss defeated.");

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
    }

    public int GetCurrentPhase() => currentPhase;
    public float GetCurrentHealth() => currentHealth;
    public float GetCurrentMaxHealth() => GetPhaseMaxHealth(currentPhase);
    public float GetCurrentDamage() => currentPhase switch { 1 => phase1Damage, 2 => phase2Damage, _ => phase3Damage };
    public bool IsDead() => isDead;

    public void HidePhaseHealthBars()
    {
        phaseBarsAllowed = false;
        ResolvePhaseHealthBars();

        for (int i = 0; i < phaseBarObjects.Length; i++)
        {
            if (phaseBarObjects[i] != null && phaseBarObjects[i].activeSelf)
            {
                phaseBarObjects[i].SetActive(false);
            }
        }

        lastLoggedVisiblePhase = 0;
    }

    private float GetPhaseMaxHealth(int phase)
    {
        float baseHealth = phase switch { 1 => phase1MaxHealth, 2 => phase2MaxHealth, _ => phase3MaxHealth };
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsHardcore())
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
            if (candidate == null || !candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded) continue;

            for (int phase = 1; phase <= 3; phase++)
            {
                int index = phase - 1;
                if (phaseBarObjects[index] == null && candidate.name == "HealthBar_phase" + phase)
                {
                    phaseBarObjects[index] = candidate.gameObject;
                }
            }
        }

        for (int i = 0; i < phaseBarObjects.Length; i++)
        {
            if (phaseBarObjects[i] == null) continue;
            phaseBarSliders[i] = phaseBarObjects[i].GetComponentInChildren<Slider>(true);

            Image[] images = phaseBarObjects[i].GetComponentsInChildren<Image>(true);
            for (int j = 0; j < images.Length; j++)
            {
                if (images[j] != null && images[j].type == Image.Type.Filled)
                {
                    phaseBarFillImages[i] = images[j];
                    break;
                }
            }
        }

        RefreshPhaseHealthBars();
    }

    private void EnsurePhaseBarVisible(GameObject bar)
    {
        if (bar == null) return;

        Canvas barCanvas = bar.GetComponentInParent<Canvas>(true);
        int uiLayer = LayerMask.NameToLayer("UI");
        if (barCanvas != null && uiLayer >= 0)
        {
            Transform[] canvasObjects = barCanvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < canvasObjects.Length; i++)
            {
                if (canvasObjects[i].gameObject.layer != uiLayer)
                {
                    canvasObjects[i].gameObject.layer = uiLayer;
                }
            }
        }

        Transform node = bar.transform;
        while (node != null)
        {
            if (!node.gameObject.activeSelf) node.gameObject.SetActive(true);
            Canvas canvas = node.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (LoadingScreenManager.Instance == null || !LoadingScreenManager.Instance.AreGameplayCanvasesHidden)
                {
                    if (!canvas.enabled) canvas.enabled = true;
                }
                if (canvas.isRootCanvas) break;
            }
            node = node.parent;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 toCamera = camera.transform.position - bar.transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                bar.transform.rotation = Quaternion.LookRotation(toCamera.normalized, camera.transform.up);
            }
        }
    }

    private void RefreshPhaseHealthBars()
    {
        for (int i = 0; i < phaseBarObjects.Length; i++)
        {
            GameObject bar = phaseBarObjects[i];
            if (bar == null) continue;

            bool active = phaseBarsAllowed && !isDead && i == currentPhase - 1;

            if (active && lastLoggedVisiblePhase != currentPhase)
            {
                lastLoggedVisiblePhase = currentPhase;
            }

            if (active) EnsurePhaseBarVisible(bar);

            if (bar.activeSelf != active)
            {
                bar.SetActive(active);
            }

            if (!active) continue;

            float maxHealth = GetPhaseMaxHealth(currentPhase);
            if (maxHealth <= 0f) continue;

            float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);

            if (phaseBarSliders[i] != null)
            {
                phaseBarSliders[i].minValue = 0f;
                phaseBarSliders[i].maxValue = 1f;
                phaseBarSliders[i].value = normalizedHealth;
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
        Collider existing = GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);

        if (existing != null)
        {
            existing.enabled = true;
            existing.isTrigger = false;
            if (damageLayer >= 0 && existing.gameObject.layer != damageLayer)
            {
                existing.gameObject.layer = damageLayer;
            }
            return;
        }

        if (!autoCreateHitboxIfMissing) return;

        BoxCollider hitbox = gameObject.AddComponent<BoxCollider>();
        hitbox.center = hitboxCenter;
        hitbox.size = hitboxSize;
        hitbox.isTrigger = false;
        hitbox.enabled = true;

        if (damageLayer >= 0) gameObject.layer = damageLayer;
    }

    private void DebugHealth(string message)
    {
        if (debugHealth) Debug.Log("[FinalBossHealth] " + message, this);
    }
}