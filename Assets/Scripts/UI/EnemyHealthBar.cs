using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Slider from a Health component (e.g. Level 1 doll, theater puppets).
/// Place on the enemy's world-space health Canvas or Slider.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Health health;

    [SerializeField]
    private Slider healthSlider;

    [SerializeField]
    private bool findHealthInParents = true;

    [SerializeField]
    private bool findSliderInChildren = true;

    [SerializeField]
    private bool hideWhenDead = true;

    [Header("Optional boss-fight visibility")]
    [Tooltip("Enable this on theater puppet health bars so they appear while watching a play or during the boss fight.")]
    [SerializeField] private bool onlyShowDuringBossFight = false;
    [SerializeField] private CanvasGroup visibilityCanvasGroup;

    private bool hasLoggedBossFightVisibility;
    private bool lastBossFightVisibility;
    private bool hasLoggedTargetVisibility;
    private bool lastTargetVisibility;
    private bool loggedFinalBossDelegation;


    private void Awake()
    {
        ResolveReferences();
        EnsureCanvasUsesUiLayer();
        RefreshImmediate();
        ApplyBossFightVisibility();
    }


    private void OnEnable()
    {
        ApplyBossFightVisibility();
    }


    private void LateUpdate()
    {
        ApplyBossFightVisibility();
        if (onlyShowDuringBossFight && !IsTheaterOrBossGameplayActive()) return;

        if (health == null || healthSlider == null)
        {
            ResolveReferences();

            if (health == null || healthSlider == null)
            {
                // Gargamel uses FinalBossHealth, which drives its three
                // assigned phase sliders directly. Do not treat that custom
                // health owner as a missing normal Health component: this
                // Canvas-level helper otherwise alpha-hides all boss bars.
                FinalBossHealth finalBossHealth =
                    GetComponentInParent<FinalBossHealth>(true);
                if (health == null && finalBossHealth != null)
                {
                    EnsureCanvasUsesUiLayer();
                    if (!loggedFinalBossDelegation)
                    {
                        loggedFinalBossDelegation = true;
                        Debug.Log("[EnemyHealthBar] " + name +
                            " is delegating its assigned phase sliders to " +
                            "FinalBossHealth on " + finalBossHealth.name +
                            "; skipping normal Health-owner hiding.", this);
                    }
                    return;
                }

                // Theater puppet bars are controlled by the boss-fight mode
                // gate; a missing Health reference must not hide them.
                if (!onlyShowDuringBossFight)
                    SetHealthTargetVisibility(false);
                return;
            }
        }

        // World-space bars can sit outside an inactive boss prefab and stay
        // enabled during theater/day transitions. Tie visibility to the
        // actual Health owner so the bar appears only when that enemy spawns.
        bool targetIsSpawned = health.gameObject.activeInHierarchy;
        if (!onlyShowDuringBossFight)
            SetHealthTargetVisibility(targetIsSpawned);
        if (!targetIsSpawned)
            return;

        FaceWorldSpaceCanvasToCamera();

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;

        // A puppet's Health component stays disabled until its activation
        // delay finishes, so its current health may still be unset (0), which
        // used to draw an empty - effectively invisible - bar. Show it full.
        if (onlyShowDuringBossFight && !health.enabled)
        {
            healthSlider.value = 1f;
            return;
        }

        float max = health.GetMaxHealth();

        if (max <= 0f)
        {
            return;
        }

        healthSlider.value = Mathf.Clamp01(
            health.GetCurrentHealth() / max
        );

        if (!onlyShowDuringBossFight && hideWhenDead && health.IsDead())
        {
            gameObject.SetActive(false);
        }
    }


    // ---------------------------------------------------------
    // PUBLIC API (used by BossFightPuppetActivation)
    // ---------------------------------------------------------

    /// <summary>True if this bar displays the given Health (or sits under it).</summary>
    public bool BelongsTo(Health owner, Transform ownerRoot)
    {
        ResolveReferences();

        if (owner != null && health == owner) return true;
        if (ownerRoot != null && transform.IsChildOf(ownerRoot)) return true;
        return false;
    }

    /// <summary>
    /// Makes the bar visible right now. Activates this object (and the parents
    /// between it and its puppet / canvas) if the scene left them inactive.
    /// </summary>
    public void ShowForBossFight()
    {
        ResolveReferences();
        EnsureCanvasUsesUiLayer();

        Transform node = transform;
        while (node != null)
        {
            if (!node.gameObject.activeSelf)
                node.gameObject.SetActive(true);

            Canvas canvas = node.GetComponent<Canvas>();
            if (canvas != null)
            {
                // Never override the loading screen's hidden canvases.
                if (!IsLoadingScreenActive())
                    canvas.enabled = true;
                if (canvas.isRootCanvas) break;
            }

            if (health != null && node == health.transform) break;
            node = node.parent;
        }

        ApplyBossFightVisibility();
    }


    // ---------------------------------------------------------
    // VISIBILITY
    // ---------------------------------------------------------

    private bool IsTheaterOrBossGameplayActive()
    {
        if (GameStateManager.Instance == null) return false;

        GameStateManager state = GameStateManager.Instance;
        GameStateManager.GameMode mode = state.GetGameMode();
        bool theaterOrBossActive =
            mode == GameStateManager.GameMode.WatchingPlay ||
            mode == GameStateManager.GameMode.BossFight;

        // The final play unlocks these lobby puppets' health bars. Keep them
        // visible if the game flow temporarily returns to Lobby after the
        // final-play completion event has already fired.
        bool finalPlayLobby =
            mode == GameStateManager.GameMode.Lobby &&
            state.IsFinalPlayComplete();

        return theaterOrBossActive || finalPlayLobby;
    }

    private static bool IsLoadingScreenActive()
    {
        return LoadingScreenManager.Instance != null &&
               LoadingScreenManager.Instance.AreGameplayCanvasesHidden;
    }

    private CanvasGroup GetOwnGroup()
    {
        if (visibilityCanvasGroup == null)
            visibilityCanvasGroup = GetComponent<CanvasGroup>();

        // Use a group on THIS object. Borrowing a parent/child group meant a
        // shared group (or one at alpha 0 higher up) kept the bar invisible.
        if (visibilityCanvasGroup == null)
            visibilityCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        return visibilityCanvasGroup;
    }

    private void ApplyBossFightVisibility()
    {
        if (!onlyShowDuringBossFight) return;

        bool dead = hideWhenDead && health != null && health.IsDead();
        bool shouldShow = IsTheaterOrBossGameplayActive() && !dead;

        if (!hasLoggedBossFightVisibility ||
            lastBossFightVisibility != shouldShow)
        {
            hasLoggedBossFightVisibility = true;
            lastBossFightVisibility = shouldShow;
            Debug.Log(
                "[EnemyHealthBar] Made " + name +
                (shouldShow ? " visible" : " invisible") +
                ", owner=" + (health != null ? health.name : "unresolved") +
                ", mode=" + (GameStateManager.Instance != null
                    ? GameStateManager.Instance.GetGameMode().ToString()
                    : "Unavailable") +
                ", finalPlayComplete=" +
                (GameStateManager.Instance != null &&
                 GameStateManager.Instance.IsFinalPlayComplete()) +
                ", render=" + GetRenderDiagnostic(),
                this
            );
        }

        CanvasGroup group = GetOwnGroup();
        group.alpha = shouldShow ? 1f : 0f;
        group.interactable = shouldShow;
        group.blocksRaycasts = shouldShow;

        // A parent CanvasGroup at alpha 0 would otherwise override this one.
        group.ignoreParentGroups = shouldShow;

        if (shouldShow && !IsLoadingScreenActive())
        {
            EnsureCanvasUsesUiLayer();
            Canvas[] canvases = GetComponentsInParent<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && !canvases[i].enabled)
                    canvases[i].enabled = true;
            }
        }
    }

    /// <summary>
    /// Enemy prefabs use the Damage layer for their colliders. Their nested
    /// world-space health canvases inherit that layer, which some gameplay
    /// cameras may exclude. Move only the health-bar canvas subtree to UI.
    /// </summary>
    private void EnsureCanvasUsesUiLayer()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        int uiLayer = LayerMask.NameToLayer("UI");
        if (canvas == null || uiLayer < 0) return;

        Transform[] canvasObjects = canvas.GetComponentsInChildren<Transform>(true);
        bool changed = false;
        for (int i = 0; i < canvasObjects.Length; i++)
        {
            if (canvasObjects[i].gameObject.layer == uiLayer) continue;
            canvasObjects[i].gameObject.layer = uiLayer;
            changed = true;
        }

        if (changed)
        {
            Debug.Log("[EnemyHealthBar] Set world-space bar Canvas subtree to UI layer " +
                      uiLayer + " so the gameplay camera can render it; canvas=" +
                      canvas.name + ", renderMode=" + canvas.renderMode +
                      ", sortingOrder=" + canvas.sortingOrder + ".", this);
        }
    }

    private string GetRenderDiagnostic()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        Camera camera = Camera.main;
        int layer = canvas != null ? canvas.gameObject.layer : gameObject.layer;
        bool cameraIncludesLayer = camera == null ||
            (camera.cullingMask & (1 << layer)) != 0;
        return "canvas=" + (canvas != null ? canvas.name : "missing") +
               ", mode=" + (canvas != null ? canvas.renderMode.ToString() : "n/a") +
               ", enabled=" + (canvas != null && canvas.enabled) +
               ", layer=" + LayerMask.LayerToName(layer) + "(" + layer + ")" +
               ", camera=" + (camera != null ? camera.name : "main camera missing") +
               ", cameraIncludesLayer=" + cameraIncludesLayer +
               ", canvasScale=" + (canvas != null
                   ? canvas.transform.lossyScale.ToString()
                   : "n/a");
    }

    private void FaceWorldSpaceCanvasToCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        Camera camera = Camera.main;
        if (canvas == null || camera == null ||
            canvas.renderMode != RenderMode.WorldSpace)
            return;

        Vector3 toCamera = camera.transform.position - canvas.transform.position;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            canvas.transform.rotation = Quaternion.LookRotation(
                toCamera.normalized, camera.transform.up);
        }

        if (canvas.worldCamera != camera)
            canvas.worldCamera = camera;
    }

    private void SetHealthTargetVisibility(bool visible)
    {
        if (!hasLoggedTargetVisibility || lastTargetVisibility != visible)
        {
            hasLoggedTargetVisibility = true;
            lastTargetVisibility = visible;
            Debug.Log(
                "[EnemyHealthBar] Made " + name +
                (visible ? " visible" : " invisible") +
                " based on owner spawn state (owner=" +
                (health != null ? health.name : "unresolved") +
                ", render=" + GetRenderDiagnostic() + ").",
                this
            );
        }

        if (visibilityCanvasGroup == null)
        {
            visibilityCanvasGroup = GetComponent<CanvasGroup>();
            if (visibilityCanvasGroup == null)
                visibilityCanvasGroup = GetComponentInParent<CanvasGroup>();
            if (visibilityCanvasGroup == null)
                visibilityCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }

        if (visibilityCanvasGroup != null)
        {
            visibilityCanvasGroup.alpha = visible ? 1f : 0f;
            visibilityCanvasGroup.interactable = visible;
            visibilityCanvasGroup.blocksRaycasts = visible;
            return;
        }

        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.enabled = visible;
    }

    private void ResolveReferences()
    {
        if (health == null && findHealthInParents)
        {
            health = GetComponentInParent<Health>();
        }

        if (healthSlider == null && findSliderInChildren)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
    }


    private void RefreshImmediate()
    {
        if (health == null || healthSlider == null)
        {
            return;
        }

        float max = health.GetMaxHealth();

        if (max <= 0f)
        {
            return;
        }

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = Mathf.Clamp01(
            health.GetCurrentHealth() / max
        );
    }
}
