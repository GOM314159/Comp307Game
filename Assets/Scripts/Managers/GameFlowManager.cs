using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

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

    [Header("Day Screens")]

    [SerializeField]
    private DayScreenCutscene dayScreenCutscene;

    private bool playDayScreenAfterLobbyLoad;
    private int pendingDayScreenDay = -1;
    private bool startDay1TutorialAfterLobbyRespawn;

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

    // =========================================================
    // LOBBY DAY / FINAL BOSS CHANGES
    // =========================================================

    [Header("Lobby Changes")]

    [SerializeField] private string day2ChangesRootName = "Day2Changes";
    [SerializeField] private string day3ChangesRootName = "Day3Changes";
    [SerializeField] private string day4ChangesRootName = "Day4Changes";
    [SerializeField] private string bossChangesRootName = "BossChanges";

    [Header("Final Boss")]

    [Tooltip(
        "Exact GameObject name of the FinalBossController host in the " +
        "loaded scene."
    )]
    [SerializeField]
    private string finalBossControllerObjectName = "FinalBossController";

    [Tooltip(
        "Exact GameObject name of the boss. Used when the controller " +
        "object is missing so one can be created and bound at runtime."
    )]
    [SerializeField]
    private string finalBossObjectName = "Gargamel";

    [SerializeField] private bool debugLobbyChanges = true;

    private bool createDaySnapshotOnLobbyLoad = false;

    private Coroutine newGamePreparationCoroutine;

    private System.Action pendingGamePlayTestAction;

    [Header("Final Ending")]

    [Tooltip(
        "How long the Credits game mode remains active before returning " +
        "to the Main Menu."
    )]
    [Min(0f)]
    [SerializeField] private float finalCreditsDuration = 10f;

    [Header("Debug")]

    [Tooltip("Enable or disable all F-key debug hotkeys.")]
    [SerializeField] private bool enableDebugHotkeys = true;

    [SerializeField] private bool debugFlow = true;

    public bool AreDebugHotkeysEnabled()
    {
        return enableDebugHotkeys;
    }

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

        RefreshSceneReferences();
    }

    private void Start()
    {
        ApplyLobbyChangesToCurrentScene();
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

        ApplyLobbyChangesToCurrentScene();

        DebugFlow(
            "Scene loaded: "
            + scene.name
            + ". Scene references and lobby changes refreshed."
        );

        if (pendingGamePlayTestAction != null &&
            scene.name == gamePlayTestScene)
        {
            System.Action action = pendingGamePlayTestAction;

            pendingGamePlayTestAction = null;

            action();
        }
    }

    // =========================================================
    // GAMEPLAYTEST SCENE GUARD
    // =========================================================

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

    // =========================================================
    // SCENE REFERENCES
    // =========================================================

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

        playerRespawnPoint = ResolveLobbyRespawnPoint();

        // =====================================================
        // CUTSCENES
        // =====================================================

        IntroCutscene sceneIntro =
            FindAnyObjectByType<IntroCutscene>();

        if (sceneIntro != null)
        {
            introCutscene = sceneIntro;
        }

        FirstGameCutscene sceneFirstGame =
            FindAnyObjectByType<FirstGameCutscene>();

        if (sceneFirstGame != null)
        {
            firstGameCutscene = sceneFirstGame;
        }

        DayScreenCutscene sceneDayScreen =
            FindAnyObjectByType<DayScreenCutscene>(
                FindObjectsInactive.Include
            );

        if (sceneDayScreen != null)
        {
            dayScreenCutscene = sceneDayScreen;
        }
        else if (DayScreenCutscene.Instance != null)
        {
            dayScreenCutscene = DayScreenCutscene.Instance;
        }

        DebugFlow(
            "Scene-local GameFlow references refreshed."
        );
    }

    // =========================================================
    // LOBBY DAY / FINAL BOSS CHANGES
    // =========================================================

    public void ApplyLobbyChangesToCurrentScene()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning(
                "[GameFlow] Cannot apply Lobby changes because " +
                "GameStateManager is missing."
            );

            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.name != gamePlayTestScene)
        {
            return;
        }

        int day =
            GameStateManager.Instance.GetCurrentDay();

        bool finalPlayComplete =
            GameStateManager.Instance.IsFinalPlayComplete();

        SetSceneRootActive(
            activeScene,
            day2ChangesRootName,
            day >= 2
        );

        SetSceneRootActive(
            activeScene,
            day3ChangesRootName,
            day >= 3
        );

        SetSceneRootActive(
            activeScene,
            day4ChangesRootName,
            day >= 4
        );

        SetSceneRootActive(
            activeScene,
            bossChangesRootName,
            finalPlayComplete
        );

        DebugLobbyChanges(
            "Applied Lobby changes for Day "
            + day
            + ". Final play complete = "
            + finalPlayComplete
            + "."
        );
    }

    private void SetSceneRootActive(
        Scene scene,
        string rootName,
        bool shouldBeActive)
    {
        if (string.IsNullOrWhiteSpace(rootName))
        {
            return;
        }

        GameObject target =
            FindSceneObjectByName(
                scene,
                rootName
            );

        if (target == null)
        {
            DebugLobbyChanges(
                "Object '"
                + rootName
                + "' was not found in scene '"
                + scene.name
                + "'."
            );

            return;
        }

        if (target.activeSelf != shouldBeActive)
        {
            target.SetActive(shouldBeActive);
        }
    }

    private GameObject FindSceneObjectByName(
        Scene scene,
        string objectName)
    {
        GameObject[] roots =
            scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            if (root == null)
            {
                continue;
            }

            GameObject found =
                FindChildObjectByName(
                    root.transform,
                    objectName
                );

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private GameObject FindChildObjectByName(
        Transform parent,
        string objectName)
    {
        if (parent.name == objectName)
        {
            return parent.gameObject;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found =
                FindChildObjectByName(
                    parent.GetChild(i),
                    objectName
                );

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void DebugLobbyChanges(string message)
    {
        if (debugLobbyChanges)
        {
            Debug.Log(
                "[LobbyChanges] " + message
            );
        }
    }

    // =========================================================
    // NEW GAME PREPARATION
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

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }

        PlayerLook look =
            player.GetComponent<PlayerLook>();

        if (look != null)
        {
            look.ResetLook();
        }

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

        PlayerStateManager state =
            player.GetComponent<PlayerStateManager>();

        if (state != null)
        {
            state.ResetToDefault();

            state.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        yield return null;

        DebugFlow(
            "Player is safely positioned for the new-game intro."
        );
    }

    private IEnumerator StartNewGameAfterPlayerReset()
    {
        yield return StartCoroutine(
            PreparePlayerForNewGame()
        );

        newGamePreparationCoroutine = null;

        StartFirstGameSequence();
    }

    // =========================================================
    // MENU
    // =========================================================

    public void OpenMainMenu()
    {
        EnsureInGamePlayTestScene(
            FinishReturnToMainMenu
        );
    }

    private void FinishReturnToMainMenu()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetTutorial();
        }

        DebugFlow(
            "Returning to Main Menu. Resetting entire game."
        );

        if (newGamePreparationCoroutine != null)
        {
            StopCoroutine(
                newGamePreparationCoroutine
            );

            newGamePreparationCoroutine = null;
        }

        if (firstGameCutscene != null)
        {
            firstGameCutscene.StopSequence();
        }

        if (introCutscene != null)
        {
            introCutscene.StopCutscene();
        }

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.MainMenu
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

        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.ShowCursorForUI();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
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

    // =========================================================
    // INTRO SCENE
    // =========================================================

    public void IntroSceneLoaded()
    {
        DebugFlow(
            "Intro scene loaded. Looking for cutscene."
        );

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

        IntroCutscene intro =
            FindAnyObjectByType<IntroCutscene>();

        if (intro == null)
        {
            Debug.LogWarning(
                "[GameFlow] No intro cutscene found. " +
                "Going directly to Lobby."
            );

            IntroFinished();

            return;
        }

        DebugFlow(
            "Intro cutscene found. Playing."
        );

        intro.PlayIntro();
    }

    public void OpenCredits()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Credits
            );
        }
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
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetTutorial();
        }

        DebugFlow(
            "Starting new game. Difficulty: "
            + difficulty
            + " | Gender: "
            + gender
        );

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

        RefreshSceneReferences();

        GameStateManager.Instance.SetDifficulty(
            difficulty
        );

        GameStateManager.Instance.SetPlayerGender(
            gender
        );

        setPlayerAnimator();

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

        PlayerHealth playerHealth =
            player != null
                ? player.GetComponent<PlayerHealth>()
                : FindAnyObjectByType<PlayerHealth>(
                    FindObjectsInactive.Include
                );

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        if (newGamePreparationCoroutine != null)
        {
            StopCoroutine(
                newGamePreparationCoroutine
            );
        }

        newGamePreparationCoroutine =
            StartCoroutine(
                StartNewGameAfterPlayerReset()
            );
    }

    // =========================================================
    // GENDER ANIMATOR
    // =========================================================

    public void setPlayerAnimator()
    {
        if (player == null)
        {
            RefreshSceneReferences();
        }

        if (player == null)
        {
            Debug.LogError(
                "[GameFlow] Player is missing. " +
                "Cannot set gender animator."
            );

            return;
        }

        PlayerMovement playerMovement =
            player.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError(
                "[GameFlow] PlayerMovement component is missing on the player."
            );

            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }

        bool male =
            GameStateManager.Instance.GetPlayerGender()
            == GameStateManager.PlayerGender.Male;

        string avatarName =
            male ? "Nephew" : "Niece";

        GameObject avatar =
            FindGenderAvatarInScene(
                avatarName
            );

        if (avatar == null)
        {
            Debug.LogError(
                "[GameFlow] Could not find gender avatar '"
                + avatarName
                + "'."
            );

            return;
        }

        if (!avatar.activeSelf)
        {
            avatar.SetActive(true);
        }

        Animator genderAnimator =
            avatar.GetComponent<Animator>();

        if (genderAnimator == null)
        {
            genderAnimator =
                avatar.GetComponentInChildren<Animator>(
                    true
                );
        }

        if (genderAnimator == null)
        {
            Debug.LogError(
                "[GameFlow] No Animator found on gender avatar '"
                + avatarName
                + "'."
            );

            return;
        }

        playerMovement.setAnimator(
            genderAnimator
        );
    }

    private static GameObject FindGenderAvatarInScene(
        string avatarName)
    {
        GameObject active =
            GameObject.Find(avatarName);

        if (active != null)
        {
            return active;
        }

        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform =
                transforms[i];

            if (
                transform == null ||
                transform.name != avatarName
            )
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

    // =========================================================
    // LOBBY RESPAWN
    // =========================================================

    private PlayerRespawnPoint ResolveLobbyRespawnPoint()
    {
        TeleportDestination[] destinations =
            FindObjectsByType<TeleportDestination>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < destinations.Length; i++)
        {
            TeleportDestination destination =
                destinations[i];

            if (destination == null)
            {
                continue;
            }

            if (
                destination.GetDestinationID()
                != "PlayerSpawn"
            )
            {
                continue;
            }

            PlayerRespawnPoint preferred =
                destination.GetComponent<PlayerRespawnPoint>();

            if (preferred != null)
            {
                return preferred;
            }
        }

        PlayerRespawnPoint[] respawnPoints =
            FindObjectsByType<PlayerRespawnPoint>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < respawnPoints.Length; i++)
        {
            PlayerRespawnPoint candidate =
                respawnPoints[i];

            if (candidate == null)
            {
                continue;
            }

            TeleportDestination destination =
                candidate.GetComponent<TeleportDestination>();

            if (
                destination != null &&
                destination.GetDestinationID()
                == "PlayerFinalSpawn"
            )
            {
                continue;
            }

            if (
                candidate.gameObject.name.Contains(
                    "Final"
                )
            )
            {
                continue;
            }

            return candidate;
        }

        return FindAnyObjectByType<PlayerRespawnPoint>();
    }

    // =========================================================
    // RETURN FROM CASE
    // =========================================================

    public void HandleReturnedFromCase(
        bool playDayScreen,
        int day)
    {
        SetGameplayHUDVisible(true);

        ApplyLobbyChangesToCurrentScene();

        if (playDayScreen && day > 0)
        {
            if (dayScreenCutscene == null)
            {
                dayScreenCutscene =
                    DayScreenCutscene.Instance;
            }

            if (dayScreenCutscene == null)
            {
                dayScreenCutscene =
                    FindAnyObjectByType<DayScreenCutscene>(
                        FindObjectsInactive.Include
                    );
            }

            if (dayScreenCutscene != null)
            {
                dayScreenCutscene.PlayDayScreenAfterLobbyLoad(
                    day
                );
            }
            else
            {
                Debug.LogWarning(
                    "[GameFlow] DayScreenCutscene missing after case return."
                );

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayMusic(
                        AudioManager.MusicType.Lobby
                    );

                    AudioManager.Instance.PlayAmbience(
                        AudioManager.AmbienceType.Lobby
                    );
                }
            }

            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance.CreateDaySnapshot();
            }
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Lobby
            );

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Lobby
            );
        }

        ObjectiveAndDayOverlay[] overlays =
            FindObjectsByType<ObjectiveAndDayOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < overlays.Length; i++)
        {
            if (overlays[i] != null)
            {
                overlays[i].RefreshNow();
            }
        }
    }

    // =========================================================
    // FIRST GAME SEQUENCE
    // =========================================================

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

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Cutscene
        );

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        SetGameplayHUDVisible(false);

        GameUICursorController cursorController =
            GameUICursorController.Instance;

        if (cursorController != null)
        {
            cursorController.HideCursorForGameplay();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }

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
            "Intro video first frame is ready. Hiding menu."
        );

        StartCoroutine(
            HideMenuAfterVideoFrame()
        );
    }

    private IEnumerator HideMenuAfterVideoFrame()
    {
        yield return new WaitForEndOfFrame();

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

    public GameObject HealthBarObject => HealthBar;
    public GameObject StaminaBarObject => StaminaBar;

    public void SetGameplayHUDVisible(bool visible)
    {
        SetToolbarVisible(visible);
        SetHealthBarVisible(visible);
        SetStaminaBarVisible(visible);

        ObjectiveAndDayOverlay[] overlays =
            FindObjectsByType<ObjectiveAndDayOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (
            ObjectiveAndDayOverlay overlay
            in overlays
        )
        {
            if (overlay != null)
            {
                overlay.SetOverlayVisible(visible);
                overlay.RefreshNow();
            }
        }
    }

    public void StartIntro()
    {
        DebugFlow(
            "Starting first intro cutscene."
        );

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

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Cutscene
        );

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        if (mainMenuGroup != null)
        {
            mainMenuGroup.SetActive(false);
        }

        if (gameModeSelectionScreen != null)
        {
            gameModeSelectionScreen.SetActive(false);
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

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

        if (dayScreenCutscene == null)
        {
            dayScreenCutscene =
                DayScreenCutscene.Instance;
        }

        if (dayScreenCutscene == null)
        {
            dayScreenCutscene =
                FindAnyObjectByType<DayScreenCutscene>(
                    FindObjectsInactive.Include
                );
        }

        if (
            dayScreenCutscene != null &&
            dayScreenCutscene.IsPlaying
        )
        {
            dayScreenCutscene.StopDayScreen();
            return;
        }

        bool skippedOpeningSequence = false;

        if (
            firstGameCutscene != null &&
            firstGameCutscene.IsSequenceRunning
        )
        {
            // Skip only the currently playing video, not the whole sequence.
            firstGameCutscene.SkipCurrentVideo();
            return;
        }
        else if (
            introCutscene != null &&
            introCutscene.IsPlaying
        )
        {
            introCutscene.StopCutscene();

            skippedOpeningSequence = true;
            IntroFinished(true);
        }
        // No active cutscene means F9 does nothing. Never forcibly complete
        // the opening sequence or bypass the tutorial.
        if (!skippedOpeningSequence)
        {
            Debug.Log("[GameFlowManager] F9 found no skippable active cutscene.");
        }
    }

    public void CheckForIntroCutscene()
    {
        DebugFlow(
            "Checking for intro cutscene."
        );

        IntroCutscene intro =
            FindAnyObjectByType<IntroCutscene>();

        if (intro != null)
        {
            DebugFlow(
                "Intro cutscene found."
            );

            return;
        }

        Debug.LogWarning(
            "[GameFlow] No intro cutscene found. " +
            "Going directly to the Lobby."
        );

        IntroFinished();
    }

    public void IntroFinished(bool showDayScreen = true)
    {
        DebugFlow(
            "Intro cutscene finished. Show Day Screen = " +
            showDayScreen
        );

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] GameStateManager.Instance is missing."
            );

            return;
        }

        // A normal intro completion shows the Day 1 screen. A debug F9
        // skip is treated as a complete cutscene override and therefore
        // goes directly into gameplay.
        StartDay(
            1,
            showDayScreen
        );
    }

    // =========================================================
    // DAY FLOW
    // =========================================================

    public void StartDay(int day)
    {
        StartDay(
            day,
            true
        );
    }

    public void StartDay(
        int day,
        bool createDaySnapshot)
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Game Flow] GameStateManager is missing."
            );

            return;
        }

        GameStateManager.Instance.SetDay(day);

        ApplyLobbyChangesToCurrentScene();

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.SetTheaterDay(day);
        }

        createDaySnapshotOnLobbyLoad =
            createDaySnapshot;

        playDayScreenAfterLobbyLoad =
            createDaySnapshot;

        pendingDayScreenDay = day;

        if (dayScreenCutscene == null)
        {
            dayScreenCutscene =
                DayScreenCutscene.Instance;
        }

        DebugFlow(
            "Starting Day " + day
        );

        OpenLobby();
    }

    public void CompleteDay()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] Cannot complete day because " +
                "GameStateManager is missing."
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

        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance
                .UnlockDayCompletion(
                    completedDay
                );
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
        EnsureInGamePlayTestScene(
            FinishOpenLobby
        );
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

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Lobby
        );

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Lobby
            );

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Lobby
            );
        }

        // IMPORTANT:
        // Re-resolve the lobby spawn immediately before teleporting.
        // This prevents a stale PlayerRespawnPoint reference from a
        // previous scene/teleport being reused.
        RefreshSceneReferences();

        StartCoroutine(
            MovePlayerToRespawnPoint()
        );

        SetToolbarVisible(true);
        SetHealthBarVisible(true);
        SetStaminaBarVisible(true);

        if (
            playDayScreenAfterLobbyLoad &&
            pendingDayScreenDay > 0
        )
        {
            if (dayScreenCutscene == null)
            {
                dayScreenCutscene =
                    DayScreenCutscene.Instance;
            }

            if (dayScreenCutscene != null)
            {
                dayScreenCutscene
                    .PlayDayScreenAfterLobbyLoad(
                        pendingDayScreenDay
                    );
            }
            else
            {
                Debug.LogWarning(
                    "[GameFlow] DayScreenCutscene is missing; " +
                    "Day screen skipped."
                );
                if (pendingDayScreenDay == 1)
                    startDay1TutorialAfterLobbyRespawn = true;
            }

            playDayScreenAfterLobbyLoad = false;
            pendingDayScreenDay = -1;
        }
        else if (GameStateManager.Instance.GetCurrentDay() == 1)
        {
            startDay1TutorialAfterLobbyRespawn = true;
        }
    }

    private IEnumerator MovePlayerToRespawnPoint()
    {
        DebugFlow(
            "Preparing to move Player to PlayerRespawnPoint."
        );

        // Re-resolve here as an additional safety check.
        PlayerRespawnPoint resolvedRespawn =
            ResolveLobbyRespawnPoint();

        if (resolvedRespawn != null)
        {
            playerRespawnPoint = resolvedRespawn;
        }

        if (playerRespawnPoint == null)
        {
            Debug.LogError(
                "[GameFlow] Player Respawn Point could not be resolved."
            );

            yield break;
        }

        if (player == null)
        {
            GameObject scenePlayer =
                GameObject.FindGameObjectWithTag("Player");

            if (scenePlayer != null)
            {
                player = scenePlayer;
            }
        }

        if (player == null)
        {
            Debug.LogError(
                "[GameFlow] Player could not be resolved."
            );

            yield break;
        }

        DebugFlow(
            "Player and PlayerRespawnPoint references found."
        );

        yield return null;

        playerRespawnPoint.SpawnPlayer(player);

        PlayerHealth health =
            player.GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.ResetHealth();
        }

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }

        PlayerStateManager state =
            player.GetComponent<PlayerStateManager>();

        if (state != null)
        {
            state.ResetToDefault();
        }

        DebugFlow(
            "Player successfully moved to PlayerRespawnPoint."
        );

        if (GameResetManager.Instance != null)
        {
            if (
                !GameResetManager.Instance
                    .HasGameStartSnapshot()
            )
            {
                GameResetManager.Instance
                    .CreateGameStartSnapshot();

                DebugFlow(
                    "Game-start snapshot created."
                );
            }
        }

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance
                .ApplyDaySnapshotToCurrentPlayer();
        }

        if (createDaySnapshotOnLobbyLoad)
        {
            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance
                    .CreateDaySnapshot();

                DebugFlow(
                    "Beginning-of-day snapshot created."
                );
            }

            createDaySnapshotOnLobbyLoad = false;
        }

        // F9 can skip the day-screen video. Day 1 still always enters the
        // tutorial once the player is back at the lobby spawn.
        if (startDay1TutorialAfterLobbyRespawn &&
            GameStateManager.Instance != null &&
            GameStateManager.Instance.GetCurrentDay() == 1 &&
            TutorialManager.Instance != null)
        {
            startDay1TutorialAfterLobbyRespawn = false;
            TutorialManager.Instance.BeginDay1Tutorial();
        }
    }

    // =========================================================
    // PLAY
    // =========================================================

    public void StartPlay(int playID)
    {
        DebugFlow(
            "Starting Play "
            + playID
            + " in the current scene."
        );

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.WatchingPlay
            );
        }

        StartCoroutine(
            InitializePlayAfterLoad(playID)
        );
    }

    private IEnumerator InitializePlayAfterLoad(
        int playID)
    {
        yield return new WaitForSeconds(
            transitionDelay
        );

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.StartPlay(
                playID
            );
        }
    }

    public void PlayFinished()
    {
        if (TheaterManager.Instance == null)
        {
            return;
        }

        if (
            TheaterManager.Instance
                .HasCaseAfterCurrentPlay()
        )
        {
            TheaterManager.Instance
                .StartCaseAfterPlay();
        }
        else
        {
            TheaterManager.Instance
                .StartNextPlay();
        }
    }

    // =========================================================
    // CASE
    // =========================================================

    public void StartCase(int caseID)
    {
        DebugFlow(
            "Starting Case "
            + caseID
            + " in the current scene."
        );

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Case
            );
        }

        StartCoroutine(
            InitializeCaseAfterLoad(caseID)
        );
    }

    private IEnumerator InitializeCaseAfterLoad(
        int caseID)
    {
        yield return new WaitForSeconds(
            transitionDelay
        );

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.StartCase(
                caseID
            );
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Case
            );

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Case
            );
        }
    }

    public void CaseCompleted()
    {
        // OpenLobby();
    }

    // =========================================================
    // FINAL PLAY
    // =========================================================

    public void StartFinalPlay()
    {
        if (TheaterManager.Instance == null)
        {
            return;
        }

        TheaterManager.PlayData finalPlay =
            TheaterManager.Instance.GetFinalPlay();

        if (finalPlay == null)
        {
            Debug.LogError(
                "No Final Play configured."
            );

            return;
        }

        StartPlay(
            finalPlay.playID
        );
    }

    public void FinalPlayFinished()
    {
        ApplyLobbyChangesToCurrentScene();

        StartBossFight();
    }

    // =========================================================
    // BOSS
    // =========================================================

    public void StartBossFight()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.BossFight
            );
        }

        // The fourth play has ended and the boss fight is starting now.
        // Force every theater puppet's mini health bar visible immediately,
        // rather than waiting for the activation-delay coroutine/update loop.
        BossFightPuppetActivation[] puppetActivations = FindObjectsByType<BossFightPuppetActivation>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < puppetActivations.Length; i++)
        {
            if (puppetActivations[i] != null)
                puppetActivations[i].ShowHealthBarsNow();
        }

        // The dolls sitting in the theater (including ones that do not own a
        // puppet-activation component) must have their slider canvases shown
        // the moment the boss fight begins.
        EnemyHealthBar[] enemyHealthBars = FindObjectsByType<EnemyHealthBar>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < enemyHealthBars.Length; i++)
        {
            if (enemyHealthBars[i] != null)
                enemyHealthBars[i].ShowForBossFight();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Boss
            );

            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Boss
            );
        }

        FinalBossController bossController =
            ResolveFinalBossController();

        if (bossController != null)
        {
            bossController.BeginBossSpawnSequence();
        }
        else
        {
            Debug.LogError(
                "[GameFlow] FinalBossController could not be resolved. " +
                "Set Final Boss Controller Object Name / Final Boss Object Name " +
                "in the GameFlowManager Inspector. Current values: '"
                + finalBossControllerObjectName
                + "' / '"
                + finalBossObjectName
                + "'."
            );
        }
    }

    private FinalBossController ResolveFinalBossController()
    {
        FinalBossController byControllerName =
            FindFinalBossControllerByObjectName(
                finalBossControllerObjectName
            );

        if (byControllerName != null)
        {
            return byControllerName;
        }

        FinalBossController existing =
            FindAnyObjectByType<FinalBossController>(
                FindObjectsInactive.Include
            );

        if (existing != null)
        {
            return existing;
        }

        GameObject bossObject =
            FindSceneObjectByNameAcrossLoadedScenes(
                finalBossObjectName
            );

        if (bossObject == null)
        {
            FinalBossHealth health =
                FindAnyObjectByType<FinalBossHealth>(
                    FindObjectsInactive.Include
                );

            if (health != null)
            {
                bossObject = health.gameObject;
            }
        }

        if (bossObject == null)
        {
            return null;
        }

        string hostName =
            string.IsNullOrWhiteSpace(
                finalBossControllerObjectName
            )
                ? "FinalBossController"
                : finalBossControllerObjectName.Trim();

        GameObject host =
            new GameObject(hostName);

        SceneManager.MoveGameObjectToScene(
            host,
            bossObject.scene
        );

        FinalBossController created =
            host.AddComponent<FinalBossController>();

        created.SetFinalBoss(
            bossObject
        );

        DebugFlow(
            "Created FinalBossController host '"
            + hostName
            + "' bound to boss '"
            + bossObject.name
            + "'."
        );

        return created;
    }

    private FinalBossController
        FindFinalBossControllerByObjectName(
            string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject host =
            FindSceneObjectByNameAcrossLoadedScenes(
                objectName.Trim()
            );

        if (host == null)
        {
            return null;
        }

        FinalBossController controller =
            host.GetComponent<FinalBossController>();

        if (controller == null)
        {
            controller =
                host.GetComponentInChildren<FinalBossController>(
                    true
                );
        }

        if (controller != null)
        {
            return controller;
        }

        if (
            host.GetComponent<FinalBossHealth>() != null ||
            host.GetComponentInParent<FinalBossHealth>() != null
        )
        {
            return null;
        }

        return host.AddComponent<FinalBossController>();
    }

    private GameObject
        FindSceneObjectByNameAcrossLoadedScenes(
            string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        string trimmedName =
            objectName.Trim();

        for (
            int i = 0;
            i < SceneManager.sceneCount;
            i++
        )
        {
            Scene scene =
                SceneManager.GetSceneAt(i);

            if (
                !scene.IsValid() ||
                !scene.isLoaded
            )
            {
                continue;
            }

            GameObject found =
                FindSceneObjectByName(
                    scene,
                    trimmedName
                );

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    // =========================================================
    // DEBUG - FINAL BOSS
    // =========================================================

    public void DebugStartFinalBossFight()
    {
        EnsureInGamePlayTestScene(
            FinishDebugStartFinalBossFight
        );
    }

    private void FinishDebugStartFinalBossFight()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[GameFlow] Cannot start debug final boss: " +
                "GameStateManager is missing."
            );

            return;
        }

        GameStateManager.Instance.SetDay(4);

        GameStateManager.Instance.CompleteFinalPlay();

        ApplyLobbyChangesToCurrentScene();

        StartBossFight();

        DebugFlow(
            "DEBUG F10: Day 4 final play completed. " +
            "BossChanges enabled and final boss started."
        );
    }

    public void FinalBossDefeated()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.DefeatFinalBoss();
        }

        StartFinalCutscene();
    }

    // =========================================================
    // CUTSCENE
    // =========================================================

    public void StartFinalCutscene()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene
            );
        }

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

    public void StartGachaWin()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene
            );
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        SetGameplayHUDVisible(false);

        OpenCredits();
    }

    public void FinalCutsceneFinished()
    {
        if (
            AchievementManager.Instance != null &&
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore()
        )
        {
            AchievementManager.Instance
                .UnlockHardcoreCompletion();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .CompleteFinalCutscene();
        }

        SetToolbarVisible(false);
        SetHealthBarVisible(false);
        SetStaminaBarVisible(false);

        OpenCredits();

        StartCoroutine(
            ReturnToMainMenuAfterFinalCredits()
        );
    }

    private IEnumerator
        ReturnToMainMenuAfterFinalCredits()
    {
        if (finalCreditsDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                finalCreditsDuration
            );
        }

        OpenMainMenu();
    }

    // =========================================================
    // LOSS
    // =========================================================

    public void PlayerDied()
    {
        DebugFlow(
            "Player died."
        );

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.HandlePlayerDeath();
        }
    }

    public void TimerExpired()
    {
        HandleLoss();
    }

    private void HandleLoss()
    {
        if (
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore()
        )
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
        if (GameStateManager.Instance == null)
        {
            return;
        }

        int day =
            GameStateManager.Instance.GetCurrentDay();

        DebugFlow(
            "Restarting Day " + day
        );

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

        StartDay(
            day,
            false
        );
    }

    private void RestartEntireGame()
    {
        DebugFlow(
            "Restarting Entire Game"
        );

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetEntireGame();
        }
        else if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetEntireGame();
        }

        StartDay(1);
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

        StartDay(
            day,
            false
        );
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

        GameStateManager.DifficultyMode difficulty =
            GameStateManager.Instance.GetDifficulty();

        GameStateManager.PlayerGender gender =
            GameStateManager.Instance.GetPlayerGender();

        DebugFlow(
            "Hardcore death. Resetting entire game."
        );

        if (GameResetManager.Instance != null)
        {
            GameResetManager.Instance.ResetEntireGame();
        }
        else
        {
            GameStateManager.Instance.ResetEntireGame();
        }

        GameStateManager.Instance.SetDifficulty(
            difficulty
        );

        GameStateManager.Instance.SetPlayerGender(
            gender
        );

        StartDay(1);
    }

    // =========================================================
    // PLAYER RESET
    // =========================================================

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

        playerRespawnPoint.SpawnPlayer(
            player
        );

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }

        PlayerLook look =
            player.GetComponent<PlayerLook>();

        if (look != null)
        {
            look.ResetLook();
        }

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

    // =========================================================
    // DEBUG HOTKEYS
    // =========================================================

    private void Update()
    {
        if (
            !enableDebugHotkeys ||
            Keyboard.current == null
        )
        {
            return;
        }

        // F11 - reset achievements.
        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.ResetAchievements();

                Debug.Log(
                    "[GameFlowManager] F11 pressed. Achievements reset."
                );
            }
            else
            {
                Debug.LogWarning(
                    "[GameFlowManager] F11 pressed, but AchievementManager is missing."
                );
            }

            return;
        }

        // F9 - skip active play/cutscene.
        if (!Keyboard.current.f9Key.wasPressedThisFrame)
        {
            return;
        }

        // F9 is a cutscene/play debug control, never a tutorial skip.
        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.IsTutorialRunning)
        {
            Debug.Log("[GameFlowManager] F9 ignored while the tutorial is active.");
            return;
        }

        Debug.Log(
            "[GameFlowManager] F9 debug skip requested."
        );

        // The actual theater chair uses Chair.cs to run the video. This must
        // be checked before ChairPlayController; otherwise F9 can find the
        // secondary controller while the real chair play keeps running.
        Chair chair =
            FindAnyObjectByType<Chair>(
                FindObjectsInactive.Include
            );

        if (chair != null && chair.DebugSkipCurrentPlay())
        {
            return;
        }

        // Keep compatibility with the separate ChairPlayController system.
        ChairPlayController chairPlayController =
            FindAnyObjectByType<ChairPlayController>(
                FindObjectsInactive.Include
            );

        if (chairPlayController != null)
        {
            TheaterManager theaterManager =
                TheaterManager.Instance;

            if (
                theaterManager != null &&
                theaterManager.IsPlayActive()
            )
            {
                Debug.Log(
                    "[GameFlowManager] Active theater play detected on " +
                    "ChairPlayController. Skipping play while preserving progression."
                );

                chairPlayController.SkipCurrentPlay();
                return;
            }
        }

        SkipCurrentCutscene();
    }

    private void DebugFlow(string message)
    {
        if (!debugFlow)
        {
            return;
        }

        Debug.Log(
            "[Game Flow] " + message
        );
    }
}
