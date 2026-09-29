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

    // The single master scene that holds the "Managers" root,
    // the Main Menu UI, the Lobby, and everything else this
    // game actually runs out of. Some scenes get tested on
    // their own in the Editor, so any "return to Main Menu"
    // request needs to be able to get back here from anywhere.
    [SerializeField] private string gamePlayTestScene = "GamePlayTest";

    [Header("Main Menu / Intro")]

    [SerializeField]
    private GameObject mainMenuGroup;

    [SerializeField]
    private GameObject gameModeSelectionScreen;

    [SerializeField]
    private IntroCutscene introCutscene;

    [Header("First Game Cutscene")]

    [SerializeField]
    private FirstGameCutscene firstGameCutscene;

    [Header("Death Menu")]

    [SerializeField]
    private GameObject deathMenu;

    [Header("UI")]
    [SerializeField] private GameObject toolbar;
    [SerializeField] private GameObject HealthBar;
    [SerializeField] private GameObject StaminaBar;

    // =========================================================
    // PLAYER REFERENCES
    // =========================================================

    [Header("Player References")]

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private PlayerRespawnPoint playerRespawnPoint;

    [Header("Transition")]

    [SerializeField] private float transitionDelay = 0.25f;

    private bool createDaySnapshotOnLobbyLoad = false;

    private Coroutine newGamePreparationCoroutine;

    // Set while we're waiting for GamePlayTest to load so a
    // caller (OpenMainMenu, OpenLobby, ...) can finish its work
    // once that scene is actually ready. See
    // EnsureInGamePlayTestScene().
    private System.Action pendingGamePlayTestAction;

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
        SceneManager.sceneLoaded += OnSceneLoaded;

        // ---------------------------------------------------------
        // PICK UP REFERENCES FOR THE SCENE WE STARTED IN
        // ---------------------------------------------------------
        //
        // SceneManager.sceneLoaded only fires for scenes loaded
        // AFTER this subscription runs. When Play is pressed
        // directly inside a scene that already has the "Managers"
        // object in it (e.g. testing GamePlayTest on its own), that
        // scene is already loaded by the time this Awake() runs, so
        // the event never fires for it and RefreshSceneReferences()
        // was never called. That left playerRespawnPoint (and
        // player) null, which is why MovePlayerToRespawnPoint()
        // was failing with "Player Respawn Point has not been
        // assigned in the Inspector!" even though it was assigned
        // in the scene, just never picked up. Refreshing once here
        // covers the initial scene too.
        RefreshSceneReferences();
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
        RefreshSceneReferences();

        DebugFlow(
            "Scene loaded: "
            + scene.name
            + ". Scene references refreshed."
        );

        // If EnsureInGamePlayTestScene() had to load GamePlayTest
        // first for something, finish that work now that it's
        // actually ready.
        if (pendingGamePlayTestAction != null
            && scene.name == gamePlayTestScene)
        {
            System.Action action = pendingGamePlayTestAction;
            pendingGamePlayTestAction = null;

            action();
        }
    }

    // =========================================================
    // GAMEPLAYTEST SCENE GUARD
    // =========================================================
    //
    // Several flows (returning to the Main Menu, opening the
    // Lobby) only make sense inside GamePlayTest - that's where
    // the Main Menu UI, the Lobby, and the day's PlayerRespawnPoint
    // actually live. If one of those is requested while a
    // different scene happens to be loaded on its own (e.g. you
    // died mid-level, in a level scene loaded via TeleportManager,
    // or you're testing a level scene directly), load GamePlayTest
    // first and run the real logic once it's ready. If we're
    // already there, it just runs immediately.

    private void EnsureInGamePlayTestScene(System.Action onReady)
    {
        if (SceneManager.GetActiveScene().name == gamePlayTestScene)
        {
            onReady();
            return;
        }

        DebugFlow(
            "Not currently in "
            + gamePlayTestScene
            + ". Loading it before continuing."
        );

        pendingGamePlayTestAction = onReady;

        SceneManager.LoadScene(gamePlayTestScene);
    }

    public void RefreshSceneReferences()
    {
        // =====================================================
        // PLAYER
        // =====================================================

        GameObject scenePlayer =
            GameObject.FindGameObjectWithTag("Player");

        if (scenePlayer != null)
        {
            player = scenePlayer;
        }

        // =====================================================
        // RESPAWN POINT
        // =====================================================

        PlayerRespawnPoint sceneRespawnPoint =
            FindAnyObjectByType<PlayerRespawnPoint>();

        if (sceneRespawnPoint != null)
        {
            playerRespawnPoint =
                sceneRespawnPoint;
        }

        // =====================================================
        // CUTSCENES
        // =====================================================

        IntroCutscene sceneIntro =
            FindAnyObjectByType<IntroCutscene>();

        if (sceneIntro != null)
        {
            introCutscene =
                sceneIntro;
        }

        FirstGameCutscene sceneFirstGame =
            FindAnyObjectByType<FirstGameCutscene>();

        if (sceneFirstGame != null)
        {
            firstGameCutscene =
                sceneFirstGame;
        }

        DebugFlow(
            "Scene-local GameFlow references refreshed."
        );
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

    private IEnumerator PreparePlayerForNewGame()
    {
        DebugFlow(
            "Preparing player for a completely new game."
        );

        if (player == null)
        {
            Debug.LogError(
                "[GameFlow] Player reference is missing."
            );

            yield break;
        }

        // =========================================================
        // STOP PLAYER MOVEMENT
        // =========================================================

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }


        // =========================================================
        // RESET LOOK
        // =========================================================

        PlayerLook look =
            player.GetComponent<PlayerLook>();

        if (look != null)
        {
            look.ResetLook();
        }


        // =========================================================
        // MOVE PLAYER AWAY FROM PREVIOUS OBJECT
        // =========================================================

        if (playerRespawnPoint != null)
        {
            CharacterController controller =
                player.GetComponent<CharacterController>();

            if (controller != null)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(
                playerRespawnPoint.transform.position,
                playerRespawnPoint.transform.rotation
            );

            if (controller != null)
            {
                controller.enabled = true;
            }
        }


        // =========================================================
        // RESET PLAYER STATE
        // =========================================================

        PlayerStateManager state =
            player.GetComponent<PlayerStateManager>();

        if (state != null)
        {
            state.ResetToDefault();

            state.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }


        // =========================================================
        // GIVE UNITY ONE FRAME
        // =========================================================

        // This is important.
        //
        // It gives the CharacterController, camera and
        // physics system a frame to acknowledge the new
        // player position before the intro video starts.

        yield return null;


        DebugFlow(
            "Player is safely positioned for the new-game intro."
        );
    }

    private IEnumerator StartNewGameAfterPlayerReset()
    {
        // ---------------------------------------------------------
        // RESET PLAYER POSITION/CAMERA
        // ---------------------------------------------------------

        yield return StartCoroutine(
            PreparePlayerForNewGame()
        );


        // ---------------------------------------------------------
        // NOW START INTRO
        // ---------------------------------------------------------

        newGamePreparationCoroutine = null;
        StartFirstGameSequence();
    }

    public void OpenMainMenu()
    {
        EnsureInGamePlayTestScene(FinishReturnToMainMenu);
    }

    private void FinishReturnToMainMenu()
    {
        DebugFlow("Returning to Main Menu. Resetting entire game.");

        // Cancel any new-game preparation that may still be running.
        if (newGamePreparationCoroutine != null)
        {
            StopCoroutine(newGamePreparationCoroutine);
            newGamePreparationCoroutine = null;
        }

        // Stop any cinematic that may still be rendering/preparing.
        if (firstGameCutscene != null)
        {
            firstGameCutscene.StopSequence();
        }

        if (introCutscene != null)
        {
            introCutscene.StopCutscene();
        }

        // Reset all game progress.
        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetEntireGame();
        }
        else if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetEntireGame();
        }

        Time.timeScale = 1f;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.MainMenu
            );
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.MainMenu
            );
        }

        SetGameplayHUDVisible(false);

        if (mainMenuGroup != null)
        {
            mainMenuGroup.SetActive(true);
        }

        if (gameModeSelectionScreen != null)
        {
            gameModeSelectionScreen.SetActive(false);
        }

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

    private void SetHealthBarVisible(bool visible)
    {
        if (HealthBar != null)
        {
            HealthBar.SetActive(visible);
        }
    }

    private void SetStaminaBarVisible(bool visible)
    {
        if (StaminaBar != null)
        {
            StaminaBar.SetActive(visible);
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

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetEntireGame();
        }
        else
        {
            GameStateManager.Instance.ResetEntireGame();
        }


        // ---------------------------------------------------------
        // APPLY MAIN MENU SELECTIONS
        // ---------------------------------------------------------

        GameStateManager.Instance.SetDifficulty(difficulty);

        GameStateManager.Instance.SetPlayerGender(gender);

        // ---------------------------------------------------------
        // GIVE PLAYER THEIR GENDER-SPECIFIC LETTER
        // ---------------------------------------------------------

        PlayerInventory playerInventory =
            FindAnyObjectByType<PlayerInventory>();

        if (playerInventory != null)
        {
            playerInventory.GiveStartingLetter();

            playerInventory.GiveStartingExodiaRightLeg();
        }
        else
        {
            Debug.LogError(
                "[GameFlow] PlayerInventory could not be found. "
                + "The starting letter could not be added."
            );
        }

        // ---------------------------------------------------------
        // START INTRO
        // ---------------------------------------------------------

        if (newGamePreparationCoroutine != null)
        {
            StopCoroutine(newGamePreparationCoroutine);
        }

        newGamePreparationCoroutine =
            StartCoroutine(StartNewGameAfterPlayerReset());


    }

    public void StartFirstGameSequence()
    {
        DebugFlow(
            "Starting first-game cinematic sequence."
        );


        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager is missing."
            );

            return;
        }


        // ---------------------------------------------------------
        // CUTSCENE STATE
        // ---------------------------------------------------------

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Cutscene
        );


        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }


        // ---------------------------------------------------------
        // HIDE HUD
        // ---------------------------------------------------------

        SetGameplayHUDVisible(false);


        // ---------------------------------------------------------
        // HIDE MENU
        // ---------------------------------------------------------

        //if (mainMenuGroup != null)
        //{
        //    mainMenuGroup.SetActive(false);
        //}

        //if (gameModeSelectionScreen != null)
        //{
        //    gameModeSelectionScreen.SetActive(false);
        //}


        // ---------------------------------------------------------
        // CURSOR
        // ---------------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        // ---------------------------------------------------------
        // PLAY SEQUENCE
        // ---------------------------------------------------------

        if (firstGameCutscene == null)
        {
            firstGameCutscene =
                FindAnyObjectByType<FirstGameCutscene>();
        }

        if (firstGameCutscene == null)
        {
            Debug.LogError(
                "[GameFlow] FirstGameCutscene reference missing."
            );

            return;
        }

        firstGameCutscene.PlayFirstGameSequence();
    }

    public void IntroVideoFirstFrameReady()
    {
        DebugFlow(
            "Intro video first frame is ready. " +
            "Hiding menu."
        );

        StartCoroutine(
            HideMenuAfterVideoFrame()
        );
    }

    private IEnumerator HideMenuAfterVideoFrame()
    {
        // Give Unity time to finish the current rendering cycle.
        yield return new WaitForEndOfFrame();

        // Now the video frame is actually ready to be displayed.

        if (mainMenuGroup != null)
        {
            mainMenuGroup.SetActive(false);
        }

        if (gameModeSelectionScreen != null)
        {
            gameModeSelectionScreen.SetActive(false);
        }

        DebugFlow(
            "Main Menu hidden after intro video became visible."
        );
    }

    // =========================================================
    // INTRO
    // =========================================================

    public void SetGameplayHUDVisible(bool visible)
    {
        SetToolbarVisible(visible);
        SetHealthBarVisible(visible);
        SetStaminaBarVisible(visible);
    }

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
        SetHealthBarVisible(false);
        SetStaminaBarVisible(false);

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

    // =========================================================
    // DEBUG - SKIP CURRENT CUTSCENE
    // =========================================================

    public void SkipCurrentCutscene()
    {
        Debug.Log(
            "[GameFlowManager] Debug override skipping cutscene."
        );


        // =====================================================
        // STOP INTRO CUTSCENE
        // =====================================================

        if (introCutscene != null)
        {
            introCutscene.StopCutscene();
        }


        // =====================================================
        // STOP FIRST-GAME CUTSCENE
        // =====================================================

        if (firstGameCutscene != null)
        {
            firstGameCutscene.StopSequence();
        }
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


        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }

        // Start the first day.
        StartDay(1);
    }

    // =========================================================
    // DAY FLOW
    // =========================================================

    public void StartDay(int day)
    {
        StartDay(day, true);
    }

    // createDaySnapshot: whether this should capture a fresh
    // "beginning of day" snapshot once the player lands in the
    // Lobby. This should be true for an actual new day (first day,
    // CompleteDay, or a full game reset back to Day 1) and false
    // when we're just restarting the SAME day after a death or
    // timeout - in that case ResetCurrentDay() already restored
    // the player to the existing day-start snapshot, and
    // recapturing it here would instead bake in whatever state the
    // player happened to be in right after the reset (e.g. an item
    // they'd picked up that day that the reset failed to strip),
    // permanently corrupting that day's baseline for every future
    // restart.
    public void StartDay(int day, bool createDaySnapshot)
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Game Flow] GameStateManager is missing."
            );

            return;
        }

        // ---------------------------------------------------------
        // SET DAY
        // ---------------------------------------------------------

        GameStateManager.Instance.SetDay(day);

        // ---------------------------------------------------------
        // THEATER DAY
        // ---------------------------------------------------------

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.SetTheaterDay(day);
        }

        // ---------------------------------------------------------
        // MARK WHETHER A NEW DAY SNAPSHOT IS REQUIRED
        // ---------------------------------------------------------

        createDaySnapshotOnLobbyLoad = createDaySnapshot;

        DebugFlow(
            "Starting Day " + day
        );

        // ---------------------------------------------------------
        // GO TO LOBBY
        // ---------------------------------------------------------

        OpenLobby();
    }

    public void CompleteDay()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] Cannot complete day because GameStateManager is missing."
            );

            return;
        }

        int completedDay =
            GameStateManager.Instance.GetCurrentDay();

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance
                .CompleteDay(completedDay);
        }

        if (GameStateManager.Instance.IsFinalDay())
        {
            StartFinalPlay();
            return;
        }

        GameStateManager.Instance.AdvanceDay();

        StartDay(
            GameStateManager.Instance.GetCurrentDay()
        );
    }

    // =========================================================
    // LOBBY
    // =========================================================

    public void OpenLobby()
    {
        EnsureInGamePlayTestScene(FinishOpenLobby);
    }

    private void FinishOpenLobby()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }

        DebugFlow(
            "Opening Lobby in the current scene."
        );

        // =========================================================
        // GAME STATE
        // =========================================================

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Lobby
        );

        // =========================================================
        // AUDIO
        // =========================================================

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Lobby
            );

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Lobby
            );
        }

        // =========================================================
        // PLAYER
        // =========================================================

        StartCoroutine(
            MovePlayerToRespawnPoint()
        );
        SetToolbarVisible(true);
        SetHealthBarVisible(true);
        SetStaminaBarVisible(true);
    }

    private IEnumerator MovePlayerToRespawnPoint()
    {
        DebugFlow(
            "Preparing to move Player to PlayerRespawnPoint."
        );

        // =========================================================
        // VALIDATE INSPECTOR REFERENCES
        // =========================================================

        if (playerRespawnPoint == null)
        {
            Debug.LogError(
                "[GameFlow] Player Respawn Point has not been assigned in the Inspector!"
            );

            yield break;
        }

        if (player == null)
        {
            Debug.LogError(
                "[GameFlow] Player has not been assigned in the Inspector!"
            );

            yield break;
        }

        DebugFlow(
            "Player and PlayerRespawnPoint references found."
        );

        // Give Unity one frame to finish any state changes
        // before moving the CharacterController.
        yield return null;

        // =========================================================
        // TELEPORT PLAYER
        // =========================================================

        playerRespawnPoint.SpawnPlayer(player);

        // =========================================================
        // RESET HEALTH
        // =========================================================

        PlayerHealth health =
            player.GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.ResetHealth();
        }

        // =========================================================
        // RESET MOVEMENT
        // =========================================================

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }

        // =========================================================
        // RESET PLAYER STATE
        // =========================================================

        PlayerStateManager state =
            player.GetComponent<PlayerStateManager>();

        if (state != null)
        {
            state.ResetToDefault();
        }

        DebugFlow(
            "Player successfully moved to PlayerRespawnPoint."
        );

        // =========================================================
        // GAME START SNAPSHOT
        // =========================================================

        if (GameResetManager.Instance != null)
        {
            if (!GameResetManager.Instance.HasGameStartSnapshot())
            {
                GameResetManager.Instance.CreateGameStartSnapshot();

                DebugFlow(
                    "Game-start snapshot created."
                );
            }
        }

        // =========================================================
        // APPLY DAY SNAPSHOT
        // =========================================================

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance
                .ApplyDaySnapshotToCurrentPlayer();
        }

        // =========================================================
        // CREATE BEGINNING-OF-DAY SNAPSHOT
        // =========================================================

        if (createDaySnapshotOnLobbyLoad)
        {
            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance.CreateDaySnapshot();

                DebugFlow(
                    "Beginning-of-day snapshot created."
                );
            }

            createDaySnapshotOnLobbyLoad = false;
        }
    }

    // =========================================================
    // PLAY
    // =========================================================

    public void StartPlay(int playID)
    {
        DebugFlow(
        "Starting Play " + playID +
        " in the current scene."
    );

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.WatchingPlay
            );
        }

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
        DebugFlow(
        "Starting Case " + caseID +
        " in the current scene."
    );

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Case
            );
        }

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
        //OpenLobby();
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
        SetHealthBarVisible(false);
        SetStaminaBarVisible(false);
    }

    public void FinalCutsceneFinished()
    {
        GameStateManager.Instance.CompleteFinalCutscene();

        SetToolbarVisible(false);
        SetHealthBarVisible(false);
        SetStaminaBarVisible(false);

        OpenCredits();
    }

    // =========================================================
    // LOSS
    // =========================================================

    public void PlayerDied()
    {
        DebugFlow("Player died.");

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.HandlePlayerDeath();
        }

        // PlayerDeathManager handles the death screen
        // and the actual respawn.
    }


    public void TimerExpired()
    {
        HandleLoss();
        //ShowDeathMenu();
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


        // ---------------------------------------------------------
        // RESET EVERYTHING THAT HAPPENED DURING THIS DAY
        // ---------------------------------------------------------

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetCurrentDay();
        }
        else
        {
            Debug.LogWarning(
                "[GameFlow] GameResetManager is missing."
            );
        }


        // ---------------------------------------------------------
        // RETURN TO THE SAME DAY'S LOBBY
        // ---------------------------------------------------------
        //
        // Same day, not a new one - keep the existing day-start
        // snapshot instead of recapturing it from whatever state
        // the reset just produced.

        StartDay(day, false);
    }

    private void RestartEntireGame()
    {
        DebugFlow(
            "Restarting Entire Game"
        );


        // ---------------------------------------------------------
        // RESET EVERYTHING
        // ---------------------------------------------------------

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetEntireGame();
        }
        else
        {
            GameStateManager.Instance.ResetEntireGame();
        }


        // ---------------------------------------------------------
        // HARDCORE DEATH
        // ---------------------------------------------------------
        //
        // A full game reset means we go back to the beginning.
        //
        // If you want Hardcore death to return to the Main Menu,
        // use OpenMainMenu() here instead.
        //
        // For now this starts a fresh Day 1.
        // ---------------------------------------------------------

        StartDay(1);
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

    // =========================================================
    // PLAYER DEATH - NORMAL
    // =========================================================

    public void RestartCurrentDayAfterDeath()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager is missing."
            );

            return;
        }

        int day =
            GameStateManager.Instance.GetCurrentDay();

        DebugFlow(
            "Normal death. Restarting Day " + day
        );

        // ---------------------------------------------------------
        // RESET EVERYTHING THAT HAPPENED THIS DAY
        // ---------------------------------------------------------

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetCurrentDay();
        }
        else
        {
            Debug.LogWarning(
                "[GameFlow] GameResetManager is missing."
            );
        }

        // ---------------------------------------------------------
        // RESTART SAME DAY
        // ---------------------------------------------------------
        //
        // Same day, not a new one - keep the existing day-start
        // snapshot instead of recapturing it from whatever state
        // the reset just produced. This is what let a weapon
        // picked up during the day (e.g. the axe) survive a death
        // and get permanently baked into the day's baseline
        // inventory, so every future restart of that day kept it
        // too.

        StartDay(day, false);
    }


    // =========================================================
    // PLAYER DEATH - HARDCORE
    // =========================================================

    public void RestartEntireGameAfterDeath()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager is missing."
            );

            return;
        }

        // Remember that this run was Hardcore.
        GameStateManager.DifficultyMode difficulty =
            GameStateManager.Instance.GetDifficulty();

        DebugFlow(
            "Hardcore death. Resetting entire game."
        );

        // ---------------------------------------------------------
        // RESET EVERYTHING
        // ---------------------------------------------------------

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetEntireGame();
        }
        else
        {
            GameStateManager.Instance.ResetEntireGame();
        }

        // ---------------------------------------------------------
        // KEEP HARDCORE MODE
        // ---------------------------------------------------------

        GameStateManager.Instance.SetDifficulty(
            difficulty
        );

        // ---------------------------------------------------------
        // START FRESH DAY 1
        // ---------------------------------------------------------

        StartDay(1);
    }

    private void ResetPlayerToRespawnPoint()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "[GameFlow] Cannot reset player position. " +
                "Player reference is missing."
            );

            return;
        }

        if (playerRespawnPoint == null)
        {
            Debug.LogWarning(
                "[GameFlow] Cannot reset player position. " +
                "PlayerRespawnPoint reference is missing."
            );

            return;
        }


        // ---------------------------------------------------------
        // TELEPORT
        // ---------------------------------------------------------

        playerRespawnPoint.SpawnPlayer(player);


        // ---------------------------------------------------------
        // RESET MOVEMENT
        // ---------------------------------------------------------

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }


        // ---------------------------------------------------------
        // RESET LOOK
        // ---------------------------------------------------------

        PlayerLook look =
            player.GetComponent<PlayerLook>();

        if (look != null)
        {
            look.ResetLook();
        }


        // ---------------------------------------------------------
        // RESET STATE
        // ---------------------------------------------------------

        PlayerStateManager state =
            player.GetComponent<PlayerStateManager>();

        if (state != null)
        {
            state.ResetToDefault();
        }


        DebugFlow(
            "Player returned to the PlayerRespawnPoint."
        );
    }
}