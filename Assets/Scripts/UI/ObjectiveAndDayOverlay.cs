using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// HUD overlay with two independent trackers:
///
///   DAY tracker       - visible in EVERY scene and on EVERY day (lobby,
///                       level 1/2/3, boss fight). It is only hidden for
///                       menus, cutscenes, plays, credits, pause and loading.
///
///   OBJECTIVE tracker - visible while a case is active (including pause or
///                       a short Lobby-mode transition) and hidden when the
///                       case is over or a full-screen transition is active.
/// </summary>
public class ObjectiveAndDayOverlay : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private TextMeshProUGUI dayText;

    [SerializeField]
    private TextMeshProUGUI objectiveText;

    [SerializeField]
    private string dayTextObjectName = "DayText";

    [SerializeField]
    private string objectiveTextObjectName = "ObjectiveText";


    [Header("Copy")]

    [SerializeField]
    private string dayFormat = "DAY {0}";

    [SerializeField]
    private string findObjectFormat = "Find the {0}";

    [SerializeField]
    private string defeatBossFormat = "Defeat the {0}";

    [SerializeField]
    private string returnFormat = "Return to the lobby";


    [Header("Visibility")]

    [Tooltip(
        "When enabled, the overlay automatically hides whenever Time.timeScale is 0. " +
        "This covers normal pause behaviour."
    )]
    [SerializeField]
    private bool hideWhilePaused = true;

    [Tooltip(
        "When enabled, the objective text is only shown if the loaded scene " +
        "contains a CaseSceneBinding whose Case ID matches the active case. " +
        "This is what limits each objective to its own level."
    )]
    [SerializeField]
    private bool requireCaseSceneBinding = true;

    [SerializeField]
    private bool debugVisibility = true;

    [Tooltip("Base sorting order for the isolated text canvases. Full-screen transitions are hidden by visibility rules.")]
    [SerializeField]
    private int textCanvasSortingOrder = 32766;


    // Explicit visibility requested by menus/cutscenes.
    private bool explicitVisibility = true;
    private GameStateManager subscribedGameState;

    // Cache for "does the loaded scene belong to the active case?"
    private ulong bindingCacheSceneHandle = ulong.MaxValue;
    private int bindingCacheCaseID = int.MinValue;
    private bool bindingCacheResult;
    private float bindingCacheTime;
    private bool sceneListenerAdded;
    private bool hasLoggedVisibility;
    private bool lastDayVisible;
    private bool lastObjectiveVisible;
    private string lastDayRenderStatus;
    private string lastObjectiveRenderStatus;
    private string lastDayVisibilityReason;
    private bool hasLoggedInitialization;
    private Canvas dayTextCanvas;
    private Canvas objectiveTextCanvas;
    private bool hasLoggedCanvasDeferral;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        ResolveTexts();
        RefreshAll();
        ApplyVisibility();
        LogInitialization();
    }


    private void OnEnable()
    {
        Subscribe();

        if (!sceneListenerAdded)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneListenerAdded = true;
        }

        RefreshAll();
        ApplyVisibility();
        LogInitialization();
    }


    private void Start()
    {
        Subscribe();
        RefreshAll();
        ApplyVisibility();
        LogInitialization();
    }


    private void Update()
    {
        // GameStateManager can be created after this UI; rebind if needed.
        Subscribe();

        if (dayText == null || objectiveText == null)
        {
            ResolveTexts();
        }

        RefreshAll();
        ApplyVisibility();
        LogInitialization();
    }


    public void RefreshNow()
    {
        ResolveTexts();
        Subscribe();
        InvalidateBindingCache();
        RefreshAll();
        ApplyVisibility();
    }


    private void OnDisable()
    {
        Unsubscribe();

        if (sceneListenerAdded)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneListenerAdded = false;
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (sceneListenerAdded)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneListenerAdded = false;
        }
    }


    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InvalidateBindingCache();

        // A scene change must never leave the HUD flagged as hidden by a
        // cutscene from the previous scene. Menu/cutscene/loading modes are
        // still hidden automatically by ApplyVisibility().
        explicitVisibility = true;

        ResolveTexts();
        RefreshAll();
        ApplyVisibility();
    }


    // ============================================================
    // VISIBILITY API
    // ============================================================

    public void SetOverlayVisible(bool visible)
    {
        explicitVisibility = visible;

        if (visible)
        {
            ActivateOverlayHierarchy();
        }

        ApplyVisibility();
    }


    public void HideOverlay()
    {
        SetOverlayVisible(false);
    }


    public void ShowOverlay()
    {
        SetOverlayVisible(true);
    }


    private void ActivateOverlayHierarchy()
    {
        Transform current = transform;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }


    /// <summary>
    /// Makes sure no ancestor Canvas was left disabled by a scene change.
    /// The loading screen legitimately disables canvases while it is shown,
    /// so nothing is touched during that time.
    /// </summary>
    private void EnsureCanvasesEnabled()
    {
        if (LoadingScreenManager.Instance != null &&
            LoadingScreenManager.Instance.AreGameplayCanvasesHidden)
        {
            return;
        }

        Canvas[] canvases = GetComponentsInParent<Canvas>(true);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && !canvases[i].enabled)
            {
                canvases[i].enabled = true;
            }
        }
    }


    private static bool IsHudHiddenMode(GameStateManager.GameMode mode)
    {
        return mode == GameStateManager.GameMode.Loading ||
               mode == GameStateManager.GameMode.MainMenu ||
               mode == GameStateManager.GameMode.WatchingPlay ||
               mode == GameStateManager.GameMode.Cutscene ||
               mode == GameStateManager.GameMode.Credits ||
               mode == GameStateManager.GameMode.Paused;
    }


    /// <summary>
    /// DayText follows the player-bar roots, and is hidden during loading,
    /// the day-screen video, and the tutorial's opaque fade. It is otherwise
    /// independent of SetOverlayVisible/HideOverlay calls and objective state.
    /// </summary>
    private bool ShouldShowDay()
    {
        // The day label belongs in gameplay HUD only; keep it off the main
        // menu even when the persistent health and stamina bar roots are active.
        GameStateManager state = GameStateManager.Instance;
        if (state != null && state.GetGameMode() == GameStateManager.GameMode.MainMenu)
        {
            return false;
        }

        // Use exactly the player HUD bars as the source of truth. The DayText
        // lives on an isolated Canvas, so separate loading/tutorial/cutscene
        // flags can incorrectly keep it hidden after the HUD has returned.
        // GameFlowManager already hides and restores these bar roots for all
        // full-screen transitions, including the Day 1 video and tutorial.
        GameFlowManager flow = GameFlowManager.Instance;
        if (flow != null &&
            (flow.HealthBarObject == null ||
             !flow.HealthBarObject.activeInHierarchy ||
             flow.StaminaBarObject == null ||
             !flow.StaminaBarObject.activeInHierarchy))
        {
            return false;
        }

        return true;
    }


    /// <summary>Objective tracker rule: only inside the level of the active case.</summary>
    private bool ShouldShowObjective()
    {
        GameStateManager state = GameStateManager.Instance;

        if (state == null)
        {
            return false;
        }

        GameStateManager.GameMode mode = state.GetGameMode();

        bool caseActive = state.IsCaseActive();
        bool caseOrBossMode = mode == GameStateManager.GameMode.Case ||
                              mode == GameStateManager.GameMode.BossFight;
        bool allowedPausedOrTransitionMode =
            mode == GameStateManager.GameMode.Paused ||
            mode == GameStateManager.GameMode.Lobby;

        if (!caseOrBossMode && !(caseActive && allowedPausedOrTransitionMode))
        {
            return false;
        }

        // Some levels do not contain a CaseSceneBinding even though the
        // active case/objective state is valid. Requiring that optional
        // scene-local helper made the objective disappear outside Level 1.
        // If a binding exists, use it as validation; if not, keep the active
        // objective visible in the current case/boss gameplay scene.
        int caseID = state.GetCurrentCase();
        if (caseID < 0)
            return false;

        // Case startup can complete while the loading screen is still
        // transitioning into the destination scene. Show the initial find
        // objective as soon as a case level is active, even if the objective
        // state has not yet advanced from NotStarted.
        if (state.GetCurrentObjective() ==
                GameStateManager.ObjectiveState.NotStarted &&
            !IsCaseLevelScene(SceneManager.GetActiveScene().name))
        {
            return false;
        }

        // Each level's objective must be allowed to display while that case
        // is running, in every level (not only Level 1). Do not gate it on
        // an optional, scene-local CaseSceneBinding match.
        return true;
    }

    private static bool IsCaseLevelScene(string sceneName)
    {
        return sceneName == "Level1Scene" ||
               sceneName == "Level2Scene" ||
               sceneName == "Level3Scene";
    }



    private bool HasAnyCaseSceneBinding()
    {
        CaseSceneBinding[] bindings = FindObjectsByType<CaseSceneBinding>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        return bindings != null && bindings.Length > 0;
    }

    private void InvalidateBindingCache()
    {
        bindingCacheSceneHandle = ulong.MaxValue;
        bindingCacheCaseID = int.MinValue;
        bindingCacheResult = false;
    }


    private bool IsActiveCaseMappedToLoadedScene(int caseID)
    {
        if (caseID < 0)
        {
            return false;
        }

        if (!requireCaseSceneBinding)
        {
            return true;
        }

        ulong sceneHandle = SceneManager.GetActiveScene().handle.GetRawData();

        bool cacheValid =
            sceneHandle == bindingCacheSceneHandle &&
            caseID == bindingCacheCaseID &&
            (bindingCacheResult ||
             Time.unscaledTime - bindingCacheTime < 0.5f);

        if (cacheValid)
        {
            return bindingCacheResult;
        }

        bool found = false;

        CaseSceneBinding[] bindings =
            FindObjectsByType<CaseSceneBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < bindings.Length; i++)
        {
            if (bindings[i] != null &&
                bindings[i].GetCaseID() == caseID)
            {
                found = true;
                break;
            }
        }

        bindingCacheSceneHandle = sceneHandle;
        bindingCacheCaseID = caseID;
        bindingCacheResult = found;
        bindingCacheTime = Time.unscaledTime;

        return found;
    }


    private void ApplyVisibility()
    {
        if (dayText == null && objectiveText == null)
        {
            return;
        }

        bool showDay = ShouldShowDay();
        // Objectives belong to case gameplay and should appear as soon as
        // the level finishes loading, even if the player HUD bars are hidden.
        bool loading = LoadingScreenManager.Instance != null &&
                       LoadingScreenManager.Instance.AreGameplayCanvasesHidden;
        bool tutorialFade = TutorialManager.Instance != null &&
                            TutorialManager.Instance.IsScreenFadeVisible;
        bool dayScreen = DayScreenCutscene.Instance != null &&
                         DayScreenCutscene.Instance.IsPlayingOrPending;
        bool showObjective = explicitVisibility && !loading &&
                             !tutorialFade && !dayScreen &&
                             ShouldShowObjective();

        if (showDay)
        {
            // DayText may be authored outside this overlay subtree in some
            // scenes. Activate its own hierarchy and canvas rather than
            // assuming the overlay's hierarchy owns it.
            if (dayText != null)
            {
                ActivateHierarchy(dayText.transform);
                EnsureTextCanvasesEnabled(dayText);
                EnsureTextRenderable(dayText);
            }
            ActivateOverlayHierarchy();
            EnsureCanvasesEnabled();
            EnsureTextCanvasSorting(dayText, textCanvasSortingOrder);
        }

        if (showObjective && objectiveText != null)
        {
            ActivateHierarchy(objectiveText.transform);
            EnsureTextCanvasesEnabled(objectiveText);
            EnsureTextRenderable(objectiveText);
            EnsureTextCanvasSorting(objectiveText, textCanvasSortingOrder + 1);
        }

        SetActiveIfChanged(dayText, showDay);
        SetActiveIfChanged(objectiveText, showObjective);
        if (showDay || showObjective)
        {
            Canvas.ForceUpdateCanvases();
            if (showDay && dayText != null) dayText.ForceMeshUpdate();
            if (showObjective && objectiveText != null)
                objectiveText.ForceMeshUpdate();
        }
        LogVisibilityChanges(showDay, showObjective);
    }

    private void EnsureTextCanvasSorting(TextMeshProUGUI text, int order)
    {
        if (text == null) return;

        // Give each label an actual root Screen Space Overlay Canvas instead
        // of relying on a nested Canvas inside the shared gameplay HUD.
        // Nested canvases can remain active/renderable yet still be batched or
        // covered by the large HUD and scene canvases.
        Canvas cachedCanvas = text == dayText ? dayTextCanvas : objectiveTextCanvas;
        if (cachedCanvas == null || cachedCanvas.transform != text.transform.parent)
        {
            Canvas existingParentCanvas = text.GetComponentInParent<Canvas>(true);
            if (existingParentCanvas != null && existingParentCanvas.isRootCanvas &&
                existingParentCanvas.gameObject.name == text.gameObject.name + "Canvas")
            {
                cachedCanvas = existingParentCanvas;
            }
            else
            {
                PersistentGameplayUI persistentUI = PersistentGameplayUI.Instance;
                if (persistentUI == null)
                {
                    if (!hasLoggedCanvasDeferral)
                    {
                        hasLoggedCanvasDeferral = true;
                        Debug.LogWarning("[ObjectiveAndDayOverlay] Waiting for PersistentGameplayUI before creating standalone text canvases.", this);
                    }
                    return;
                }

                cachedCanvas = CreateStandaloneTextCanvas(text, persistentUI.transform);
            }

            if (cachedCanvas == null) return;
            if (text == dayText) dayTextCanvas = cachedCanvas;
            else if (text == objectiveText) objectiveTextCanvas = cachedCanvas;
        }

        cachedCanvas.overrideSorting = true;
        cachedCanvas.sortingOrder = order;
        cachedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (!cachedCanvas.enabled) cachedCanvas.enabled = true;
    }

    private Canvas CreateStandaloneTextCanvas(TextMeshProUGUI text, Transform persistentRoot)
    {
        RectTransform textRect = text.transform as RectTransform;
        if (textRect == null)
        {
            Debug.LogError("[ObjectiveAndDayOverlay] Cannot create standalone Canvas for " +
                            text.name + ": its Transform is not a RectTransform.", text);
            return null;
        }

        Vector2 anchorMin = textRect.anchorMin;
        Vector2 anchorMax = textRect.anchorMax;
        Vector2 pivot = textRect.pivot;
        Vector2 sizeDelta = textRect.sizeDelta;
        Vector3 anchoredPosition = textRect.anchoredPosition3D;
        Quaternion localRotation = textRect.localRotation;
        Vector3 localScale = textRect.localScale;

        Canvas sourceCanvas = text.GetComponentInParent<Canvas>(true);
        if (sourceCanvas != null) sourceCanvas = sourceCanvas.rootCanvas;
        CanvasScaler sourceScaler = sourceCanvas != null
            ? sourceCanvas.GetComponent<CanvasScaler>()
            : null;

        GameObject canvasObject = new GameObject(
            text.gameObject.name + "Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.layer = text.gameObject.layer;
        canvasObject.transform.SetParent(persistentRoot, false);

        RectTransform canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.anchoredPosition3D = Vector3.zero;
        canvasRect.sizeDelta = Vector2.zero;
        canvasRect.localScale = Vector3.one;
        canvasRect.localRotation = Quaternion.identity;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        if (sourceCanvas != null)
        {
            canvas.sortingLayerID = sourceCanvas.sortingLayerID;
            canvas.targetDisplay = sourceCanvas.targetDisplay;
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (sourceScaler != null)
        {
            scaler.uiScaleMode = sourceScaler.uiScaleMode;
            scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
            scaler.scaleFactor = sourceScaler.scaleFactor;
            scaler.referenceResolution = sourceScaler.referenceResolution;
            scaler.screenMatchMode = sourceScaler.screenMatchMode;
            scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            scaler.physicalUnit = sourceScaler.physicalUnit;
            scaler.fallbackScreenDPI = sourceScaler.fallbackScreenDPI;
            scaler.defaultSpriteDPI = sourceScaler.defaultSpriteDPI;
            scaler.dynamicPixelsPerUnit = sourceScaler.dynamicPixelsPerUnit;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        textRect.SetParent(canvasRect, false);
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.pivot = pivot;
        textRect.sizeDelta = sizeDelta;
        textRect.anchoredPosition3D = anchoredPosition;
        textRect.localRotation = localRotation;
        textRect.localScale = localScale;

        Canvas obsoleteNestedCanvas = text.GetComponent<Canvas>();
        if (obsoleteNestedCanvas != null)
            Destroy(obsoleteNestedCanvas);

        Debug.Log("[ObjectiveAndDayOverlay] Moved " + text.gameObject.name +
                  " to standalone Screen Space Overlay Canvas '" + canvasObject.name +
                  "'; sourceRect preserved, sorting order will be " +
                  (text == dayText ? textCanvasSortingOrder : textCanvasSortingOrder + 1) + ".", text);
        return canvas;
    }

    private void LogVisibilityChanges(bool showDay, bool showObjective)
    {
        if (!debugVisibility)
        {
            return;
        }

        string dayStatus = ActualVisibility(dayText);
        string objectiveStatus = ActualVisibility(objectiveText);
        string dayReason = GetDayVisibilityReason();
        bool stateChanged = !hasLoggedVisibility ||
                            lastDayVisible != showDay ||
                            lastObjectiveVisible != showObjective ||
                            lastDayRenderStatus != dayStatus ||
                            lastObjectiveRenderStatus != objectiveStatus ||
                            lastDayVisibilityReason != dayReason;
        if (!stateChanged) return;

        hasLoggedVisibility = true;
        lastDayVisible = showDay;
        lastObjectiveVisible = showObjective;
        lastDayRenderStatus = dayStatus;
        lastObjectiveRenderStatus = objectiveStatus;
        lastDayVisibilityReason = dayReason;

        GameStateManager state = GameStateManager.Instance;
        string mode = state != null ? state.GetGameMode().ToString() : "Unavailable";
        string dayBars = GameFlowManager.Instance != null
            ? "health=" + (GameFlowManager.Instance.HealthBarObject != null &&
                           GameFlowManager.Instance.HealthBarObject.activeInHierarchy) +
              ", stamina=" + (GameFlowManager.Instance.StaminaBarObject != null &&
                               GameFlowManager.Instance.StaminaBarObject.activeInHierarchy)
            : "player bars unavailable";

        Debug.Log(
            "[ObjectiveAndDayOverlay] Visibility state: " +
            DescribeText(dayText, dayStatus) + "; " +
            DescribeText(objectiveText, objectiveStatus) +
            " (requested day=" + showDay + ", objective=" + showObjective + ")" +
            ", scene=" + SceneManager.GetActiveScene().name +
            ", mode=" + mode +
            ", caseActive=" + (state != null && state.IsCaseActive()) +
            ", currentCase=" + (state != null ? state.GetCurrentCase() : -1) +
            ", objectiveState=" + (state != null ? state.GetCurrentObjective().ToString() : "Unavailable") +
            ", dayVisibilityReason=" + dayReason +
            ", " + dayBars,
            this
        );
    }

    private static string GetDayVisibilityReason()
    {
        GameFlowManager flow = GameFlowManager.Instance;
        if (flow == null) return "visible (GameFlowManager unavailable; no bar gate)";
        if (flow.HealthBarObject == null) return "hidden (HealthBar reference missing)";
        if (!flow.HealthBarObject.activeInHierarchy) return "hidden (HealthBar inactive)";
        if (flow.StaminaBarObject == null) return "hidden (StaminaBar reference missing)";
        if (!flow.StaminaBarObject.activeInHierarchy) return "hidden (StaminaBar inactive)";
        return "visible (HealthBar and StaminaBar active)";
    }

    private static string ActualVisibility(TextMeshProUGUI text)
    {
        if (text == null) return "unresolved";
        if (!text.gameObject.activeInHierarchy) return "invisible (inactive hierarchy)";
        if (!text.enabled) return "invisible (TMP component disabled)";
        if (text.color.a <= 0.001f) return "invisible (text alpha is zero)";

        Vector3 scale = text.transform.lossyScale;
        if (Mathf.Abs(scale.x) <= 0.0001f ||
            Mathf.Abs(scale.y) <= 0.0001f ||
            Mathf.Abs(scale.z) <= 0.0001f)
        {
            return "invisible (zero transform scale " + scale + ")";
        }

        Canvas[] canvases = text.GetComponentsInParent<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && !canvases[i].enabled)
                return "invisible (Canvas " + canvases[i].name + " disabled)";
        }

        float canvasGroupAlpha = 1f;
        string canvasGroupTrace = string.Empty;
        Transform node = text.transform;
        while (node != null)
        {
            CanvasGroup[] groups = node.GetComponents<CanvasGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].enabled)
                {
                    canvasGroupAlpha *= groups[i].alpha;
                    canvasGroupTrace += node.name + "=" +
                                        groups[i].alpha.ToString("0.###") + ";";
                    if (groups[i].ignoreParentGroups)
                    {
                        node = null;
                        break;
                    }
                }
            }

            if (node != null) node = node.parent;
        }

        if (canvasGroupAlpha <= 0.01f)
            return "invisible (CanvasGroup alpha=" + canvasGroupAlpha +
                   "; " + canvasGroupTrace + ")";

        if (text.canvasRenderer != null && text.canvasRenderer.GetAlpha() <= 0.001f)
            return "invisible (CanvasRenderer alpha is zero)";

        if (text.canvasRenderer != null && text.canvasRenderer.cull)
            return "invisible (CanvasRenderer culled)";

        Canvas textCanvas = text.GetComponentInParent<Canvas>(true);
        string sortingTrace = textCanvas != null
            ? ", textCanvasSorting=" + textCanvas.sortingLayerName + "/" +
              textCanvas.sortingOrder + ", overrideSorting=" + textCanvas.overrideSorting
            : ", overlayCanvas=<none>";

        string higherCanvasTrace = DescribeHigherCanvases(textCanvas);
        return "renderable (path=" + GetHierarchyPath(text.transform) +
               ", lossyScale=" + scale + ", colorAlpha=" + text.color.a +
               ", rendererAlpha=" + (text.canvasRenderer != null
                   ? text.canvasRenderer.GetAlpha().ToString("0.###")
                   : "unavailable") + ", groups=" +
               (string.IsNullOrEmpty(canvasGroupTrace) ? "none" : canvasGroupTrace) +
               sortingTrace + ", higherActiveCanvases=" + higherCanvasTrace + ")";
    }

    private static string DescribeHigherCanvases(Canvas target)
    {
        if (target == null) return "unknown (no isolated text Canvas)";

        int targetLayer = SortingLayer.GetLayerValueFromID(target.sortingLayerID);
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas candidate = canvases[i];
            if (candidate == null || candidate == target || !candidate.enabled ||
                !candidate.gameObject.activeInHierarchy)
                continue;

            int candidateLayer = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
            bool drawsAfter = candidateLayer > targetLayer ||
                              (candidateLayer == targetLayer &&
                               candidate.sortingOrder > target.sortingOrder);
            if (!drawsAfter) continue;

            if (result.Length > 0) result.Append("; ");
            Graphic[] visibleGraphics = candidate.GetComponentsInChildren<Graphic>(false);
            System.Text.StringBuilder graphicTrace = new System.Text.StringBuilder();
            for (int g = 0; g < visibleGraphics.Length; g++)
            {
                Graphic graphic = visibleGraphics[g];
                if (graphic == null || !graphic.enabled ||
                    !graphic.gameObject.activeInHierarchy || graphic.color.a <= 0.01f)
                    continue;
                if (graphicTrace.Length > 0) graphicTrace.Append(',');
                graphicTrace.Append(graphic.gameObject.name).Append(" alpha=")
                    .Append(graphic.color.a.ToString("0.##"));
            }

            result.Append(candidate.name).Append("(")
                .Append(candidate.sortingLayerName).Append('/')
                .Append(candidate.sortingOrder).Append(", mode=")
                .Append(candidate.renderMode).Append(", graphics=")
                .Append(graphicTrace.Length == 0 ? "none" : graphicTrace.ToString())
                .Append(')');
        }

        return result.Length == 0 ? "none" : result.ToString();
    }

    private static string DescribeText(TextMeshProUGUI text, string status)
    {
        if (text == null) return "text=<unresolved>";
        return text.gameObject.name + "#" + text.GetEntityId() + "=" + status +
               " (scene=" + text.gameObject.scene.name +
               ", path=" + GetHierarchyPath(text.transform) + ")";
    }

    private void EnsureTextRenderable(TextMeshProUGUI text)
    {
        if (text == null) return;

        if (!text.enabled) text.enabled = true;

        Color graphicColor = text.color;
        if (graphicColor.a <= 0.001f)
        {
            text.color = new Color(
                graphicColor.r, graphicColor.g, graphicColor.b, 1f);
            Debug.LogWarning(
                "[ObjectiveAndDayOverlay] Restored zero Graphic alpha for " +
                GetHierarchyPath(text.transform) + ".",
                text
            );
        }

        if (text.canvasRenderer != null &&
            text.canvasRenderer.GetAlpha() <= 0.001f)
        {
            text.canvasRenderer.SetAlpha(1f);
            Debug.LogWarning(
                "[ObjectiveAndDayOverlay] Restored zero CanvasRenderer alpha for " +
                GetHierarchyPath(text.transform) + ".",
                text
            );
        }

        Transform node = text.transform;
        while (node != null)
        {
            CanvasGroup[] groups = node.GetComponents<CanvasGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null || !groups[i].enabled) continue;

                if (groups[i].alpha <= 0.001f)
                {
                    groups[i].alpha = 1f;
                    Debug.LogWarning(
                        "[ObjectiveAndDayOverlay] Restored zero-alpha CanvasGroup " +
                        node.name + " for " + GetHierarchyPath(text.transform) + ".",
                        groups[i]
                    );
                }

                if (groups[i].ignoreParentGroups) return;
            }

            node = node.parent;
        }
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null) return "<null>";

        string path = target.name;
        Transform parent = target.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private void LogInitialization()
    {
        if (!debugVisibility || hasLoggedInitialization) return;
        if (dayText == null || objectiveText == null ||
            GameStateManager.Instance == null)
        {
            return;
        }

        hasLoggedInitialization = true;
        int day = GameStateManager.Instance != null
            ? GameStateManager.Instance.GetCurrentDay()
            : 1;

        Debug.Log(
            "[ObjectiveAndDayOverlay] Initialized: day=" + day +
            ", DayText='" + (dayText != null ? dayText.text : "<unresolved>") +
            "', ObjectiveText='" +
            (objectiveText != null ? objectiveText.text : "<unresolved>") +
            "', scene=" + SceneManager.GetActiveScene().name + ".",
            this
        );
    }


    private void SetActiveIfChanged(TextMeshProUGUI text, bool active)
    {
        if (text != null && text.gameObject.activeSelf != active)
        {
            text.gameObject.SetActive(active);
            if (debugVisibility)
            {
                Debug.Log(
                    "[ObjectiveAndDayOverlay] Toggled " + text.gameObject.name +
                    " activeSelf=" + text.gameObject.activeSelf +
                    "; render check: " + ActualVisibility(text) +
                    " path=" + GetHierarchyPath(text.transform) +
                    " (scene=" + text.gameObject.scene.name + ").",
                    text
                );
            }
        }
    }


    // ============================================================
    // TEXT RESOLUTION
    // ============================================================

    private void ResolveTexts()
    {
        PersistentGameplayUI persistentUI = PersistentGameplayUI.Instance;

        // Always prefer the DayText that belongs to the persistent gameplay
        // UI. Do not keep a cached scene-local reference across scene loads.
        TextMeshProUGUI persistentDay = persistentUI != null
            ? persistentUI.FindText(dayTextObjectName) as TextMeshProUGUI
            : null;
        if (persistentDay != null)
            dayText = persistentDay;
        else if (dayText == null || !dayText.gameObject.scene.IsValid())
            dayText = FindNamedTextAnywhere(dayTextObjectName);

        if (objectiveText == null || !objectiveText.gameObject.scene.IsValid())
        {
            objectiveText = persistentUI != null
                ? persistentUI.FindText(objectiveTextObjectName) as TextMeshProUGUI
                : null;
            if (objectiveText == null)
                objectiveText = FindNamedText(objectiveTextObjectName);
        }

        if (objectiveText != null)
            objectiveText.richText = true;
    }

    private static TextMeshProUGUI FindNamedTextAnywhere(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return null;
        TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI candidate = texts[i];
            if (candidate != null && candidate.gameObject.scene.IsValid() &&
                candidate.gameObject.name == objectName)
                return candidate;
        }
        return null;
    }

    private static void ActivateHierarchy(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static void EnsureTextCanvasesEnabled(TextMeshProUGUI target)
    {
        if (target == null) return;
        Canvas[] canvases = target.GetComponentsInParent<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null) continue;

            if (canvas.isRootCanvas &&
                canvas.renderMode != RenderMode.WorldSpace &&
                (Mathf.Abs(canvas.transform.localScale.x) < 0.0001f ||
                 Mathf.Abs(canvas.transform.localScale.y) < 0.0001f ||
                 Mathf.Abs(canvas.transform.localScale.z) < 0.0001f))
            {
                Debug.LogWarning(
                    "[ObjectiveAndDayOverlay] Canvas " + canvas.name +
                    " had zero root scale; restoring it for " +
                    GetHierarchyPath(target.transform) + ".",
                    canvas
                );
                canvas.transform.localScale = Vector3.one;
            }

            if (!canvas.enabled) canvas.enabled = true;
        }
    }


    private TextMeshProUGUI FindNamedText(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == objectName)
            {
                return children[i].GetComponent<TextMeshProUGUI>();
            }
        }

        return null;
    }


    // ============================================================
    // GAME STATE EVENTS
    // ============================================================

    private void Subscribe()
    {
        GameStateManager current = GameStateManager.Instance;
        if (current == null || current == subscribedGameState) return;

        if (subscribedGameState != null)
        {
            subscribedGameState.OnGameModeChanged -= HandleGameModeChanged;
            subscribedGameState.OnDayChanged -= HandleDayChanged;
            subscribedGameState.OnObjectiveChanged -= HandleObjectiveChanged;
            subscribedGameState.OnCaseStarted -= HandleCaseStarted;
            subscribedGameState.OnCaseCompleted -= HandleCaseCompleted;
        }

        subscribedGameState = current;
        subscribedGameState.OnGameModeChanged += HandleGameModeChanged;
        subscribedGameState.OnDayChanged += HandleDayChanged;
        subscribedGameState.OnObjectiveChanged += HandleObjectiveChanged;
        subscribedGameState.OnCaseStarted += HandleCaseStarted;
        subscribedGameState.OnCaseCompleted += HandleCaseCompleted;
    }


    private void Unsubscribe()
    {
        if (subscribedGameState == null) return;
        subscribedGameState.OnGameModeChanged -= HandleGameModeChanged;
        subscribedGameState.OnDayChanged -= HandleDayChanged;
        subscribedGameState.OnObjectiveChanged -= HandleObjectiveChanged;
        subscribedGameState.OnCaseStarted -= HandleCaseStarted;
        subscribedGameState.OnCaseCompleted -= HandleCaseCompleted;
        subscribedGameState = null;
    }


    private void HandleGameModeChanged(GameStateManager.GameMode mode)
    {
        if (mode == GameStateManager.GameMode.Case)
        {
            // Case entry restores the overlay after any theater/cutscene.
            explicitVisibility = true;
            InvalidateBindingCache();
            ActivateOverlayHierarchy();
            RefreshAll();
        }

        ApplyVisibility();
    }


    private void HandleDayChanged(int day)
    {
        RefreshDay(day);
        ApplyVisibility();
    }


    private void HandleObjectiveChanged(GameStateManager.ObjectiveState state)
    {
        InvalidateBindingCache();
        RefreshObjectives(state);
        ApplyVisibility();
    }


    private void HandleCaseStarted()
    {
        InvalidateBindingCache();

        RefreshObjectives(
            GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentObjective()
                : GameStateManager.ObjectiveState.FindObject
        );

        ApplyVisibility();
    }


    private void HandleCaseCompleted()
    {
        RefreshObjectives(GameStateManager.ObjectiveState.Complete);
        ApplyVisibility();
    }


    // ============================================================
    // REFRESH (text content only - visibility is ApplyVisibility's job)
    // ============================================================

    private void RefreshAll()
    {
        ResolveTexts();

        int day = GameStateManager.Instance != null
            ? GameStateManager.Instance.GetCurrentDay()
            : 1;

        RefreshDay(day);

        RefreshObjectives(
            GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentObjective()
                : GameStateManager.ObjectiveState.NotStarted
        );
    }


    private void RefreshDay(int day)
    {
        if (dayText == null)
        {
            return;
        }

        string value = string.Format(dayFormat, Mathf.Max(1, day));

        if (dayText.text != value)
        {
            dayText.text = value;
            if (debugVisibility)
            {
                Debug.Log(
                    "[ObjectiveAndDayOverlay] DayText set to '" + value +
                    "' (day " + Mathf.Max(1, day) + ").",
                    this
                );
            }
        }
    }


    private void RefreshObjectives(GameStateManager.ObjectiveState state)
    {
        if (objectiveText == null)
        {
            return;
        }

        bool initialLevelObjective =
            state == GameStateManager.ObjectiveState.NotStarted &&
            GameStateManager.Instance != null &&
            GameStateManager.Instance.GetCurrentCase() >= 0 &&
            IsCaseLevelScene(SceneManager.GetActiveScene().name);

        if (state == GameStateManager.ObjectiveState.NotStarted &&
            !initialLevelObjective)
        {
            if (!string.IsNullOrEmpty(objectiveText.text))
            {
                objectiveText.text = string.Empty;
            }

            return;
        }

        objectiveText.richText = true;

        if (initialLevelObjective)
        {
            state = GameStateManager.ObjectiveState.FindObject;
        }

        string objectName = "object";
        string bossName = "boss";

        if (CaseManager.Instance != null)
        {
            CaseManager.CaseData data = CaseManager.Instance.GetCurrentCase();

            if (data != null)
            {
                if (!string.IsNullOrWhiteSpace(data.objectName))
                {
                    objectName = data.objectName;
                }

                if (!string.IsNullOrWhiteSpace(data.bossName))
                {
                    bossName = data.bossName;
                }
            }
        }

        string findLine = string.Format(findObjectFormat, objectName);
        string bossLine = string.Format(defeatBossFormat, bossName);

        GameStateManager gameState = GameStateManager.Instance;
        bool findDone = gameState != null
            ? gameState.IsObjectFound()
            : state == GameStateManager.ObjectiveState.BossUnlocked ||
              state == GameStateManager.ObjectiveState.BossDefeated ||
              state == GameStateManager.ObjectiveState.Complete;

        bool bossDone = gameState != null
            ? gameState.IsBossDefeated()
            : state == GameStateManager.ObjectiveState.BossDefeated ||
              state == GameStateManager.ObjectiveState.Complete;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        builder.AppendLine(StrikeIf(findLine, findDone));
        builder.Append(StrikeIf(bossLine, bossDone));

        if (bossDone)
        {
            builder.AppendLine();
            builder.Append(
                StrikeIf(
                    returnFormat,
                    state == GameStateManager.ObjectiveState.Complete
                )
            );
        }

        string result = builder.ToString().TrimEnd();

        if (objectiveText.text != result)
        {
            objectiveText.text = result;
        }
    }


    private static string StrikeIf(string text, bool struck)
    {
        return struck ? "<s>" + text + "</s>" : text;
    }
}
