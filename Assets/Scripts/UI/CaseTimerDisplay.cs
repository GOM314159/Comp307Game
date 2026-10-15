using TMPro;
using UnityEngine;

/// <summary>Displays the active case timer without toggling the shared HUD root.</summary>
public class CaseTimerDisplay : MonoBehaviour
{
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private bool hideWhenTimerStopped = true;
    [SerializeField] private bool debugTimerVisibility = true;

    private bool hasLoggedVisibility;
    private bool lastVisible;

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponentInChildren<TMP_Text>(true);

        // This scene assigns ObjectiveAndDayOverlay as timerRoot. It also
        // contains DayText, ObjectiveText, reward text, and other HUD items,
        // so only toggle Timer_Text itself.
        Hide();
    }

    private void Update()
    {
        CaseManager caseManager = CaseManager.Instance;
        GameStateManager state = GameStateManager.Instance;
        bool gameplayCaseMode = state != null &&
            (state.GetGameMode() == GameStateManager.GameMode.Case ||
             state.GetGameMode() == GameStateManager.GameMode.BossFight ||
             state.GetGameMode() == GameStateManager.GameMode.Paused);
        bool timerActive = caseManager != null && caseManager.HasTimer() &&
                           (!hideWhenTimerStopped || caseManager.IsTimerRunning());
        bool visible = gameplayCaseMode && timerActive;

        if (!visible)
        {
            Hide();
            LogVisibility(false, caseManager, state, gameplayCaseMode);
            return;
        }

        if (timerText != null)
        {
            if (!timerText.gameObject.activeSelf)
                timerText.gameObject.SetActive(true);

            float remaining = Mathf.Max(0f, caseManager.GetTimeRemaining());
            int seconds = Mathf.CeilToInt(remaining);
            timerText.text = (seconds / 60).ToString("00") + ":" +
                             (seconds % 60).ToString("00");
        }

        LogVisibility(true, caseManager, state, gameplayCaseMode);
    }

    private void Hide()
    {
        if (timerText != null && timerText.gameObject.activeSelf)
            timerText.gameObject.SetActive(false);
    }

    private void LogVisibility(bool visible, CaseManager caseManager,
        GameStateManager state, bool gameplayCaseMode)
    {
        if (!debugTimerVisibility || (hasLoggedVisibility && lastVisible == visible))
            return;

        hasLoggedVisibility = true;
        lastVisible = visible;
        Debug.Log("[CaseTimerDisplay] Timer_Text " +
                  (visible ? "visible" : "invisible") +
                  "; gameplayCaseMode=" + gameplayCaseMode +
                  ", hasTimer=" + (caseManager != null && caseManager.HasTimer()) +
                  ", timerRunning=" + (caseManager != null && caseManager.IsTimerRunning()) +
                  ", mode=" + (state != null ? state.GetGameMode().ToString() : "Unavailable") +
                  ", sharedTimerRoot=" + (timerRoot != null ? timerRoot.name : "none") +
                  " (root is not toggled).", this);
    }
}
