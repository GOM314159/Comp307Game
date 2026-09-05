using UnityEngine;
using System;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }


    // =========================================================
    // GAME MODE
    // =========================================================

    public enum GameMode
    {
        Loading,
        MainMenu,
        Lobby,
        WatchingPlay,
        Inventory,
        Case,
        BossFight,
        Cutscene,
        Credits,
        Paused
    }


    // =========================================================
    // DIFFICULTY
    // =========================================================

    public enum DifficultyMode
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
        BossUnlocked,
        BossDefeated,
        Complete
    }


    // =========================================================
    // LOSS REASON
    // =========================================================

    public enum LossReason
    {
        PlayerDeath,
        TimerExpired
    }


    // =========================================================
    // OVERALL GAME STATE
    // =========================================================

    [Header("Game State")]

    [SerializeField]
    private GameMode currentGameMode =
        GameMode.Loading;

    [SerializeField]
    private DifficultyMode difficulty =
        DifficultyMode.Normal;


    // =========================================================
    // DAY
    // =========================================================

    [Header("Day Progression")]

    [SerializeField]
    [Range(1, 4)]
    private int currentDay = 1;

    [Header("Day Configuration")]

    [SerializeField]
    [Min(1)]
    private int maximumDay = 4;


    // =========================================================
    // PLAY
    // =========================================================

    [Header("Theater Progression")]

    [SerializeField]
    private int currentPlay = -1;


    // =========================================================
    // CASE
    // =========================================================

    [Header("Case Progression")]

    [SerializeField]
    private int currentCase = -1;

    [SerializeField]
    private bool caseActive = false;


    // =========================================================
    // OBJECTIVES
    // =========================================================

    [Header("Objectives")]

    [SerializeField]
    private ObjectiveState currentObjective =
        ObjectiveState.NotStarted;

    [SerializeField]
    private bool objectFound = false;

    [SerializeField]
    private bool bossUnlocked = false;

    [SerializeField]
    private bool bossDefeated = false;

    [SerializeField]
    private bool sideQuestComplete = false;


    // =========================================================
    // CASE TIMER STATE
    // =========================================================
    //
    // GameStateManager STORES timer information.
    //
    // CaseManager is responsible for actually counting
    // the timer down.
    //
    // =========================================================

    [Header("Case Timer")]

    [SerializeField]
    private bool caseTimerActive = false;

    [SerializeField]
    private float caseTimeLimit = 0f;

    [SerializeField]
    private float caseTimeRemaining = 0f;


    // =========================================================
    // FINAL GAME STATE
    // =========================================================

    [Header("Final Progression")]

    [SerializeField]
    private bool finalPlayComplete = false;

    [SerializeField]
    private bool finalBossDefeated = false;

    [SerializeField]
    private bool finalCutsceneComplete = false;


    // =========================================================
    // GACHA / CURRENCY
    // =========================================================

    [Header("Gacha")]

    [SerializeField]
    private int gachaCurrency = 0;


    // =========================================================
    // LOSS STATE
    // =========================================================

    [Header("Loss State")]

    [SerializeField]
    private bool gameOver = false;

    [SerializeField]
    private LossReason lastLossReason;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<GameMode> OnGameModeChanged;

    public event Action<DifficultyMode> OnDifficultyChanged;

    public event Action<int> OnDayChanged;

    public event Action<int> OnPlayChanged;

    public event Action<int> OnCaseChanged;

    public event Action<ObjectiveState> OnObjectiveChanged;

    public event Action OnObjectFound;

    public event Action OnBossUnlocked;

    public event Action OnBossDefeated;

    public event Action OnSideQuestCompleted;

    public event Action OnCaseStarted;

    public event Action OnCaseCompleted;

    public event Action OnFinalPlayCompleted;

    public event Action OnFinalBossDefeated;

    public event Action OnFinalCutsceneCompleted;

    public event Action<float> OnCaseTimerStarted;

    public event Action OnCaseTimerStopped;

    public event Action<LossReason> OnLoss;


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


    // =========================================================
    // GAME MODE
    // =========================================================

    public void SetGameMode(GameMode newMode)
    {
        if (currentGameMode == newMode)
        {
            return;
        }

        currentGameMode = newMode;

        OnGameModeChanged?.Invoke(
            currentGameMode
        );
    }


    public GameMode GetGameMode()
    {
        return currentGameMode;
    }


    public bool IsGameMode(GameMode mode)
    {
        return currentGameMode == mode;
    }


    // =========================================================
    // DIFFICULTY
    // =========================================================

    public void SetDifficulty(
        DifficultyMode newDifficulty)
    {
        difficulty = newDifficulty;

        OnDifficultyChanged?.Invoke(
            difficulty
        );
    }


    public DifficultyMode GetDifficulty()
    {
        return difficulty;
    }


    public bool IsHardcore()
    {
        return difficulty ==
               DifficultyMode.Hardcore;
    }


    public bool IsNormal()
    {
        return difficulty ==
               DifficultyMode.Normal;
    }


    // =========================================================
    // DAY
    // =========================================================

    public void SetDay(int day)
    {
        day = Mathf.Clamp(day, 1, maximumDay);

        if (currentDay == day)
        {
            return;
        }

        currentDay = day;

        OnDayChanged?.Invoke(
            currentDay
        );
    }


    public void AdvanceDay()
    {
        if (currentDay >= maximumDay)
        {
            return;
        }

        SetDay(currentDay + 1);
    }


    public int GetCurrentDay()
    {
        return currentDay;
    }


    public bool IsFinalDay()
    {
        return currentDay == 4;
    }


    // =========================================================
    // PLAY
    // =========================================================

    public void SetCurrentPlay(int playID)
    {
        currentPlay = playID;

        OnPlayChanged?.Invoke(
            currentPlay
        );
    }


    public int GetCurrentPlay()
    {
        return currentPlay;
    }


    // =========================================================
    // CASE
    // =========================================================

    public void SetCase(int caseID)
    {
        currentCase = caseID;

        OnCaseChanged?.Invoke(
            currentCase
        );
    }


    public int GetCurrentCase()
    {
        return currentCase;
    }


    public void StartCase()
    {
        caseActive = true;

        currentObjective =
            ObjectiveState.FindObject;

        objectFound = false;

        bossUnlocked = false;

        bossDefeated = false;

        sideQuestComplete = false;

        gameOver = false;

        SetGameMode(
            GameMode.Case
        );

        OnCaseStarted?.Invoke();

        OnObjectiveChanged?.Invoke(
            currentObjective
        );
    }


    public void CompleteCase()
    {
        caseActive = false;

        currentObjective =
            ObjectiveState.Complete;

        StopCaseTimer();

        OnCaseCompleted?.Invoke();

        OnObjectiveChanged?.Invoke(
            currentObjective
        );
    }


    public bool IsCaseActive()
    {
        return caseActive;
    }


    // =========================================================
    // OBJECTIVE 1
    // =========================================================

    public void FindObject()
    {
        if (!caseActive)
        {
            return;
        }

        if (objectFound)
        {
            return;
        }

        objectFound = true;

        bossUnlocked = true;

        currentObjective =
            ObjectiveState.BossUnlocked;

        OnObjectFound?.Invoke();

        OnBossUnlocked?.Invoke();

        OnObjectiveChanged?.Invoke(
            currentObjective
        );
    }


    public bool IsObjectFound()
    {
        return objectFound;
    }


    // =========================================================
    // OBJECTIVE 2
    // =========================================================

    public void UnlockBoss()
    {
        if (!objectFound)
        {
            return;
        }

        if (bossUnlocked)
        {
            return;
        }

        bossUnlocked = true;

        currentObjective =
            ObjectiveState.BossUnlocked;

        OnBossUnlocked?.Invoke();

        OnObjectiveChanged?.Invoke(
            currentObjective
        );
    }


    public bool IsBossUnlocked()
    {
        return bossUnlocked;
    }


    public void DefeatCaseBoss()
    {
        if (!bossUnlocked)
        {
            return;
        }

        if (bossDefeated)
        {
            return;
        }

        bossDefeated = true;

        currentObjective =
            ObjectiveState.BossDefeated;

        OnBossDefeated?.Invoke();

        OnObjectiveChanged?.Invoke(
            currentObjective
        );
    }


    public bool IsBossDefeated()
    {
        return bossDefeated;
    }


    // =========================================================
    // SIDE QUEST
    // =========================================================

    public void CompleteSideQuest()
    {
        if (!caseActive)
        {
            return;
        }

        if (sideQuestComplete)
        {
            return;
        }

        sideQuestComplete = true;

        OnSideQuestCompleted?.Invoke();
    }


    public bool IsSideQuestComplete()
    {
        return sideQuestComplete;
    }


    // =========================================================
    // TIMER
    // =========================================================
    //
    // IMPORTANT:
    // CaseManager owns the actual countdown.
    //
    // This class ONLY stores the timer state.
    //
    // =========================================================

    public void StartCaseTimer(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        caseTimerActive = true;

        caseTimeLimit = duration;

        caseTimeRemaining = duration;

        OnCaseTimerStarted?.Invoke(
            duration
        );
    }


    public void UpdateCaseTimer(
        float remainingTime)
    {
        if (!caseTimerActive)
        {
            return;
        }

        caseTimeRemaining =
            Mathf.Max(
                0f,
                remainingTime
            );
    }


    public void StopCaseTimer()
    {
        if (!caseTimerActive)
        {
            return;
        }

        caseTimerActive = false;

        caseTimeRemaining = 0f;

        OnCaseTimerStopped?.Invoke();
    }


    public bool IsCaseTimerActive()
    {
        return caseTimerActive;
    }


    public float GetCaseTimeLimit()
    {
        return caseTimeLimit;
    }


    public float GetTimeRemaining()
    {
        return caseTimeRemaining;
    }


    // =========================================================
    // FINAL PLAY
    // =========================================================

    public void CompleteFinalPlay()
    {
        finalPlayComplete = true;

        SetGameMode(
            GameMode.BossFight
        );

        OnFinalPlayCompleted?.Invoke();
    }


    public bool IsFinalPlayComplete()
    {
        return finalPlayComplete;
    }


    // =========================================================
    // FINAL BOSS
    // =========================================================

    public void DefeatFinalBoss()
    {
        if (finalBossDefeated)
        {
            return;
        }

        finalBossDefeated = true;

        OnFinalBossDefeated?.Invoke();
    }


    public bool IsFinalBossDefeated()
    {
        return finalBossDefeated;
    }


    // =========================================================
    // FINAL CUTSCENE
    // =========================================================

    public void CompleteFinalCutscene()
    {
        finalCutsceneComplete = true;

        OnFinalCutsceneCompleted?.Invoke();

        SetGameMode(
            GameMode.Credits
        );
    }


    public bool IsFinalCutsceneComplete()
    {
        return finalCutsceneComplete;
    }


    // =========================================================
    // GACHA CURRENCY
    // =========================================================

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


    public int GetGachaCurrency()
    {
        return gachaCurrency;
    }


    public void SetGachaCurrency(int amount)
    {
        gachaCurrency =
            Mathf.Max(0, amount);
    }


    // =========================================================
    // LOSS / DEATH
    // =========================================================

    public void HandleLoss(
        LossReason reason)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;

        lastLossReason = reason;

        StopCaseTimer();

        OnLoss?.Invoke(
            reason
        );
    }


    public void HandlePlayerDeath()
    {
        HandleLoss(
            LossReason.PlayerDeath
        );
    }


    public LossReason GetLastLossReason()
    {
        return lastLossReason;
    }


    public bool HasLost()
    {
        return gameOver;
    }


    public void ClearLossState()
    {
        gameOver = false;
    }


    // =========================================================
    // CASE RESET
    // =========================================================

    public void ResetCaseProgress()
    {
        caseActive = false;

        objectFound = false;

        bossUnlocked = false;

        bossDefeated = false;

        sideQuestComplete = false;

        currentObjective =
            ObjectiveState.NotStarted;

        StopCaseTimer();

        gameOver = false;
    }


    // =========================================================
    // DAY RESET
    // =========================================================
    //
    // Used by NORMAL difficulty after death/loss.
    //
    // This resets gameplay progress for the current day
    // without changing the day itself.
    //
    // =========================================================

    public void ResetCurrentDay()
    {
        currentPlay = -1;

        currentCase = -1;

        ResetCaseProgress();

        finalPlayComplete = false;

        finalBossDefeated = false;

        finalCutsceneComplete = false;

        gameOver = false;

        SetGameMode(
            GameMode.Lobby
        );
    }


    // =========================================================
    // FULL GAME RESET
    // =========================================================
    //
    // Used by HARDCORE after death/loss.
    //
    // No saving/loading is involved.
    //
    // =========================================================

    public void ResetEntireGame()
    {
        currentDay = 1;

        currentPlay = -1;

        currentCase = -1;

        ResetCaseProgress();

        finalPlayComplete = false;

        finalBossDefeated = false;

        finalCutsceneComplete = false;

        gachaCurrency = 0;

        gameOver = false;

        difficulty =
            DifficultyMode.Normal;

        SetGameMode(
            GameMode.MainMenu
        );

        OnDayChanged?.Invoke(
            currentDay
        );
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public GameMode CurrentGameMode
    {
        get { return currentGameMode; }
    }


    public DifficultyMode Difficulty
    {
        get { return difficulty; }
    }


    public int CurrentDay
    {
        get { return currentDay; }
    }


    public int CurrentPlay
    {
        get { return currentPlay; }
    }


    public int CurrentCase
    {
        get { return currentCase; }
    }


    public ObjectiveState CurrentObjective
    {
        get { return currentObjective; }
    }

    public ObjectiveState GetCurrentObjective()
    {
        return currentObjective;
    }
}