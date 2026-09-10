using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    // =========================================================
    // SCENE REFERENCES
    // =========================================================

    [Header("Scenes")]

    [SerializeField] private string loadingScene = "Loading";
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string lobbyScene = "Lobby";
    [SerializeField] private string theaterScene = "Theater";
    [SerializeField] private string caseScene = "Case";
    [SerializeField] private string creditsScene = "Credits";

    [Header("Main Menu / Intro")]

    [SerializeField]
    private GameObject mainMenuGroup;

    [SerializeField]
    private GameObject gameModeSelectionScreen;

    [SerializeField]
    private IntroCutscene introCutscene;

    [Header("UI")]
    [SerializeField] private GameObject toolbar;

    [Header("First Game Scene")]

    [SerializeField]
    private string firstGameScene = "";

    [Header("Transition")]

    [SerializeField] private float transitionDelay = 0.25f;

    [Header("Debug")]

    [SerializeField] private bool debugFlow = true;

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

    //private void Start()
    //{
    //    if (GameStateManager.Instance != null)
    //    {
    //        GameStateManager.Instance.SetGameMode(
    //            GameStateManager.GameMode.MainMenu
    //        );
    //    }

    //    if (PlayerStateManager.Instance != null)
    //    {
    //        PlayerStateManager.Instance.SetState(
    //            PlayerStateManager.PlayerState.MainMenu
    //        );
    //    }
    //}

    // =========================================================
    // MENU
    // =========================================================

    public void OpenMainMenu()
    {
        DebugFlow("Returning to Main Menu. Resetting entire game.");

        // Reset all game progress.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetEntireGame();
        }

        // Make sure the game is running.
        Time.timeScale = 1f;

        // Set player state.
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.MainMenu
            );
        }

        // Hide gameplay UI.
        if (toolbar != null)
        {
            toolbar.SetActive(false);
        }

        // Show Main Menu.
        if (mainMenuGroup != null)
        {
            mainMenuGroup.SetActive(true);
        }

        if (gameModeSelectionScreen != null)
        {
            gameModeSelectionScreen.SetActive(false);
        }

        // Menu needs mouse.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetToolbarVisible(bool visible)
    {
        if (toolbar != null)
        {
            toolbar.SetActive(visible);
        }
    }

    public void IntroSceneLoaded()
    {
        DebugFlow("Intro scene loaded. Looking for cutscene.");

        // ---------------------------------------------------------
        // MAKE SURE WE ARE ACTUALLY IN CUTSCENE MODE
        // ---------------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }

        if (
            GameStateManager.Instance.CurrentGameMode
            != GameStateManager.GameMode.Cutscene
        )
        {
            Debug.LogWarning(
                "[GameFlow] Intro scene loaded while game is not " +
                "in Cutscene mode. Returning to Lobby."
            );

            OpenLobby();

            return;
        }


        // ---------------------------------------------------------
        // LOOK FOR INTRO CUTSCENE
        // ---------------------------------------------------------

        IntroCutscene introCutscene =
            FindAnyObjectByType<IntroCutscene>();


        // ---------------------------------------------------------
        // NO CUTSCENE
        // ---------------------------------------------------------

        if (introCutscene == null)
        {
            Debug.LogWarning(
                "[GameFlow] No intro cutscene found. " +
                "Going directly to Lobby."
            );

            IntroFinished();

            return;
        }


        // ---------------------------------------------------------
        // CUTSCENE FOUND
        // ---------------------------------------------------------

        DebugFlow("Intro cutscene found. Playing.");

        introCutscene.PlayIntro();
    }

    public void OpenCredits()
    {
        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Credits);

        LoadScene(creditsScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =========================================================
    // NEW GAME
    // =========================================================

    public void StartNewGame(
    GameStateManager.DifficultyMode difficulty,
    GameStateManager.PlayerGender gender)
    {
        DebugFlow(
            "Starting new game. Difficulty: "
            + difficulty
            + " | Gender: "
            + gender
        );

        // ---------------------------------------------------------
        // RESET PREVIOUS GAME DATA
        // ---------------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }

        GameStateManager.Instance.ResetEntireGame();


        // ---------------------------------------------------------
        // APPLY MAIN MENU SELECTIONS
        // ---------------------------------------------------------

        GameStateManager.Instance.SetDifficulty(difficulty);

        GameStateManager.Instance.SetPlayerGender(gender);


        // ---------------------------------------------------------
        // START INTRO
        // ---------------------------------------------------------

        StartIntro();
    }

    // =========================================================
    // INTRO
    // =========================================================

    public void StartIntro()
    {
        DebugFlow("Starting first intro cutscene.");

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }
        SetToolbarVisible(false);

        // ---------------------------------------------------------
        // GAME STATE
        // ---------------------------------------------------------

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Cutscene
        );

        // ---------------------------------------------------------
        // PLAYER STATE
        // ---------------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        // ---------------------------------------------------------
        // HIDE MENU
        // ---------------------------------------------------------

        if (mainMenuGroup != null)
        {
            mainMenuGroup.SetActive(false);
        }

        if (gameModeSelectionScreen != null)
        {
            gameModeSelectionScreen.SetActive(false);
        }

        // ---------------------------------------------------------
        // CURSOR
        // ---------------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        // ---------------------------------------------------------
        // PLAY VIDEO
        // ---------------------------------------------------------

        if (introCutscene == null)
        {
            Debug.LogError(
                "[GameFlow] IntroCutscene reference is missing!"
            );

            return;
        }

        introCutscene.PlayIntro();
    }

    public void CheckForIntroCutscene()
    {
        DebugFlow("Checking for intro cutscene.");

        IntroCutscene introCutscene =
            FindAnyObjectByType<IntroCutscene>();

        if (introCutscene != null)
        {
            DebugFlow("Intro cutscene found.");

            return;
        }

        // ---------------------------------------------------------
        // NO CUTSCENE FOUND
        // ---------------------------------------------------------

        Debug.LogWarning(
            "[GameFlow] No intro cutscene found. " +
            "Going directly to the Lobby."
        );

        IntroFinished();
    }

    public void IntroFinished()
    {
        DebugFlow("Intro cutscene finished.");

        // ---------------------------------------------------------
        // RESTORE PLAYER CONTROL
        // ---------------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }

        // ---------------------------------------------------------
        // FIRST GAME SCENE
        // ---------------------------------------------------------

        if (string.IsNullOrEmpty(firstGameScene))
        {
            Debug.Log(
                "[GameFlow] Intro finished. " +
                "No first game scene has been configured yet."
            );
            SetToolbarVisible(true);
            return;
        }

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Loading
        );

        LoadScene(firstGameScene);
    }

    // =========================================================
    // DAY FLOW
    // =========================================================

    public void StartDay(int day)
    {
        GameStateManager.Instance.SetDay(day);

        GameStateManager.Instance.ResetCurrentDay();

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.SetTheaterDay(day);
        }

        OpenLobby();
    }

    public void CompleteDay()
    {
        if (GameStateManager.Instance.IsFinalDay())
        {
            StartFinalPlay();
            return;
        }

        GameStateManager.Instance.AdvanceDay();

        StartDay(GameStateManager.Instance.GetCurrentDay());
    }

    // =========================================================
    // LOBBY
    // =========================================================

    public void OpenLobby()
    {
        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Lobby);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Lobby);

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Lobby);
        }

        LoadScene(lobbyScene);
    }

    // =========================================================
    // PLAY
    // =========================================================

    public void StartPlay(int playID)
    {
        LoadScene(theaterScene);

        StartCoroutine(
            InitializePlayAfterLoad(playID));
    }

    private IEnumerator InitializePlayAfterLoad(int playID)
    {
        yield return new WaitForSeconds(transitionDelay);

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.StartPlay(playID);
        }
    }

    public void PlayFinished()
    {
        if (TheaterManager.Instance == null)
            return;

        if (TheaterManager.Instance.HasCaseAfterCurrentPlay())
        {
            TheaterManager.Instance.StartCaseAfterPlay();
        }
        else
        {
            TheaterManager.Instance.StartNextPlay();
        }
    }

    // =========================================================
    // CASE
    // =========================================================

    public void StartCase(int caseID)
    {
        LoadScene(caseScene);

        StartCoroutine(
            InitializeCaseAfterLoad(caseID));
    }

    private IEnumerator InitializeCaseAfterLoad(int caseID)
    {
        yield return new WaitForSeconds(transitionDelay);

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.StartCase(caseID);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Case);

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Case);
        }
    }

    public void CaseCompleted()
    {
        OpenLobby();
    }

    // =========================================================
    // FINAL PLAY
    // =========================================================

    public void StartFinalPlay()
    {
        if (TheaterManager.Instance == null)
            return;

        TheaterManager.PlayData finalPlay =
            TheaterManager.Instance.GetFinalPlay();

        if (finalPlay == null)
        {
            Debug.LogError(
                "No Final Play configured.");

            return;
        }

        StartPlay(finalPlay.playID);
    }

    public void FinalPlayFinished()
    {
        StartBossFight();
    }

    // =========================================================
    // BOSS
    // =========================================================

    public void StartBossFight()
    {
        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.BossFight);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Boss);

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Boss);
        }
    }

    public void FinalBossDefeated()
    {
        GameStateManager.Instance.DefeatFinalBoss();

        StartFinalCutscene();
    }

    // =========================================================
    // CUTSCENE
    // =========================================================

    public void StartFinalCutscene()
    {
        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Cutscene);

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        SetToolbarVisible(false);
    }

    public void FinalCutsceneFinished()
    {
        GameStateManager.Instance.CompleteFinalCutscene();

        SetToolbarVisible(false);

        OpenCredits();
    }

    // =========================================================
    // LOSS
    // =========================================================

    public void PlayerDied()
    {
        GameStateManager.Instance.HandlePlayerDeath();

        HandleLoss();
    }

    public void TimerExpired()
    {
        HandleLoss();
    }

    private void HandleLoss()
    {
        if (GameStateManager.Instance.IsHardcore())
        {
            RestartEntireGame();
        }
        else
        {
            RestartCurrentDay();
        }
    }

    // =========================================================
    // RESTARTS
    // =========================================================

    private void RestartCurrentDay()
    {
        int day =
            GameStateManager.Instance.GetCurrentDay();

        DebugFlow(
            "Restarting Day " + day);

        GameStateManager.Instance.ClearLossState();

        StartDay(day);
    }

    private void RestartEntireGame()
    {
        GameStateManager.DifficultyMode difficulty =
            GameStateManager.Instance.GetDifficulty();

        DebugFlow("Restarting Entire Game");

        GameStateManager.Instance.ResetEntireGame();

        GameStateManager.Instance.SetDifficulty(difficulty);

        StartDay(1);
    }

    // =========================================================
    // SCENE LOADING
    // =========================================================

    public void LoadScene(string sceneName)
    {
        StartCoroutine(
            LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        yield return new WaitForSeconds(transitionDelay);

        SceneManager.LoadScene(sceneName);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugFlow(string message)
    {
        if (!debugFlow)
            return;

        Debug.Log(
            "[Game Flow] " + message);
    }
}