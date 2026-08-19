using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    // =========================================================
    // GAME MODE
    // =========================================================

    public enum GameMode
    {
        LoadingCredits,

        MainMenu,
        Settings,
        Credits,

        DifficultySelection,
        IntroVideo,

        Lobby,
        WatchingPlay,
        Case,

        BossFight,
        Cutscene
    }


    // =========================================================
    // DIFFICULTY
    // =========================================================

    public enum Difficulty
    {
        Normal,
        Hardcore
    }


    // =========================================================
    // OBJECTIVE STATE
    // =========================================================

    public enum ObjectiveState
    {
        NotStarted,
        FindObject,
        KillBoss,
        Complete
    }


    // =========================================================
    // LOSS TYPE
    // =========================================================

    public enum LossReason
    {
        PlayerDeath,
        TimerExpired
    }


    // =========================================================
    // CURRENT GAME STATE
    // =========================================================

    [Header("Current Game State")]

    [SerializeField]
    private GameMode currentGameMode = GameMode.LoadingCredits;

    [SerializeField]
    private Difficulty currentDifficulty = Difficulty.Normal;

    [SerializeField]
    [Range(1, 4)]
    private int currentDay = 1;

    [SerializeField]
    private int currentCase = 0;


    // =========================================================
    // INVENTORY
    // =========================================================

    public enum InventoryStatus
    {
        Closed,
        Open,
        Locked
    }

    [Header("Inventory")]

    [SerializeField]
    private InventoryStatus inventoryStatus = InventoryStatus.Closed;


    // =========================================================
    // OBJECTIVES
    // =========================================================

    [Header("Current Case Objectives")]

    [SerializeField]
    private ObjectiveState currentObjective =
        ObjectiveState.NotStarted;

    [SerializeField]
    private bool objectFound = false;

    [SerializeField]
    private bool bossDefeated = false;

    [SerializeField]
    private bool sideQuestComplete = false;


    // =========================================================
    // CASE TIMER
    // =========================================================

    [Header("Case Timer")]

    [SerializeField]
    private bool caseHasTimer = false;

    [SerializeField]
    private bool timerActive = false;

    [SerializeField]
    private float timeRemaining = 0f;


    // =========================================================
    // GACHA
    // =========================================================

    [Header("Gacha")]

    [SerializeField]
    private int gachaCurrency = 0;


    // =========================================================
    // FINAL GAME PROGRESSION
    // =========================================================

    [Header("Final Game Progression")]

    [SerializeField]
    private bool finalPlayComplete = false;

    [SerializeField]
    private bool finalBossDefeated = false;

    [SerializeField]
    private bool finalCutsceneComplete = false;

    [SerializeField]
    private bool gameCompleted = false;


    // =========================================================
    // LOSS
    // =========================================================

    [Header("Loss State")]

    [SerializeField]
    private bool gameLost = false;

    [SerializeField]
    private LossReason lastLossReason;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugStateChanges = true;


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

        // Survives scene changes.
        // It does NOT save anything after the game closes.
        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // GAME MODE
    // =========================================================

    public GameMode GetCurrentGameMode()
    {
        return currentGameMode;
    }


    public void SetGameMode(GameMode newMode)
    {
        if (currentGameMode == newMode)
        {
            return;
        }

        currentGameMode = newMode;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Game Mode changed to: "
                + currentGameMode
            );
        }
    }


    public bool IsGameMode(GameMode mode)
    {
        return currentGameMode == mode;
    }


    // =========================================================
    // DIFFICULTY
    // =========================================================

    public Difficulty GetDifficulty()
    {
        return currentDifficulty;
    }


    public void SetDifficulty(Difficulty difficulty)
    {
        currentDifficulty = difficulty;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Difficulty selected: "
                + currentDifficulty
            );
        }
    }


    public bool IsNormalMode()
    {
        return currentDifficulty == Difficulty.Normal;
    }


    public bool IsHardcoreMode()
    {
        return currentDifficulty == Difficulty.Hardcore;
    }


    // =========================================================
    // DAY
    // =========================================================

    public int GetCurrentDay()
    {
        return currentDay;
    }


    public void SetDay(int day)
    {
        if (day < 1 || day > 4)
        {
            Debug.LogWarning(
                "[Game State] Invalid day: " + day
            );

            return;
        }

        currentDay = day;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Day changed to: "
                + currentDay
            );
        }
    }


    public void AdvanceDay()
    {
        if (currentDay >= 4)
        {
            Debug.LogWarning(
                "[Game State] Cannot advance beyond Day 4."
            );

            return;
        }

        currentDay++;

        ResetCaseProgress();

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Advanced to Day "
                + currentDay
            );
        }
    }


    // =========================================================
    // CASE
    // =========================================================

    public int GetCurrentCase()
    {
        return currentCase;
    }


    public void SetCase(int caseNumber)
    {
        if (caseNumber < 0)
        {
            Debug.LogWarning(
                "[Game State] Invalid case number."
            );

            return;
        }

        currentCase = caseNumber;

        ResetCaseProgress();

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Current case: "
                + currentCase
            );
        }
    }


    // =========================================================
    // OBJECTIVES
    // =========================================================

    public ObjectiveState GetCurrentObjective()
    {
        return currentObjective;
    }


    public bool IsObjectFound()
    {
        return objectFound;
    }


    public bool IsBossDefeated()
    {
        return bossDefeated;
    }


    public bool IsSideQuestComplete()
    {
        return sideQuestComplete;
    }


    // ---------------------------------------------------------
    // START CASE
    // ---------------------------------------------------------

    public void StartCase()
    {
        currentObjective = ObjectiveState.FindObject;

        objectFound = false;
        bossDefeated = false;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Case started. "
                + "Objective: Find Object."
            );
        }
    }


    // ---------------------------------------------------------
    // OBJECT FOUND
    // ---------------------------------------------------------

    public void FindObject()
    {
        if (currentObjective != ObjectiveState.FindObject)
        {
            Debug.LogWarning(
                "[Game State] Object cannot be found "
                + "at the current objective state."
            );

            return;
        }

        objectFound = true;

        currentObjective = ObjectiveState.KillBoss;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Object found. "
                + "Objective changed to: Kill Boss."
            );
        }
    }


    // ---------------------------------------------------------
    // BOSS DEFEATED
    // ---------------------------------------------------------

    public void DefeatCaseBoss()
    {
        if (currentObjective != ObjectiveState.KillBoss)
        {
            Debug.LogWarning(
                "[Game State] Boss cannot be defeated "
                + "at the current objective state."
            );

            return;
        }

        bossDefeated = true;

        currentObjective = ObjectiveState.Complete;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Case boss defeated. "
                + "Case complete."
            );
        }
    }


    // ---------------------------------------------------------
    // SIDE QUEST
    // ---------------------------------------------------------

    public void CompleteSideQuest()
    {
        sideQuestComplete = true;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Side quest completed."
            );
        }
    }


    // ---------------------------------------------------------
    // CASE COMPLETE
    // ---------------------------------------------------------

    public bool IsCaseComplete()
    {
        return currentObjective == ObjectiveState.Complete;
    }


    // =========================================================
    // CASE TIMER
    // =========================================================

    public bool HasCaseTimer()
    {
        return caseHasTimer;
    }


    public bool IsTimerActive()
    {
        return timerActive;
    }


    public float GetTimeRemaining()
    {
        return timeRemaining;
    }


    public void StartCaseTimer(float duration)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning(
                "[Game State] Invalid timer duration."
            );

            return;
        }

        caseHasTimer = true;
        timerActive = true;
        timeRemaining = duration;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Case timer started: "
                + duration
                + " seconds."
            );
        }
    }


    public void StopCaseTimer()
    {
        timerActive = false;
    }


    private void Update()
    {
        if (!timerActive)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerActive = false;

            HandleLoss(LossReason.TimerExpired);
        }
    }


    // =========================================================
    // LOSS SYSTEM
    // =========================================================

    public bool IsGameLost()
    {
        return gameLost;
    }


    public LossReason GetLastLossReason()
    {
        return lastLossReason;
    }


    public void PlayerDied()
    {
        HandleLoss(LossReason.PlayerDeath);
    }


    public void HandleLoss(LossReason reason)
    {
        gameLost = true;
        lastLossReason = reason;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Player lost. Reason: "
                + reason
            );
        }

        if (currentDifficulty == Difficulty.Hardcore)
        {
            HandleHardcoreLoss();
        }
        else
        {
            HandleNormalLoss();
        }
    }


    private void HandleNormalLoss()
    {
        Debug.Log(
            "[Game State] Normal Mode loss. "
            + "Restart current day."
        );

        // Scene/day restart will be handled by
        // the GameFlowManager / SceneManager.

        ResetCurrentDay();
    }


    private void HandleHardcoreLoss()
    {
        Debug.Log(
            "[Game State] Hardcore Mode loss. "
            + "Restart entire game."
        );

        // Full game restart will be handled by
        // the GameFlowManager / SceneManager.

        ResetGameProgress();
    }


    // =========================================================
    // NORMAL MODE DAY RESET
    // =========================================================

    public void ResetCurrentDay()
    {
        currentCase = 0;

        ResetCaseProgress();

        gameLost = false;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Current day reset."
            );
        }
    }


    // =========================================================
    // FINAL GAME PROGRESSION
    // =========================================================

    public bool IsFinalPlayComplete()
    {
        return finalPlayComplete;
    }


    public bool IsFinalBossDefeated()
    {
        return finalBossDefeated;
    }


    public bool IsFinalCutsceneComplete()
    {
        return finalCutsceneComplete;
    }


    public bool IsGameCompleted()
    {
        return gameCompleted;
    }


    public void CompleteFinalPlay()
    {
        finalPlayComplete = true;
    }


    public void DefeatFinalBoss()
    {
        finalBossDefeated = true;
    }


    public void CompleteFinalCutscene()
    {
        finalCutsceneComplete = true;
    }


    public void CompleteGame()
    {
        gameCompleted = true;
    }


    // =========================================================
    // INVENTORY
    // =========================================================

    public InventoryStatus GetInventoryStatus()
    {
        return inventoryStatus;
    }


    public void SetInventoryStatus(InventoryStatus status)
    {
        inventoryStatus = status;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Inventory status: "
                + inventoryStatus
            );
        }
    }


    public bool IsInventoryOpen()
    {
        return inventoryStatus == InventoryStatus.Open;
    }


    public bool IsInventoryLocked()
    {
        return inventoryStatus == InventoryStatus.Locked;
    }


    // =========================================================
    // GACHA
    // =========================================================

    public int GetGachaCurrency()
    {
        return gachaCurrency;
    }


    public void AddGachaCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        gachaCurrency += amount;
    }


    public bool SpendGachaCurrency(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (gachaCurrency < amount)
        {
            return false;
        }

        gachaCurrency -= amount;

        return true;
    }


    // =========================================================
    // CASE RESET
    // =========================================================

    public void ResetCaseProgress()
    {
        currentObjective = ObjectiveState.NotStarted;

        objectFound = false;
        bossDefeated = false;
        sideQuestComplete = false;

        caseHasTimer = false;
        timerActive = false;
        timeRemaining = 0f;
    }


    // =========================================================
    // COMPLETE GAME RESET
    // =========================================================

    public void ResetGameProgress()
    {
        currentDay = 1;
        currentCase = 0;

        currentDifficulty = Difficulty.Normal;

        ResetCaseProgress();

        inventoryStatus = InventoryStatus.Closed;

        gachaCurrency = 0;

        finalPlayComplete = false;
        finalBossDefeated = false;
        finalCutsceneComplete = false;
        gameCompleted = false;

        gameLost = false;

        currentGameMode = GameMode.MainMenu;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] Entire game progress reset."
            );
        }
    }


    // =========================================================
    // NEW GAME
    // =========================================================

    public void StartNewGame(Difficulty difficulty)
    {
        ResetGameProgress();

        currentDifficulty = difficulty;
        currentDay = 1;

        SetGameMode(GameMode.IntroVideo);

        if (debugStateChanges)
        {
            Debug.Log(
                "[Game State] New game started. Mode: "
                + difficulty
            );
        }
    }
}