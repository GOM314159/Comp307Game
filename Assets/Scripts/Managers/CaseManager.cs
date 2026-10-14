using UnityEngine;
using UnityEngine.SceneManagement;
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


        // =====================================================
        // TIMER
        // =====================================================

        [Header("Timer")]

        public bool hasTimer = false;

        [Min(1f)]
        public float timeLimit = 300f;


        // =====================================================
        // MAIN OBJECTIVE
        // =====================================================

        [Header("Main Objective")]

        [Tooltip(
            "Display name for the required object.\n\n" +
            "The actual object reference is assigned in the " +
            "CaseSceneBinding inside the appropriate level scene."
        )]
        public string objectName = "Required Object";


        // =====================================================
        // BOSS
        // =====================================================

        [Header("Boss")]

        public string bossName = "Case Boss";


        // =====================================================
        // SIDE QUEST
        // =====================================================

        [Header("Side Quest")]

        public bool hasSideQuest = true;

        [Tooltip(
            "Display name for the side quest item.\n\n" +
            "The actual object reference is assigned in the " +
            "CaseSceneBinding inside the appropriate level scene."
        )]
        public string sideQuestName;


        // =====================================================
        // COMPLETION
        // =====================================================

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
    // SCENE REFERENCES
    // =========================================================
    //
    // IMPORTANT:
    //
    // These are NOT serialized because the objects belong to
    // scenes that are loaded/unloaded.
    //
    // CaseSceneBinding supplies these references after the
    // appropriate scene loads.
    //
    // =========================================================

    [Header("Runtime Scene References")]

    [SerializeField]
    private WorldInteractable currentRequiredObject;

    [SerializeField]
    private WorldInteractable currentSideQuestItem;


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


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
    // SCENE LOADED
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        RefreshSceneReferences();
    }


    public void RefreshSceneReferences()
    {
        // -----------------------------------------------------
        // CLEAR OLD SCENE REFERENCES
        // -----------------------------------------------------

        currentRequiredObject = null;

        currentSideQuestItem = null;


        // -----------------------------------------------------
        // NO CURRENT CASE
        // -----------------------------------------------------

        if (currentCase == null)
        {
            DebugCase(
                "No current case. Scene references cleared."
            );

            return;
        }


        // -----------------------------------------------------
        // FIND SCENE BINDINGS
        // -----------------------------------------------------

        CaseSceneBinding[] bindings =
            FindObjectsByType<CaseSceneBinding>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        // -----------------------------------------------------
        // FIND THE BINDING FOR THIS CASE
        // -----------------------------------------------------

        CaseSceneBinding matchingBinding = null;

        foreach (
            CaseSceneBinding binding
            in bindings
        )
        {
            if (binding == null)
            {
                continue;
            }

            if (
                binding.GetCaseID()
                == currentCase.caseID
            )
            {
                matchingBinding = binding;
                break;
            }
        }


        // -----------------------------------------------------
        // NO BINDING
        // -----------------------------------------------------

        if (matchingBinding == null)
        {
            DebugCase(
                "No CaseSceneBinding for Case "
                + currentCase.caseID
                + " was found in scene '"
                + SceneManager.GetActiveScene().name
                + "'."
            );

            return;
        }


        // -----------------------------------------------------
        // ASSIGN REQUIRED OBJECT
        // -----------------------------------------------------

        currentRequiredObject =
            matchingBinding.GetRequiredObject();


        // -----------------------------------------------------
        // ASSIGN SIDE QUEST OBJECT
        // -----------------------------------------------------

        currentSideQuestItem =
            matchingBinding.GetSideQuestItem();


        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (currentRequiredObject == null)
        {
            Debug.LogError(
                "[Case Manager] Case "
                + currentCase.caseID
                + " has a CaseSceneBinding, but its " +
                "Required Object is missing."
            );
        }

        if (
            currentCase.hasSideQuest &&
            currentSideQuestItem == null
        )
        {
            Debug.LogError(
                "[Case Manager] Case "
                + currentCase.caseID
                + " has a CaseSceneBinding, but its " +
                "Side Quest Item is missing."
            );
        }


        DebugCase(
            "Scene references refreshed for Case "
            + currentCase.caseID
            + ". Required Object = "
            + GetObjectName(currentRequiredObject)
            + ", Side Quest = "
            + GetObjectName(currentSideQuestItem)
        );
    }


    // =========================================================
    // RESET FOR DAY
    // =========================================================

    public void ResetForDay()
    {
        ResetCaseState();

        currentCaseIndex = -1;

        currentCase = null;

        currentRequiredObject = null;

        currentSideQuestItem = null;

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

        currentRequiredObject = null;

        currentSideQuestItem = null;

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


        // -----------------------------------------------------
        // SELECT CASE
        // -----------------------------------------------------

        currentCase = selectedCase;

        currentCaseIndex =
            cases.IndexOf(selectedCase);


        // -----------------------------------------------------
        // RESET CASE STATE
        // -----------------------------------------------------

        ResetCaseState();


        // -----------------------------------------------------
        // ACTIVATE CASE
        // -----------------------------------------------------

        caseActive = true;


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetCase(caseID);

            GameStateManager.Instance.StartCase();
        }

        // -----------------------------------------------------
        // LEVEL MUSIC
        // -----------------------------------------------------

        // Case IDs 1/2/3 correspond to Levels 1/2/3. The AudioManager
        // owns the looping music source so the track survives scene loads.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelMusic(caseID);
        }


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        DebugCase(
            "Started Case "
            + caseID
            + ": "
            + selectedCase.caseName
        );


        // -----------------------------------------------------
        // TIMER
        // -----------------------------------------------------

        if (selectedCase.hasTimer)
        {
            StartTimer(
                selectedCase.timeLimit
            );
        }


        // -----------------------------------------------------
        // IMPORTANT
        // -----------------------------------------------------
        //
        // We DO NOT try to assign the level objects here.
        //
        // The case scene has not necessarily loaded yet.
        //
        // TeleportManager loads the scene, and OnSceneLoaded
        // calls RefreshSceneReferences().
        //
        // -----------------------------------------------------

        if (GachaManager.Instance != null)
        {
            GachaManager.Instance.BeginLevel();
        }

        OnCaseStarted?.Invoke();

        // Refresh the persistent HUD from the common case-entry point.
        // Day 2/3 may enter through a cross-scene portal, so the overlay
        // cannot rely only on scene-local portal callbacks.
        ObjectiveAndDayOverlay persistentOverlay =
            PersistentGameplayUI.Instance != null
                ? PersistentGameplayUI.Instance.GetObjectiveOverlay()
                : null;
        if (persistentOverlay != null)
        {
            persistentOverlay.SetOverlayVisible(true);
            persistentOverlay.RefreshNow();
        }

        ObjectiveAndDayOverlay[] overlays =
            FindObjectsByType<ObjectiveAndDayOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        foreach (ObjectiveAndDayOverlay overlay in overlays)
        {
            if (overlay == null) continue;
            overlay.SetOverlayVisible(true);
            overlay.RefreshNow();
        }

        return true;
    }


    // =========================================================
    // FIND CASE
    // =========================================================

    public CaseData GetCase(int caseID)
    {
        foreach (
            CaseData caseData
            in cases
        )
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
    // CURRENT OBJECTS
    // =========================================================

    public WorldInteractable GetCurrentRequiredObject()
    {
        return currentRequiredObject;
    }


    public WorldInteractable GetCurrentSideQuestItem()
    {
        return currentSideQuestItem;
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


        // -----------------------------------------------------
        // ENSURE CURRENT SCENE REFERENCES ARE VALID
        // -----------------------------------------------------

        if (currentRequiredObject == null)
        {
            RefreshSceneReferences();
        }


        // -----------------------------------------------------
        // CORRECT OBJECT?
        // -----------------------------------------------------

        if (
            currentRequiredObject == null ||
            currentRequiredObject != pickedUpObject
        )
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


        // -----------------------------------------------------
        // ALREADY FOUND
        // -----------------------------------------------------

        if (objectFound)
        {
            return false;
        }


        // -----------------------------------------------------
        // REGISTER
        // -----------------------------------------------------

        objectFound = true;

        bossUnlocked = true;


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FindObject();
        }


        // -----------------------------------------------------
        // EVENTS
        // -----------------------------------------------------

        OnObjectFound?.Invoke();

        OnBossUnlocked?.Invoke();


        DebugCase(
            "Required object registered: "
            + pickedUpObject.gameObject.name
            + ". Boss objective unlocked."
        );

        return true;
    }


    // =========================================================
    // SIDE QUEST OBJECT
    // =========================================================

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


        // -----------------------------------------------------
        // ENSURE CURRENT SCENE REFERENCES ARE VALID
        // -----------------------------------------------------

        if (currentSideQuestItem == null)
        {
            RefreshSceneReferences();
        }


        // -----------------------------------------------------
        // CORRECT OBJECT?
        // -----------------------------------------------------

        if (
            currentSideQuestItem == null ||
            currentSideQuestItem != pickedUpItem
        )
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


        // -----------------------------------------------------
        // ALREADY COMPLETE
        // -----------------------------------------------------

        if (sideQuestComplete)
        {
            return false;
        }


        // -----------------------------------------------------
        // COMPLETE SIDE QUEST
        // -----------------------------------------------------

        sideQuestComplete = true;


        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .CompleteSideQuest();
        }


        DebugCase(
            "Side quest item registered: "
            + pickedUpItem.gameObject.name
        );


        OnSideQuestCompleted?.Invoke();

        return true;
    }


    // =========================================================
    // DIRECT OBJECTIVE ADVANCE
    // =========================================================

    public void FindRequiredObject()
    {
        if (!CanFindObject())
        {
            return;
        }


        objectFound = true;

        bossUnlocked = true;


        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.FindObject();
        }


        DebugCase(
            "Required object found."
        );

        DebugCase(
            "Boss objective unlocked."
        );


        OnObjectFound?.Invoke();

        OnBossUnlocked?.Invoke();
    }


    // =========================================================
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
        // Leaving a level immediately stops its music. Do this even if the
        // case was already marked complete by its boss-death logic.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

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


        currentRequiredObject = null;

        currentSideQuestItem = null;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugCase(
        string message)
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


    private string GetObjectName(
        WorldInteractable interactable)
    {
        if (interactable == null)
        {
            return "NULL";
        }

        return interactable.gameObject.name;
    }
}
