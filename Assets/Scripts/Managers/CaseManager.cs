using UnityEngine;
using System;
using System.Collections.Generic;

public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance { get; private set; }


    // =========================================================
    // OBJECTIVE TYPES
    // =========================================================

    public enum CaseObjective
    {
        FindObject,
        KillBoss
    }


    // =========================================================
    // CASE DATA
    // =========================================================

    [Serializable]
    public class CaseData
    {
        [Header("Case Information")]

        public int caseID;

        public string caseName;

        [TextArea(2, 5)]
        public string description;


        // =========================================================
        // TIMER
        // =========================================================

        [Header("Timer")]

        public bool hasTimer = false;

        [Min(1f)]
        public float timeLimit = 300f;


        // =========================================================
        // MAIN OBJECTIVE
        // =========================================================

        [Header("Main Objective")]

        [Tooltip(
            "The world interactable that completes the " +
            "main objective for this case.\n\n" +
            "Example:\n" +
            "Day 1 = Axe\n" +
            "Day 2 = Cable Cutter\n" +
            "Day 3 = Gun"
        )]
        public WorldInteractable requiredObject;

        [Tooltip("Display name for the required object.")]
        public string objectName = "Required Object";


        // =========================================================
        // BOSS
        // =========================================================

        [Header("Boss")]

        public string bossName = "Case Boss";


        // =========================================================
        // SIDE QUEST
        // =========================================================

        [Header("Side Quest")]

        public bool hasSideQuest = true;

        [Tooltip(
            "The world interactable that completes the side quest " +
            "for this case.\n\n" +
            "Example:\n" +
            "Day 1 = White Powder\n" +
            "Day 2 = Blue Tablet\n" +
            "Day 3 = White Tablet"
        )]
        public WorldInteractable sideQuestItem;

        public string sideQuestName;


        // =========================================================
        // COMPLETION
        // =========================================================

        [Header("Completion")]

        public bool requiredForDayCompletion = true;
    }


    // =========================================================
    // CASE DATABASE
    // =========================================================

    [Header("Cases")]

    [SerializeField]
    private List<CaseData> cases =
        new List<CaseData>();


    // =========================================================
    // CURRENT CASE
    // =========================================================

    [Header("Current Case")]

    [SerializeField]
    private CaseData currentCase;

    [SerializeField]
    private int currentCaseIndex = -1;


    // =========================================================
    // CASE STATE
    // =========================================================

    [Header("Case State")]

    [SerializeField]
    private bool caseActive = false;

    [SerializeField]
    private bool objectFound = false;

    [SerializeField]
    private bool bossUnlocked = false;

    [SerializeField]
    private bool bossDefeated = false;

    [SerializeField]
    private bool sideQuestComplete = false;

    [SerializeField]
    private bool caseComplete = false;


    // =========================================================
    // TIMER
    // =========================================================

    [Header("Timer")]

    [SerializeField]
    private float timeRemaining = 0f;

    [SerializeField]
    private bool timerRunning = false;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnCaseStarted;

    public event Action OnObjectFound;

    public event Action OnBossUnlocked;

    public event Action OnBossDefeated;

    public event Action OnSideQuestCompleted;

    public event Action OnCaseCompleted;

    public event Action OnTimerStarted;

    public event Action OnTimerExpired;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugCaseManager = true;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    private void Update()
    {
        if (!timerRunning)
        {
            return;
        }

        UpdateTimer();
    }

    // =========================================================
    // RESET FOR DAY
    // =========================================================

    public void ResetForDay()
    {
        ResetCaseState();

        currentCaseIndex = -1;

        currentCase = null;

        DebugCase(
            "Case Manager reset for current day."
        );
    }


    // =========================================================
    // RESET FOR ENTIRE GAME
    // =========================================================

    public void ResetForGame()
    {
        ResetCaseState();

        currentCaseIndex = -1;

        currentCase = null;

        DebugCase(
            "Case Manager reset for entire game."
        );
    }

    // =========================================================
    // START CASE
    // =========================================================

    public bool StartCase(int caseID)
    {
        CaseData selectedCase =
            GetCase(caseID);

        if (selectedCase == null)
        {
            Debug.LogError(
                "[Case] Case ID "
                + caseID
                + " does not exist."
            );

            return false;
        }

        currentCase = selectedCase;

        currentCaseIndex =
            cases.IndexOf(selectedCase);

        ResetCaseState();

        caseActive = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetCase(caseID);

            GameStateManager.Instance.StartCase();
        }

        DebugCase(
            "Started Case "
            + caseID
            + ": "
            + selectedCase.caseName
        );

        if (selectedCase.hasTimer)
        {
            StartTimer(
                selectedCase.timeLimit
            );
        }

        OnCaseStarted?.Invoke();

        return true;
    }


    // =========================================================
    // FIND CASE
    // =========================================================

    public CaseData GetCase(int caseID)
    {
        foreach (CaseData caseData in cases)
        {
            if (caseData == null)
            {
                continue;
            }

            if (caseData.caseID == caseID)
            {
                return caseData;
            }
        }

        return null;
    }


    public CaseData GetCurrentCase()
    {
        return currentCase;
    }


    public int GetCurrentCaseID()
    {
        if (currentCase == null)
        {
            return -1;
        }

        return currentCase.caseID;
    }


    // =========================================================
    // OBJECTIVE 1
    // FIND OBJECT
    // =========================================================

    public bool CanFindObject()
    {
        if (!caseActive)
        {
            return false;
        }

        if (objectFound)
        {
            return false;
        }

        return true;
    }

    public bool TryRegisterRequiredObject(
    WorldInteractable pickedUpObject)
    {
        if (!caseActive)
        {
            DebugCase(
                "Required object pickup ignored. " +
                "No case is active."
            );

            return false;
        }

        if (pickedUpObject == null)
        {
            Debug.LogWarning(
                "[Case Manager] Required object was null."
            );

            return false;
        }

        if (currentCase == null)
        {
            Debug.LogWarning(
                "[Case Manager] No current case."
            );

            return false;
        }

        // ---------------------------------------------------------
        // MAKE SURE THIS IS THE CORRECT CASE ITEM
        // ---------------------------------------------------------

        if (currentCase.requiredObject != pickedUpObject)
        {
            Debug.LogWarning(
                "[Case Manager] "
                + pickedUpObject.gameObject.name
                + " was picked up, but it is NOT the required "
                + "object for Case "
                + currentCase.caseID
                + "."
            );

            return false;
        }

        // ---------------------------------------------------------
        // ALREADY FOUND
        // ---------------------------------------------------------

        if (objectFound)
        {
            return false;
        }

        // ---------------------------------------------------------
        // REGISTER
        // ---------------------------------------------------------

        objectFound = true;
        bossUnlocked = true;

        // ---------------------------------------------------------
        // GAME STATE
        // ---------------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FindObject();
        }

        // ---------------------------------------------------------
        // EVENTS
        // ---------------------------------------------------------

        OnObjectFound?.Invoke();
        OnBossUnlocked?.Invoke();

        DebugCase(
            "Required object registered: "
            + pickedUpObject.gameObject.name
            + ". Boss objective unlocked."
        );

        return true;
    }

    public bool TryRegisterSideQuestItem(
    WorldInteractable pickedUpItem)
    {
        if (!caseActive)
        {
            DebugCase(
                "Side quest pickup ignored. "
                + "No case is active."
            );

            return false;
        }

        if (pickedUpItem == null)
        {
            return false;
        }

        if (currentCase == null)
        {
            return false;
        }

        if (!currentCase.hasSideQuest)
        {
            return false;
        }

        // ---------------------------------------------------------
        // MAKE SURE THIS IS THE CORRECT SIDE QUEST ITEM
        // ---------------------------------------------------------

        if (currentCase.sideQuestItem != pickedUpItem)
        {
            Debug.LogWarning(
                "[Case Manager] "
                + pickedUpItem.gameObject.name
                + " was picked up, but it is NOT the side quest "
                + "item for Case "
                + currentCase.caseID
                + "."
            );

            return false;
        }

        // ---------------------------------------------------------
        // ALREADY COMPLETE
        // ---------------------------------------------------------

        if (sideQuestComplete)
        {
            return false;
        }

        // ---------------------------------------------------------
        // COMPLETE SIDE QUEST
        // ---------------------------------------------------------

        sideQuestComplete = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.CompleteSideQuest();
        }

        DebugCase(
            "Side quest item registered: "
            + pickedUpItem.gameObject.name
        );

        OnSideQuestCompleted?.Invoke();

        return true;
    }
    public void FindRequiredObject()
    {
        if (!CanFindObject())
        {
            return;
        }

        // The required object has been found.
        objectFound = true;

        // Tell GameStateManager to advance the official objective.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FindObject();
        }

        // Keep CaseManager synchronized with GameStateManager.
        bossUnlocked = true;

        DebugCase("Required object found.");
        DebugCase("Boss objective unlocked.");

        OnObjectFound?.Invoke();
        OnBossUnlocked?.Invoke();
    }


    // =========================================================
    // OBJECTIVE 2
    // BOSS
    // =========================================================

    public bool IsBossUnlocked()
    {
        return bossUnlocked;
    }


    public bool CanFightBoss()
    {
        return caseActive &&
               objectFound &&
               bossUnlocked &&
               !bossDefeated;
    }


    public void DefeatBoss()
    {
        if (!CanFightBoss())
        {
            Debug.LogWarning(
                "[Case] Boss cannot be defeated yet."
            );

            return;
        }

        bossDefeated = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .DefeatCaseBoss();
        }

        DebugCase(
            "Case boss defeated."
        );

        OnBossDefeated?.Invoke();

        CheckCaseCompletion();
    }


    // =========================================================
    // SIDE QUEST
    // =========================================================

    public bool HasSideQuest()
    {
        return currentCase != null &&
               currentCase.hasSideQuest;
    }


    public bool IsSideQuestComplete()
    {
        return sideQuestComplete;
    }


    public void CompleteSideQuest()
    {
        if (!caseActive)
        {
            return;
        }

        if (!HasSideQuest())
        {
            return;
        }

        if (sideQuestComplete)
        {
            return;
        }

        sideQuestComplete = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .CompleteSideQuest();
        }

        DebugCase(
            "Side quest completed."
        );

        OnSideQuestCompleted?.Invoke();
    }


    // =========================================================
    // CASE COMPLETION
    // =========================================================

    private void CheckCaseCompletion()
    {
        if (!objectFound)
        {
            return;
        }

        if (!bossDefeated)
        {
            return;
        }

        CompleteCase();
    }


    public void CompleteCase()
    {
        if (caseComplete)
        {
            return;
        }

        caseComplete = true;

        caseActive = false;

        StopTimer();

        DebugCase(
            "Case completed."
        );

        OnCaseCompleted?.Invoke();

        // GameFlowManager handles returning
        // to the lobby / theater progression.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.CaseCompleted();
        }
    }


    // =========================================================
    // TIMER
    // =========================================================

    public void StartTimer(float duration)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning(
                "[Case] Timer duration must be greater than 0."
            );

            return;
        }

        timeRemaining = duration;

        timerRunning = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .StartCaseTimer(duration);
        }

        DebugCase(
            "Case timer started: "
            + duration
            + " seconds."
        );

        OnTimerStarted?.Invoke();
    }


    private void UpdateTimer()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining > 0f)
        {
            return;
        }

        timeRemaining = 0f;

        timerRunning = false;

        DebugCase(
            "Case timer expired."
        );

        OnTimerExpired?.Invoke();

        TimerExpired();
    }


    private void TimerExpired()
    {
        if (!caseActive)
        {
            return;
        }

        caseActive = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.HandleLoss(
                GameStateManager.LossReason.TimerExpired
            );
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TimerExpired();
        }
    }


    public void StopTimer()
    {
        timerRunning = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StopCaseTimer();
        }
    }


    public float GetTimeRemaining()
    {
        return timeRemaining;
    }


    public bool IsTimerRunning()
    {
        return timerRunning;
    }


    public bool HasTimer()
    {
        return currentCase != null &&
               currentCase.hasTimer;
    }


    // =========================================================
    // CASE STATE
    // =========================================================

    public bool IsCaseActive()
    {
        return caseActive;
    }


    public bool IsObjectFound()
    {
        return objectFound;
    }


    public bool IsBossDefeated()
    {
        return bossDefeated;
    }


    public bool IsCaseComplete()
    {
        return caseComplete;
    }


    // =========================================================
    // CURRENT OBJECTIVE
    // =========================================================

    public GameStateManager.ObjectiveState
        GetCurrentObjective()
    {
        if (GameStateManager.Instance == null)
        {
            return GameStateManager.ObjectiveState.NotStarted;
        }

        return GameStateManager.Instance
            .GetCurrentObjective();
    }


    // =========================================================
    // RESET
    // =========================================================

    private void ResetCaseState()
    {
        caseActive = false;

        objectFound = false;

        bossUnlocked = false;

        bossDefeated = false;

        sideQuestComplete = false;

        caseComplete = false;

        timeRemaining = 0f;

        timerRunning = false;
    }


    public void ResetCurrentCase()
    {
        StopTimer();

        ResetCaseState();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .ResetCaseProgress();
        }

        DebugCase(
            "Current case reset."
        );
    }

    // =========================================================
    // DEACTIVATE CASE
    // =========================================================

    public void DeactivateCase()
    {
        if (!caseActive)
        {
            return;
        }

        caseActive = false;

        StopTimer();

        DebugCase(
            "Case deactivated."
        );

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetCaseProgress();

            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
        }
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugCase(string message)
    {
        if (!debugCaseManager)
        {
            return;
        }

        Debug.Log(
            "[Case Manager] "
            + message
        );
    }
}