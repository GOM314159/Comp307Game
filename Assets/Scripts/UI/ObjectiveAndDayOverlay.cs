using TMPro;
using UnityEngine;


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


    // Explicit visibility requested by menus/cutscenes.
    // This is separate from Time.timeScale so a cutscene can hide
    // the overlay even if the cutscene does not pause the game.
    private bool explicitVisibility = true;


    private void Awake()
    {
        ResolveTexts();
        RefreshAll();
        ApplyVisibility();
    }


    private void OnEnable()
    {
        Subscribe();
        RefreshAll();
        ApplyVisibility();
    }


    private void Start()
    {
        // Managers may boot after this UI on the first frame.
        Subscribe();
        RefreshAll();
        ApplyVisibility();
    }


    private void Update()
    {
        // Scene loads and UI activation order can temporarily leave the
        // text references unresolved. Retry until both are available.
        if (dayText == null || objectiveText == null)
        {
            ResolveTexts();
            RefreshAll();
        }

        // This catches pause/unpause without requiring the pause system
        // to know anything about this UI.
        ApplyVisibility();
    }


    public void RefreshNow()
    {
        ResolveTexts();
        Subscribe();
        RefreshAll();
        ApplyVisibility();
    }


    private void OnDisable()
    {
        Unsubscribe();
    }


    // ============================================================
    // VISIBILITY
    // ============================================================

    /// <summary>
    /// Shows or hides both the Day Tracker and Objective Tracker.
    ///
    /// Menus and cutscenes can call:
    ///
    /// ObjectiveAndDayOverlay.SetOverlayVisible(false);
    ///
    /// and later:
    ///
    /// ObjectiveAndDayOverlay.SetOverlayVisible(true);
    /// </summary>
    public void SetOverlayVisible(bool visible)
    {
        explicitVisibility = visible;

        // A scene transition can leave the Canvas or one of its parent UI
        // roots inactive. Updating the TMP objects alone is not enough; the
        // hierarchy itself must be active before the objective can be seen.
        if (visible)
        {
            ActivateOverlayHierarchy();
        }

        ApplyVisibility();
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
    /// Hides both trackers.
    /// Useful to call when starting a menu or cutscene.
    /// </summary>
    public void HideOverlay()
    {
        SetOverlayVisible(false);
    }


    /// <summary>
    /// Shows both trackers again.
    /// Useful to call when returning to normal gameplay.
    /// </summary>
    public void ShowOverlay()
    {
        SetOverlayVisible(true);
    }


    private void ApplyVisibility()
    {
        if (dayText == null && objectiveText == null)
        {
            return;
        }

        bool shouldBeVisible = explicitVisibility;

        // Menus, pause screens, cutscenes, credits and plays are not
        // gameplay HUD states. Hide the overlay automatically so individual
        // menu/cutscene scripts do not have to remember to toggle it.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.GameMode mode =
                GameStateManager.Instance.GetGameMode();

            if (mode == GameStateManager.GameMode.Loading ||
                mode == GameStateManager.GameMode.MainMenu ||
                mode == GameStateManager.GameMode.WatchingPlay ||
                mode == GameStateManager.GameMode.Cutscene ||
                mode == GameStateManager.GameMode.Credits ||
                mode == GameStateManager.GameMode.Paused)
            {
                shouldBeVisible = false;
            }
        }

        // Pause automatically hides the trackers as a fallback for pause
        // states where the game mode has not switched yet.
        if (hideWhilePaused && Time.timeScale <= 0f)
        {
            shouldBeVisible = false;
        }

        if (shouldBeVisible)
        {
            ActivateOverlayHierarchy();
        }

        if (dayText != null)
        {
            dayText.gameObject.SetActive(shouldBeVisible);
        }

        if (objectiveText != null)
        {
            // Objective visibility also depends on whether an objective
            // currently exists. We therefore only force it on when the
            // overlay itself is allowed to be visible.
            if (!shouldBeVisible)
            {
                objectiveText.gameObject.SetActive(false);
            }
            else
            {
                RefreshObjectives(
                    GameStateManager.Instance != null
                        ? GameStateManager.Instance.GetCurrentObjective()
                        : GameStateManager.ObjectiveState.NotStarted
                );
            }
        }
    }


    // ============================================================
    // TEXT RESOLUTION
    // ============================================================

    private void ResolveTexts()
    {
        if (dayText == null)
        {
            dayText = FindNamedText(dayTextObjectName);
        }

        if (objectiveText == null)
        {
            objectiveText = FindNamedText(objectiveTextObjectName);
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

        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null &&
                children[i].name == objectName)
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
        if (GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.Instance.OnDayChanged -= HandleDayChanged;
        GameStateManager.Instance.OnDayChanged += HandleDayChanged;

        GameStateManager.Instance.OnObjectiveChanged -= HandleObjectiveChanged;
        GameStateManager.Instance.OnObjectiveChanged += HandleObjectiveChanged;

        GameStateManager.Instance.OnCaseStarted -= HandleCaseStarted;
        GameStateManager.Instance.OnCaseStarted += HandleCaseStarted;

        GameStateManager.Instance.OnCaseCompleted -= HandleCaseCompleted;
        GameStateManager.Instance.OnCaseCompleted += HandleCaseCompleted;
    }


    private void Unsubscribe()
    {
        if (GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.Instance.OnDayChanged -= HandleDayChanged;
        GameStateManager.Instance.OnObjectiveChanged -= HandleObjectiveChanged;
        GameStateManager.Instance.OnCaseStarted -= HandleCaseStarted;
        GameStateManager.Instance.OnCaseCompleted -= HandleCaseCompleted;
    }


    private void HandleDayChanged(int day)
    {
        RefreshDay(day);
        ApplyVisibility();
    }


    private void HandleObjectiveChanged(
        GameStateManager.ObjectiveState state)
    {
        RefreshObjectives(state);
        ApplyVisibility();
    }


    private void HandleCaseStarted()
    {
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
    // REFRESH
    // ============================================================

    private void RefreshAll()
    {
        ResolveTexts();

        int day = GameStateManager.Instance != null
            ? GameStateManager.Instance.GetCurrentDay()
            : 1;

        RefreshDay(day);

        GameStateManager.ObjectiveState objective =
            GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentObjective()
                : GameStateManager.ObjectiveState.NotStarted;

        RefreshObjectives(objective);
    }


    private void RefreshDay(int day)
    {
        if (dayText == null)
        {
            return;
        }

        dayText.text =
            string.Format(dayFormat, Mathf.Max(1, day));
    }


    private void RefreshObjectives(
        GameStateManager.ObjectiveState state)
    {
        if (objectiveText == null)
        {
            return;
        }

        bool caseActive =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsCaseActive();

        if ((!caseActive &&
             state != GameStateManager.ObjectiveState.Complete) ||
            state == GameStateManager.ObjectiveState.NotStarted)
        {
            objectiveText.text = string.Empty;
            objectiveText.gameObject.SetActive(false);
            return;
        }

        // Do not allow this method to override a menu/cutscene hiding
        // the entire overlay.
        bool overlayAllowed =
            explicitVisibility &&
            (!hideWhilePaused || Time.timeScale > 0f);

        if (!overlayAllowed)
        {
            objectiveText.gameObject.SetActive(false);
            return;
        }

        objectiveText.gameObject.SetActive(true);
        objectiveText.richText = true;

        string objectName = "object";
        string bossName = "boss";

        if (CaseManager.Instance != null)
        {
            CaseManager.CaseData data =
                CaseManager.Instance.GetCurrentCase();

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

        string findLine =
            string.Format(findObjectFormat, objectName);

        string bossLine =
            string.Format(defeatBossFormat, bossName);

        bool findDone =
            state == GameStateManager.ObjectiveState.BossUnlocked ||
            state == GameStateManager.ObjectiveState.BossDefeated ||
            state == GameStateManager.ObjectiveState.Complete;

        bool bossDone =
            state == GameStateManager.ObjectiveState.BossDefeated ||
            state == GameStateManager.ObjectiveState.Complete;

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        builder.AppendLine(
            StrikeIf(findLine, findDone)
        );

        builder.Append(
            StrikeIf(bossLine, bossDone)
        );

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

        objectiveText.text =
            builder.ToString().TrimEnd();
    }


    private static string StrikeIf(string text, bool struck)
    {
        if (!struck)
        {
            return text;
        }

        // TextMeshPro supports the <s> rich-text tag for a true
        // strikethrough. Keep the tag around the complete line so the
        // completed objective is visibly crossed out rather than removed.
        return "<s>" + text + "</s>";
    }
}

