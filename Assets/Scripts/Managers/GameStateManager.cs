using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Behavior;
using UnityEngine.SceneManagement;

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

    public enum PlayerGender
    {
        Male,
        Female
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
        TimeFail,
        // Backward-compatible alias for existing serialized/referencing code.
        TimerExpired = TimeFail
    }

    public enum DeathCause
    {
        Unknown,
        Doll,
        Other
    }

    // =========================================================
    // EXODIA PIECES
    // =========================================================

    public enum ExodiaPiece
    {
        RightLeg,
        LeftLeg,
        RightArm,
        LeftArm,
        Head
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

    [SerializeField]
    private PlayerGender playerGender =
    PlayerGender.Male;

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

    [Header("Portal Progression")]

    [Tooltip("The level portal unlocked by the most recently completed theater play. -1 means none.")]
    [SerializeField]
    private int revealedPortalDay = -1;


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
    // EXODIA COLLECTION
    // =========================================================

    [Header("Exodia Collection")]

    [SerializeField]
    private bool exodiaRightLegCollected;

    [SerializeField]
    private bool exodiaLeftLegCollected;

    [SerializeField]
    private bool exodiaRightArmCollected;

    [SerializeField]
    private bool exodiaLeftArmCollected;

    [SerializeField]
    private bool exodiaHeadCollected;

    [SerializeField]
    private bool allExodiaCollected;


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

    [SerializeField, Min(0)]
    private int startingGachaCurrency = 100;

    [SerializeField]
    private int gachaCurrency = 100;

    [SerializeField]
    private List<string> collectedGachaRewardIDs = new List<string>();


    // =========================================================
    // LOSS STATE
    // =========================================================

    [Header("Loss State")]

    [SerializeField]
    private bool gameOver = false;

    [SerializeField]
    private LossReason lastLossReason;

    [SerializeField]
    private DeathCause lastDeathCause = DeathCause.Unknown;

    [Header("Behaviour Tree")]
    [SerializeField]
    private BehaviorGraphAgent behaviourAgent;

    [Header("Debug Hotkeys")]
    [Tooltip("Master switch for all F1-F12 debug hotkeys, including debug teleports, theater skip, tutorial skip, portal override and achievement reset.")]
    [SerializeField] private bool enableDebugKeys = true;

    public bool AreDebugKeysEnabled()
    {
        return enableDebugKeys;
    }

    [Header("Persistent Manager Transform")]
    [Tooltip("GameStateManager is a logic-only persistent manager. Keep its transform at the origin so it cannot accidentally be moved/fall with a scene object.")]
    [SerializeField] private bool keepTransformAtOrigin = true;


    // =========================================================
    // EVENTS
    // =========================================================

    public event System.Action<GameMode> OnGameModeChanged;

    public event System.Action<DifficultyMode> OnDifficultyChanged;

    public event System.Action<PlayerGender> OnPlayerGenderChanged;

    public event System.Action<int> OnDayChanged;

    public event System.Action<int> OnPlayChanged;

    public event System.Action<int> OnCaseChanged;

    public event System.Action<ObjectiveState> OnObjectiveChanged;

    public event System.Action OnObjectFound;

    public event System.Action OnBossUnlocked;

    public event System.Action OnBossDefeated;

    public event System.Action OnSideQuestCompleted;

    public event System.Action OnCaseStarted;

    public event System.Action OnCaseCompleted;

    public event System.Action OnFinalPlayCompleted;

    public event System.Action OnFinalBossDefeated;

    public event System.Action OnFinalCutsceneCompleted;

    public event System.Action<float> OnCaseTimerStarted;

    public event System.Action OnCaseTimerStopped;

    public event System.Action<LossReason> OnLoss;


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

        // Managers should never be positioned inside the playable world.
        // If this object was accidentally placed at a world position in the
        // scene, normalize it before making it persistent.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        if (keepTransformAtOrigin)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void LateUpdate()
    {
        if (keepTransformAtOrigin)
        {
            if (transform.position != Vector3.zero)
                transform.position = Vector3.zero;

            if (transform.rotation != Quaternion.identity)
                transform.rotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(
    Scene scene,
    LoadSceneMode mode)
    {
        RefreshBehaviourAgentReference();

        Debug.Log(
            "[GameStateManager] Scene loaded: "
            + scene.name
            + ". BehaviourGraphAgent reference refreshed."
        );
    }

    public void SetPlayerGender(PlayerGender newGender)
    {
        playerGender = newGender;

        HideOtherGender();

        OnPlayerGenderChanged?.Invoke(playerGender);
    }

    private void HideOtherGender()
    {
        GameObject nephew = FindGenderAvatar("Nephew");
        GameObject niece = FindGenderAvatar("Niece");

        if (GetPlayerGender() == PlayerGender.Male)
        {
            if (nephew != null)
            {
                nephew.SetActive(true);
            }

            setCameraToHead(nephew);

            if (niece != null)
            {
                niece.SetActive(false);
            }

            if (niece != null)
            {
                niece.SetActive(false);
            }
        }
        else
        {
            if (niece != null)
            {
                niece.SetActive(true);
            }

            setCameraToHead(niece);

            if (nephew != null)
            {
                nephew.SetActive(false);
            }            

            if (nephew != null)
            {
                nephew.SetActive(false);
            }
        }
    }

    private void setCameraToHead(GameObject character)
    {
        CameraAttachment camScript = FindObjectOfType<CameraAttachment>();
        if (camScript != null)
        {
            camScript.AttachCameraToHead(character);
        }
        else
        {
            Debug.LogError("Could not find the CameraBoneAttach script in the scene!");
        }
    }

    private static GameObject FindGenderAvatar(string avatarName)
    {
        GameObject active = GameObject.Find(avatarName);
        if (active != null)
        {
            return active;
        }

        // GameObject.Find skips inactive objects. After a prior run
        // selected the other gender, the chosen avatar can still be
        // inactive and must be found explicitly.
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform == null || transform.name != avatarName)
            {
                continue;
            }

            if (transform.gameObject.scene.IsValid())
            {
                return transform.gameObject;
            }
        }

        return null;
    }

 


    public PlayerGender GetPlayerGender()
    {
        return playerGender;
    }


    public bool IsMale()
    {
        return playerGender == PlayerGender.Male;
    }


    public bool IsFemale()
    {
        return playerGender == PlayerGender.Female;
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
        return currentDay == maximumDay;
    }


    // =========================================================
    // PORTAL PROGRESSION
    // =========================================================

    public void SetRevealedPortalDay(int day)
    {
        if (day < -1)
        {
            day = -1;
        }
        else if (day > maximumDay)
        {
            day = maximumDay;
        }

        revealedPortalDay = day;
    }

    public int GetRevealedPortalDay()
    {
        return revealedPortalDay;
    }

    public void ClearRevealedPortalDay()
    {
        revealedPortalDay = -1;
    }

    public int GetMaximumDay()
    {
        return maximumDay;
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
        if (!caseActive || bossDefeated)
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


    public bool HasGachaRewardBeenCollected(string rewardID)
    {
        if (string.IsNullOrEmpty(rewardID))
        {
            return false;
        }

        return collectedGachaRewardIDs.Contains(rewardID);
    }


    public void MarkGachaRewardCollected(string rewardID)
    {
        if (string.IsNullOrEmpty(rewardID))
        {
            return;
        }

        if (!collectedGachaRewardIDs.Contains(rewardID))
        {
            collectedGachaRewardIDs.Add(rewardID);
        }
    }


    public void ClearGachaRewards()
    {
        collectedGachaRewardIDs.Clear();
    }


    public bool HasAllDragonBalls()
    {
        for (int i = 1; i <= 7; i++)
        {
            if (!HasGachaRewardBeenCollected("DragonBall" + i))
            {
                return false;
            }
        }

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


    public void HandlePlayerDeath(DeathCause cause = DeathCause.Other)
    {
        lastDeathCause = cause;

        HandleLoss(
            LossReason.PlayerDeath
        );
    }


    public void HandleDollDeath()
    {
        HandlePlayerDeath(DeathCause.Doll);
    }


    public LossReason GetLastLossReason()
    {
        return lastLossReason;
    }


    public DeathCause GetLastDeathCause()
    {
        return lastDeathCause;
    }


    public string GetLastLossDescription()
    {
        if (lastLossReason == LossReason.TimeFail ||
            lastLossReason == LossReason.TimerExpired)
        {
            return "Time Fail";
        }

        if (lastLossReason == LossReason.PlayerDeath)
        {
            if (lastDeathCause == DeathCause.Doll)
            {
                return "Death to Doll";
            }

            return "Player Death";
        }

        return "Unknown";
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

        // IMPORTANT: resetting a day after death must NOT clear the
        // theater portal that was already unlocked for this day.
        // Portal progression is cleared only by CompleteDay() or a
        // full-game reset.

        currentCase = -1;

        ResetCaseProgress();

        ResetBehaviourTree();

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

        revealedPortalDay = -1;

        currentCase = -1;

        ResetCaseProgress();

        finalPlayComplete = false;

        finalBossDefeated = false;

        finalCutsceneComplete = false;

        gachaCurrency = Mathf.Max(0, startingGachaCurrency);
        collectedGachaRewardIDs.Clear();
        if (GachaManager.Instance != null)
        {
            GachaManager.Instance.ResetDragonBallEndingState();
        }

        gameOver = false;
        lastLossReason = LossReason.PlayerDeath;
        lastDeathCause = DeathCause.Unknown;

        difficulty =
            DifficultyMode.Normal;

        playerGender =
    PlayerGender.Male;

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

    internal void ResetBehaviourTree()
    {
        RefreshBehaviourAgentReference();

        if (behaviourAgent == null)
        {
            Debug.Log(
                "[GameStateManager] No BehaviorGraphAgent exists " +
                "in the current scene. Behaviour tree reset skipped."
            );

            return;
        }

        behaviourAgent.SetVariableValue(
            "PlayerAlive",
            true
        );

        behaviourAgent.SetVariableValue(
            "AxeInteracted",
            false
        );

        Debug.Log(
            "[GameStateManager] Resetting Behaviour Tree variables."
        );
    }

    private void RefreshBehaviourAgentReference()
    {
        behaviourAgent =
            FindAnyObjectByType<BehaviorGraphAgent>();
    }

    public PlayerGender Gender
    {
        get { return playerGender; }
    }

    // =========================================================
    // EXODIA COLLECTION
    // =========================================================

    public bool RegisterExodiaPiece(
        ExodiaPiece piece
    )
    {
        if (HasCollectedExodiaPiece(piece))
        {
            return false;
        }


        switch (piece)
        {
            case ExodiaPiece.RightLeg:

                exodiaRightLegCollected = true;

                break;


            case ExodiaPiece.LeftLeg:

                exodiaLeftLegCollected = true;

                break;


            case ExodiaPiece.RightArm:

                exodiaRightArmCollected = true;

                break;


            case ExodiaPiece.LeftArm:

                exodiaLeftArmCollected = true;

                break;


            case ExodiaPiece.Head:

                exodiaHeadCollected = true;

                break;
        }


        UpdateAllExodiaCollected();


        Debug.Log(
            "[Exodia] Collected "
            + piece
            + " | "
            + GetExodiaPiecesCollected()
            + "/5"
        );


        return true;
    }


    // =========================================================
    // CHECK INDIVIDUAL PIECE
    // =========================================================

    public bool HasCollectedExodiaPiece(
        ExodiaPiece piece
    )
    {
        switch (piece)
        {
            case ExodiaPiece.RightLeg:
                return exodiaRightLegCollected;

            case ExodiaPiece.LeftLeg:
                return exodiaLeftLegCollected;

            case ExodiaPiece.RightArm:
                return exodiaRightArmCollected;

            case ExodiaPiece.LeftArm:
                return exodiaLeftArmCollected;

            case ExodiaPiece.Head:
                return exodiaHeadCollected;
        }


        return false;
    }


    // =========================================================
    // UPDATE ALL EXODIA
    // =========================================================

    private void UpdateAllExodiaCollected()
    {
        allExodiaCollected =
            exodiaRightLegCollected &&
            exodiaLeftLegCollected &&
            exodiaRightArmCollected &&
            exodiaLeftArmCollected &&
            exodiaHeadCollected;


        if (allExodiaCollected)
        {
            Debug.Log(
                "[Exodia] ALL FIVE PIECES HAVE BEEN COLLECTED."
            );
        }
    }


    // =========================================================
    // CHECK ALL FIVE
    // =========================================================

    public bool HasCollectedAllExodiaPieces()
    {
        return allExodiaCollected;
    }


    // =========================================================
    // COUNT
    // =========================================================

    public int GetExodiaPiecesCollected()
    {
        int count = 0;


        if (exodiaRightLegCollected)
            count++;

        if (exodiaLeftLegCollected)
            count++;

        if (exodiaRightArmCollected)
            count++;

        if (exodiaLeftArmCollected)
            count++;

        if (exodiaHeadCollected)
            count++;


        return count;
    }

    public bool[] GetExodiaCollectionSnapshot()
    {
        return new bool[]
        {
        exodiaRightLegCollected,
        exodiaLeftLegCollected,
        exodiaRightArmCollected,
        exodiaLeftArmCollected,
        exodiaHeadCollected
        };
    }

    public void RestoreExodiaCollectionSnapshot(
    bool[] snapshot
)
    {
        if (
            snapshot == null ||
            snapshot.Length != 5
        )
        {
            Debug.LogWarning(
                "[Exodia] Invalid snapshot."
            );

            return;
        }


        exodiaRightLegCollected = snapshot[0];

        exodiaLeftLegCollected = snapshot[1];

        exodiaRightArmCollected = snapshot[2];

        exodiaLeftArmCollected = snapshot[3];

        exodiaHeadCollected = snapshot[4];


        UpdateAllExodiaCollected();
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetExodiaCollection()
    {
        exodiaRightLegCollected = false;

        exodiaLeftLegCollected = false;

        exodiaRightArmCollected = false;

        exodiaLeftArmCollected = false;

        exodiaHeadCollected = false;

        allExodiaCollected = false;


        Debug.Log(
            "[Exodia] Collection reset."
        );
    }
}
