using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// HUD overlay with two independent trackers:
///
///   DAY tracker       - visible in EVERY scene and on EVERY day (lobby,
///                       level 1/2/3, boss fight). It is only hidden for
///                       menus, cutscenes, plays, credits, pause and loading.
///
///   OBJECTIVE tracker - visible ONLY while a case is running AND the
///                       currently loaded scene contains the
///                       CaseSceneBinding of that case (i.e. the level the
///                       objective belongs to). It is never shown in the
///                       lobby.
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


    // Explicit visibility requested by menus/cutscenes.
    private bool explicitVisibility = true;
    private GameStateManager subscribedGameState;

    // Cache for "does the loaded scene belong to the active case?"
    private ulong bindingCacheSceneHandle = ulong.MaxValue;
    private int bindingCacheCaseID = int.MinValue;
    private bool bindingCacheResult;
    private float bindingCacheTime;
    private bool sceneListenerAdded;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        ResolveTexts();
        RefreshAll();
        ApplyVisibility();
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
    }


    private void Start()
    {
        Subscribe();
        RefreshAll();
        ApplyVisibility();
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


    /// <summary>Day tracker rule: always on during normal gameplay.</summary>
    private bool ShouldShowDay()
    {
        // The loading screen always wins: all GUI is hidden while it is up.
        if (LoadingScreenManager.Instance != null &&
            LoadingScreenManager.Instance.AreGameplayCanvasesHidden)
        {
            return false;
        }

        if (!explicitVisibility)
        {
            return false;
        }

        // Keep the day counter independent from temporary gameplay states
        // such as the gacha reward message, plays, and pause/time-scale
        // changes. Only menu/loading/credits/cutscene modes hide the HUD.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.GameMode mode = GameStateManager.Instance.GetGameMode();
            if (mode == GameStateManager.GameMode.Loading ||
                mode == GameStateManager.GameMode.MainMenu ||
                mode == GameStateManager.GameMode.Credits ||
                mode == GameStateManager.GameMode.Cutscene)
            {
                return false;
            }
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

        if (state.GetCurrentObjective() ==
            GameStateManager.ObjectiveState.NotStarted)
        {
            return false;
        }

        GameStateManager.GameMode mode = state.GetGameMode();

        if (mode != GameStateManager.GameMode.Case &&
            mode != GameStateManager.GameMode.BossFight)
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

        if (!requireCaseSceneBinding)
            return true;

        return IsActiveCaseMappedToLoadedScene(caseID) ||
               !HasAnyCaseSceneBinding();
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
        bool showObjective = showDay && ShouldShowObjective();

        if (showDay)
        {
            ActivateOverlayHierarchy();
            EnsureCanvasesEnabled();
        }

        SetActiveIfChanged(dayText, showDay);
        SetActiveIfChanged(objectiveText, showObjective);
    }


    private static void SetActiveIfChanged(TextMeshProUGUI text, bool active)
    {
        if (text != null && text.gameObject.activeSelf != active)
        {
            text.gameObject.SetActive(active);
        }
    }


    // ============================================================
    // TEXT RESOLUTION
    // ============================================================

    private void ResolveTexts()
    {
        PersistentGameplayUI persistentUI = PersistentGameplayUI.Instance;

        if (dayText == null)
        {
            dayText = persistentUI != null
                ? persistentUI.FindText(dayTextObjectName) as TextMeshProUGUI
                : FindNamedText(dayTextObjectName);
        }

        if (objectiveText == null)
        {
            objectiveText = persistentUI != null
                ? persistentUI.FindText(objectiveTextObjectName) as TextMeshProUGUI
                : FindNamedText(objectiveTextObjectName);
        }

        if (objectiveText != null)
        {
            objectiveText.richText = true;
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
        }
    }


    private void RefreshObjectives(GameStateManager.ObjectiveState state)
    {
        if (objectiveText == null)
        {
            return;
        }

        if (state == GameStateManager.ObjectiveState.NotStarted)
        {
            if (!string.IsNullOrEmpty(objectiveText.text))
            {
                objectiveText.text = string.Empty;
            }

            return;
        }

        objectiveText.richText = true;

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

        bool findDone =
            state == GameStateManager.ObjectiveState.BossUnlocked ||
            state == GameStateManager.ObjectiveState.BossDefeated ||
            state == GameStateManager.ObjectiveState.Complete;

        bool bossDone =
            state == GameStateManager.ObjectiveState.BossDefeated ||
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
