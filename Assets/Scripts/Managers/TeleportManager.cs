using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TeleportManager : MonoBehaviour
{
    // =========================================================
    // DEBUG TARGET
    // =========================================================

    [System.Serializable]
    private class DebugTarget
    {
        public TeleportDestination sameSceneDestination;

        public string sceneName;

        public string destinationID;
    }


    // =========================================================
    // SINGLETON
    // =========================================================

    public static TeleportManager Instance { get; private set; }


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugTeleport = true;


    // =========================================================
    // CURRENT TELEPORT
    // =========================================================

    private GameObject teleportingPlayer;

    private TeleportPortal pendingPortal;

    private string pendingSceneName;

    private string pendingDestinationID;

    private TeleportPortal.TeleportType pendingTeleportType;

    // When a ReturnFromCase portal advances the day, play that day's
    // screen after the lobby finishes loading (normal progression).
    private bool pendingReturnPlayDayScreen;

    private int pendingReturnDayScreenDay = -1;


    // =========================================================
    // LOADING SCREEN
    // =========================================================

    [Header("Scene Transition")]

    [SerializeField]
    private LoadingScreenManager loadingScreenManager;


    // =========================================================
    // DEBUG DESTINATIONS
    // =========================================================

    [Header("F-Key Debug Overrides")]

    [SerializeField]
    private TeleportDestination f1LobbyDay1;

    [SerializeField]
    private TeleportDestination f2Level1;

    [SerializeField]
    private TeleportDestination f3LobbyDay2;

    [SerializeField]
    private TeleportDestination f4Level2;

    [SerializeField]
    private TeleportDestination f5LobbyDay3;

    [SerializeField]
    private TeleportDestination f6Level3;

    [SerializeField]
    private TeleportDestination f7LobbyDay4;

    [SerializeField]
    private TeleportDestination f10FinalBoss;


    [SerializeField]
    private DebugTarget f1Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f2Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f3Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f4Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f5Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f6Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f7Target = new DebugTarget();

    [SerializeField]
    private DebugTarget f10Target = new DebugTarget();

    // Set while F10 is waiting for its destination teleport to finish.
    private bool pendingDebugFinalBoss;
    private bool pendingDebugDay1Tutorial;


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

        EnsureLoadingScreen();
    }


    // =========================================================
    // NORMAL TELEPORT
    // =========================================================

    public void Teleport(
        TeleportPortal portal,
        GameObject player)
    {
        // -----------------------------------------------------
        // VALIDATE PORTAL
        // -----------------------------------------------------

        if (portal == null)
        {
            Debug.LogError(
                "[Teleport] Portal is null."
            );

            return;
        }


        // -----------------------------------------------------
        // VALIDATE PLAYER
        // -----------------------------------------------------

        if (player == null)
        {
            Debug.LogError(
                "[Teleport] Player is null."
            );

            return;
        }


        // -----------------------------------------------------
        // PREVENT DOUBLE TELEPORT
        // -----------------------------------------------------

        if (teleportingPlayer != null)
        {
            Debug.LogWarning(
                "[Teleport] A teleport is already in progress."
            );

            return;
        }


        // -----------------------------------------------------
        // VALID DESTINATION
        // -----------------------------------------------------

        if (!portal.HasValidDestination())
        {
            Debug.LogError(
                "[Teleport] Portal "
                + portal.gameObject.name
                + " has no valid destination."
            );

            return;
        }


        // -----------------------------------------------------
        // STORE TELEPORT INFORMATION
        // -----------------------------------------------------

        teleportingPlayer = player;

        pendingPortal = portal;

        pendingSceneName =
            portal.GetDestinationSceneName();

        pendingDestinationID =
            portal.GetDestinationID();

        pendingTeleportType =
            portal.GetTeleportType();


        // -----------------------------------------------------
        // CAPTURE PRE-CASE CHECKPOINT
        // -----------------------------------------------------
        //
        // This happens BEFORE the level scene is loaded. If the
        // player dies inside the case, GameResetManager restores
        // this exact state instead of allowing case pickups to
        // leak into the next attempt.

        if (
            pendingTeleportType ==
            TeleportPortal.TeleportType.EnterCase
        )
        {
            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance
                    .CreateCaseEntrySnapshot();
            }
            else
            {
                Debug.LogWarning(
                    "[Teleport] GameResetManager is missing. " +
                    "No pre-case inventory checkpoint was captured."
                );
            }
        }


        // -----------------------------------------------------
        // KEEP PLAYER ALIVE DURING SCENE CHANGE
        // -----------------------------------------------------

        DontDestroyOnLoad(
            teleportingPlayer
        );


        // -----------------------------------------------------
        // TELEPORT EFFECT
        // -----------------------------------------------------

        PlayTeleportEffect(
            portal
        );


        // -----------------------------------------------------
        // DAY CHANGE
        // -----------------------------------------------------

        pendingReturnPlayDayScreen = false;
        pendingReturnDayScreenDay = -1;

        if (portal.ChangesDay())
        {
            ChangeDay(
                portal.GetDestinationDay()
            );

            if (portal.GetTeleportType() ==
                TeleportPortal.TeleportType.ReturnFromCase)
            {
                pendingReturnPlayDayScreen = true;
                pendingReturnDayScreenDay =
                    portal.GetDestinationDay();
            }
        }


        // -----------------------------------------------------
        // CASE STATE
        // -----------------------------------------------------

        HandleCaseState(
            portal
        );


        // -----------------------------------------------------
        // START TELEPORT
        // -----------------------------------------------------

        StartCoroutine(
            FinishTeleportAfterDelay(
                portal.GetTeleportDelay()
            )
        );
    }


    // =========================================================
    // TELEPORT DELAY
    // =========================================================

    private IEnumerator FinishTeleportAfterDelay(
        float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(
                delay
            );
        }

        yield return FinishTeleport();
    }


    // =========================================================
    // FINISH TELEPORT
    // =========================================================

    private IEnumerator FinishTeleport()
    {
        yield return null;


        // -----------------------------------------------------
        // VALIDATE
        // -----------------------------------------------------

        if (pendingPortal == null)
        {
            ResetTeleportState();

            yield break;
        }


        // =====================================================
        // CROSS-SCENE TELEPORT
        // =====================================================

        if (pendingPortal.HasCrossSceneDestination())
        {
            // -------------------------------------------------
            // GAME STATE
            // -------------------------------------------------

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetGameMode(
                    GameStateManager.GameMode.Loading
                );
            }


            // -------------------------------------------------
            // PLAYER STATE
            // -------------------------------------------------

            if (PlayerStateManager.Instance != null)
            {
                PlayerStateManager.Instance.SetState(
                    PlayerStateManager.PlayerState.NoMovementNoCamera
                );
            }


            // -------------------------------------------------
            // LOADING SCREEN
            // -------------------------------------------------

            EnsureLoadingScreen();

            if (loadingScreenManager != null)
            {
                loadingScreenManager.Show();
            }


            // -------------------------------------------------
            // DEBUG
            // -------------------------------------------------

            DebugTeleport(
                "Loading scene '"
                + pendingSceneName
                + "' for destination '"
                + pendingDestinationID
                + "'."
            );


            // -------------------------------------------------
            // LOAD THE EXACT REQUESTED SCENE
            // -------------------------------------------------

            // For EnterCase portals, start/rebind the case after the
            // destination scene has loaded. Same-scene EnterCase still
            // starts immediately in HandleCaseState().
            int caseIDAfterLoad = 0;

            if (pendingTeleportType ==
                TeleportPortal.TeleportType.EnterCase)
            {
                caseIDAfterLoad = ResolveEnterCaseID(
                    pendingPortal,
                    pendingSceneName,
                    pendingDestinationID
                );
            }

            yield return LoadSceneAndFinishTeleport(
                pendingSceneName,
                pendingDestinationID,
                pendingTeleportType,
                caseIDAfterLoad
            );

            yield break;
        }


        // =====================================================
        // SAME-SCENE TELEPORT
        // =====================================================

        TeleportDestination destination =
            pendingPortal.GetDestination();


        if (destination == null)
        {
            Debug.LogError(
                "[Teleport] Same-scene destination is missing."
            );

            ResetTeleportState();

            yield break;
        }


        // -----------------------------------------------------
        // MOVE PLAYER
        // -----------------------------------------------------

        MovePlayerToDestination(
            teleportingPlayer,
            destination
        );


        // -----------------------------------------------------
        // RESTORE GAMEPLAY
        // -----------------------------------------------------

        RestoreGameplayState();


        // -----------------------------------------------------
        // SAME-SCENE RETURN FROM CASE
        // -----------------------------------------------------

        if (pendingTeleportType ==
                TeleportPortal.TeleportType.ReturnFromCase &&
            GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.HandleReturnedFromCase(
                pendingReturnPlayDayScreen,
                pendingReturnDayScreenDay
            );
        }

        pendingReturnPlayDayScreen = false;
        pendingReturnDayScreenDay = -1;


        // -----------------------------------------------------
        // RESET
        // -----------------------------------------------------

        ResetTeleportState();
    }


    private IEnumerator LoadSceneAndFinishTeleport(
    string sceneName,
    string destinationID,
    TeleportPortal.TeleportType teleportType,
    int caseIDAfterLoad = 0)
    {
        // =========================================================
        // VALIDATE SCENE
        // =========================================================

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "[Teleport] Scene name is empty."
            );

            FailSceneTeleport();

            yield break;
        }


        // =========================================================
        // VALIDATE DESTINATION
        // =========================================================

        if (string.IsNullOrWhiteSpace(destinationID))
        {
            Debug.LogError(
                "[Teleport] Destination ID is empty."
            );

            FailSceneTeleport();

            yield break;
        }


        // =========================================================
        // FIND BUILD INDEX
        // =========================================================

        int buildIndex =
            SceneUtility.GetBuildIndexByScenePath(
                sceneName
            );


        if (buildIndex < 0)
        {
            Debug.LogError(
                "[Teleport] Scene '"
                + sceneName
                + "' is NOT available in the Build Profile."
            );

            FailSceneTeleport();

            yield break;
        }


        Debug.Log(
            "[Teleport] Scene '"
            + sceneName
            + "' has Build Index "
            + buildIndex
            + "."
        );


        // =========================================================
        // GIVE LOADING SCREEN A FRAME TO APPEAR
        // =========================================================

        Debug.Log(
            "[Teleport] Preparing loading screen before "
            + "loading scene."
        );


        yield return null;


        // =========================================================
        // CREATE SCENE LOADED CALLBACK
        // =========================================================
        //
        // IMPORTANT:
        // The rest of the teleport is deliberately performed
        // inside this callback.
        //
        // We do NOT wait for the callback from this coroutine.
        //
        // This prevents the teleport from depending on the
        // coroutine continuing after the scene transition.
        //
        // =========================================================

        UnityEngine.Events.UnityAction<Scene, LoadSceneMode>
            onSceneLoaded =
            null;


        onSceneLoaded =
            (loadedScene, loadMode) =>
            {
                // =================================================
                // IGNORE OTHER SCENES
                // =================================================

                if (loadedScene.name != sceneName)
                {
                    return;
                }


                // =================================================
                // REMOVE CALLBACK IMMEDIATELY
                // =================================================

                SceneManager.sceneLoaded -= onSceneLoaded;


                // =================================================
                // DEBUG
                // =================================================

                Debug.Log(
                    "[Teleport] sceneLoaded event received for '"
                    + loadedScene.name
                    + "'."
                );


                // =================================================
                // VERIFY SCENE
                // =================================================

                if (!loadedScene.IsValid())
                {
                    Debug.LogError(
                        "[Teleport] Loaded scene is invalid."
                    );

                    FailSceneTeleport();

                    return;
                }


                if (!loadedScene.isLoaded)
                {
                    Debug.LogError(
                        "[Teleport] Loaded scene reports "
                        + "isLoaded = FALSE."
                    );

                    FailSceneTeleport();

                    return;
                }


                Debug.Log(
                    "[Teleport] Scene loaded successfully: "
                    + loadedScene.name
                );


                // =================================================
                // VERIFY ACTIVE SCENE
                // =================================================

                Scene activeScene =
                    SceneManager.GetActiveScene();


                Debug.Log(
                    "[Teleport] Active scene after load: "
                    + activeScene.name
                );


                // =================================================
                // SET ACTIVE SCENE IF NECESSARY
                // =================================================

                if (activeScene.name != sceneName)
                {
                    Debug.Log(
                        "[Teleport] Setting loaded scene as "
                        + "the active scene."
                    );


                    if (!SceneManager.SetActiveScene(
                        loadedScene))
                    {
                        Debug.LogError(
                            "[Teleport] Failed to set '"
                            + loadedScene.name
                            + "' as the active scene."
                        );

                        FailSceneTeleport();

                        return;
                    }
                }


                // =================================================
                // VERIFY ACTIVE SCENE
                // =================================================

                activeScene =
                    SceneManager.GetActiveScene();


                if (activeScene.name != sceneName)
                {
                    Debug.LogError(
                        "[Teleport] Wrong scene is active."
                    );

                    Debug.LogError(
                        "[Teleport] Expected: "
                        + sceneName
                    );

                    Debug.LogError(
                        "[Teleport] Actual: "
                        + activeScene.name
                    );

                    FailSceneTeleport();

                    return;
                }


                Debug.Log(
                    "[Teleport] Active scene verified: "
                    + activeScene.name
                );


                // =================================================
                // FIND DESTINATION
                // =================================================

                Debug.Log(
                    "[Teleport] Looking for destination '"
                    + destinationID
                    + "'."
                );


                TeleportDestination destination =
                    FindDestinationByID(
                        destinationID
                    );


                if (destination == null)
                {
                    Debug.LogError(
                        "[Teleport] Could not find destination '"
                        + destinationID
                        + "' in scene '"
                        + loadedScene.name
                        + "'."
                    );


                    // ---------------------------------------------
                    // DEBUG ALL DESTINATIONS
                    // ---------------------------------------------

                    TeleportDestination[] allDestinations =
                        FindObjectsByType<TeleportDestination>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None
                        );


                    Debug.Log(
                        "[Teleport] Teleport destinations found: "
                        + allDestinations.Length
                    );


                    foreach (
                        TeleportDestination possibleDestination
                        in allDestinations)
                    {
                        if (possibleDestination == null)
                        {
                            continue;
                        }


                        Debug.Log(
                            "[Teleport] Destination: '"
                            + possibleDestination.gameObject.name
                            + "'"
                            + " | ID: '"
                            + possibleDestination.GetDestinationID()
                            + "'"
                        );
                    }


                    FailSceneTeleport();

                    return;
                }


                Debug.Log(
                    "[Teleport] Destination found: "
                    + destination.gameObject.name
                );


                // =================================================
                // FIND PLAYER
                // =================================================

                if (teleportingPlayer == null)
                {
                    teleportingPlayer =
                        GameObject.FindGameObjectWithTag(
                            "Player"
                        );
                }


                if (teleportingPlayer == null)
                {
                    Debug.LogError(
                        "[Teleport] Player could not be found "
                        + "after loading scene."
                    );

                    FailSceneTeleport();

                    return;
                }


                Debug.Log(
                    "[Teleport] Player found: "
                    + teleportingPlayer.name
                );


                // =================================================
                // MOVE PLAYER
                // =================================================

                Debug.Log(
                    "[Teleport] Moving player to destination '"
                    + destinationID
                    + "'."
                );


                MovePlayerToDestination(
                    teleportingPlayer,
                    destination
                );


                // =================================================
                // HIDE LOADING SCREEN EARLY
                // =================================================
                // The destination scene and player are valid now.
                // Hide the loading screen before optional scene-manager
                // rebinding so a broken optional manager cannot trap
                // the player behind the loading overlay.

                EnsureLoadingScreen();

                if (loadingScreenManager != null)
                {
                    loadingScreenManager.Hide();
                }

                // =================================================
                // REFRESH SCENE REFERENCES
                // =================================================

                Debug.Log(
                    "[Teleport] Refreshing scene references."
                );

                try
                {
                    RebindSceneSystems();
                }
                catch (System.Exception exception)
                {
                    Debug.LogError(
                        "[Teleport] Scene reference refresh threw an "
                        + "exception. Teleport will continue."
                        + exception
                    );
                }


                // =================================================
                // START CASE AFTER CROSS-SCENE LOAD
                // =================================================
                // Both normal level portals and debug level teleports
                // must start CaseManager only after the destination scene
                // exists. Previously this branch only handled debug cases,
                // leaving normal cross-scene portals in Case mode without
                // an active case/objective.
                if (teleportType == TeleportPortal.TeleportType.EnterCase &&
                    CaseManager.Instance != null)
                {
                    int caseToStart = caseIDAfterLoad;

                    if (caseToStart <= 0)
                    {
                        CaseSceneBinding sceneBinding =
                            FindAnyObjectByType<CaseSceneBinding>(
                                FindObjectsInactive.Include
                            );
                        if (sceneBinding != null)
                            caseToStart = sceneBinding.GetCaseID();
                    }

                    if (caseToStart > 0)
                    {
                        CaseManager.Instance.StartCase(caseToStart);

                        if (GameFlowManager.Instance != null)
                        {
                            GameFlowManager.Instance.SetGameplayHUDVisible(true);
                        }

                        ObjectiveAndDayOverlay[] overlays =
                            FindObjectsByType<ObjectiveAndDayOverlay>(
                                FindObjectsInactive.Include,
                                FindObjectsSortMode.None
                            );

                        foreach (ObjectiveAndDayOverlay overlay in overlays)
                        {
                            if (overlay != null)
                            {
                                overlay.SetOverlayVisible(true);
                                overlay.RefreshNow();
                            }
                        }

                        Debug.Log(
                            "[Teleport] Started Case "
                            + caseToStart
                            + " after cross-scene load."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Teleport] EnterCase portal/debug teleport had no valid Case ID after scene load."
                        );
                    }
                }

                // =================================================
                // CASE / LOBBY STATE
                // =================================================

                if (GameStateManager.Instance != null)
                {
                    if (
                        teleportType
                        == TeleportPortal.TeleportType.EnterCase
                    )
                    {
                        GameStateManager.Instance.SetGameMode(
                            GameStateManager.GameMode.Case
                        );
                    }
                    else
                    {
                        GameStateManager.Instance.SetGameMode(
                            GameStateManager.GameMode.Lobby
                        );
                    }
                }


                // =================================================
                // RETURN FROM CASE
                // =================================================

                if (
                    teleportType
                    == TeleportPortal.TeleportType.ReturnFromCase
                )
                {
                    if (CaseManager.Instance != null)
                    {
                        CaseManager.Instance.DeactivateCase();
                    }

                    if (GameFlowManager.Instance != null)
                    {
                        GameFlowManager.Instance.HandleReturnedFromCase(
                            pendingReturnPlayDayScreen,
                            pendingReturnDayScreenDay
                        );
                    }
                    else
                    {
                        ObjectiveAndDayOverlay[] overlays =
                            FindObjectsByType<ObjectiveAndDayOverlay>(
                                FindObjectsInactive.Include,
                                FindObjectsSortMode.None
                            );

                        foreach (ObjectiveAndDayOverlay overlay in overlays)
                        {
                            if (overlay != null)
                            {
                                overlay.RefreshNow();
                            }
                        }
                    }

                    pendingReturnPlayDayScreen = false;
                    pendingReturnDayScreenDay = -1;
                }


                // =================================================
                // PLAYER STATE
                // =================================================

                if (PlayerStateManager.Instance != null)
                {
                    PlayerStateManager.Instance.ResetToDefault();

                    PlayerStateManager.Instance.SetState(
                        PlayerStateManager.PlayerState.AllMovement
                    );
                }


                // =================================================
                // HUD
                // =================================================

                if (GameFlowManager.Instance != null)
                {
                    GameFlowManager.Instance.SetGameplayHUDVisible(
                        true
                    );
                }


                // =================================================
                // CURSOR
                // =================================================

                Cursor.lockState =
                    CursorLockMode.Locked;

                Cursor.visible = false;


                // =================================================
                // HIDE LOADING SCREEN
                // =================================================

                Debug.Log(
                    "[Teleport] Hiding loading screen."
                );


                // Re-resolve right before use instead of trusting the
                // cached reference across the scene load -- if the
                // instance we grabbed before loading no longer exists,
                // this finds whichever instance is now the real survivor.
                EnsureLoadingScreen();

                if (loadingScreenManager != null)
                {
                    loadingScreenManager.Hide();
                }
                else
                {
                    Debug.LogWarning(
                        "[Teleport] LoadingScreenManager is NULL "
                        + "when trying to hide loading screen."
                    );
                }


                // =================================================
                // COMPLETE
                // =================================================

                Debug.Log(
                    "[Teleport] ========================================"
                );

                Debug.Log(
                    "[Teleport] CROSS-SCENE TELEPORT COMPLETE"
                );

                Debug.Log(
                    "[Teleport] Scene: "
                    + loadedScene.name
                );

                Debug.Log(
                    "[Teleport] Destination: "
                    + destinationID
                );

                Debug.Log(
                    "[Teleport] Player position: "
                    + teleportingPlayer.transform.position
                );

                Debug.Log(
                    "[Teleport] ========================================"
                );

                if (pendingDebugFinalBoss)
                {
                    FinishPendingDebugFinalBoss();
                }

                StartPendingDebugDay1Tutorial();

                // =================================================
                // RESET
                // =================================================

                ResetTeleportState();
            };


        // =========================================================
        // REGISTER CALLBACK
        // =========================================================

        SceneManager.sceneLoaded += onSceneLoaded;


        // =========================================================
        // LOAD TARGET SCENE
        // =========================================================

        Debug.Log(
            "[Teleport] Loading scene '"
            + sceneName
            + "' using Build Index "
            + buildIndex
            + "."
        );


        try
        {
            SceneManager.LoadScene(
                buildIndex,
                LoadSceneMode.Single
            );
        }
        catch (System.Exception exception)
        {
            SceneManager.sceneLoaded -= onSceneLoaded;

            Debug.LogException(
                exception
            );

            FailSceneTeleport();

            yield break;
        }


        // =========================================================
        // IMPORTANT
        // =========================================================
        //
        // Do NOT wait here.
        //
        // The sceneLoaded callback above now completes the
        // teleport when Unity finishes loading the scene.
        //
        // =========================================================

        yield break;
    }


    // =========================================================
    // MOVE PLAYER
    // =========================================================

    private void MovePlayerToDestination(
        GameObject player,
        TeleportDestination destination)
    {
        if (player == null)
        {
            Debug.LogError(
                "[Teleport] Cannot move player because "
                + "player is null."
            );

            return;
        }


        if (destination == null)
        {
            Debug.LogError(
                "[Teleport] Cannot move player because "
                + "destination is null."
            );

            return;
        }


        // -----------------------------------------------------
        // CHARACTER CONTROLLER
        // -----------------------------------------------------

        CharacterController controller =
            player.GetComponent<CharacterController>();


        if (controller != null)
        {
            controller.enabled = false;
        }


        // -----------------------------------------------------
        // POSITION / ROTATION
        // -----------------------------------------------------

        player.transform.SetPositionAndRotation(
            destination.GetSpawnPosition(),
            destination.GetSpawnRotation()
        );


        Physics.SyncTransforms();


        // -----------------------------------------------------
        // CHARACTER CONTROLLER BACK ON
        // -----------------------------------------------------

        if (controller != null)
        {
            controller.enabled = true;
        }


        // -----------------------------------------------------
        // RESET MOVEMENT
        // -----------------------------------------------------

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();


        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }


        // -----------------------------------------------------
        // RESET PLAYER STATE
        // -----------------------------------------------------

        PlayerStateManager state =
            player.GetComponent<PlayerStateManager>();


        if (state != null)
        {
            state.ResetToDefault();
        }
    }


    // =========================================================
    // FIND DESTINATION
    // =========================================================

    private TeleportDestination FindDestinationByID(
        string destinationID)
    {
        if (string.IsNullOrWhiteSpace(destinationID))
        {
            return null;
        }


        TeleportDestination[] destinations =
            FindObjectsByType<TeleportDestination>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (
            TeleportDestination destination
            in destinations)
        {
            if (destination == null)
            {
                continue;
            }


            if (
                destination.GetDestinationID()
                == destinationID
            )
            {
                return destination;
            }
        }


        return null;
    }


    // =========================================================
    // CASE STATE
    // =========================================================

    private void HandleCaseState(
        TeleportPortal portal)
    {
        if (portal == null)
        {
            return;
        }


        // =====================================================
        // ENTER CASE
        // =====================================================

        if (
            portal.GetTeleportType()
            == TeleportPortal.TeleportType.EnterCase
        )
        {
            // Cross-scene EnterCase starts CaseManager after the
            // destination scene finishes loading inside
            // LoadSceneAndFinishTeleport. Starting it here would bind
            // against the old scene and break level progression.
            if (portal.HasCrossSceneDestination())
            {
                return;
            }

            int caseID = ResolveEnterCaseID(
                portal,
                portal.GetDestinationSceneName(),
                portal.GetDestinationID()
            );


            if (caseID <= 0)
            {
                Debug.LogWarning(
                    "[Teleport] EnterCase portal has no Case ID."
                );

                return;
            }


            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.StartCase(
                    caseID
                );

                if (GameFlowManager.Instance != null)
                {
                    GameFlowManager.Instance.SetGameplayHUDVisible(true);
                }

                ObjectiveAndDayOverlay[] overlays =
                    FindObjectsByType<ObjectiveAndDayOverlay>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                foreach (ObjectiveAndDayOverlay overlay in overlays)
                {
                    if (overlay != null)
                    {
                        overlay.SetOverlayVisible(true);
                        overlay.RefreshNow();
                    }
                }
            }
            else
            {
                Debug.LogError(
                    "[Teleport] CaseManager is missing."
                );
            }


            return;
        }


        // =====================================================
        // RETURN FROM CASE
        // =====================================================

        if (
            portal.GetTeleportType()
            == TeleportPortal.TeleportType.ReturnFromCase
        )
        {
            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.DeactivateCase();
            }


            // A successful return is the new checkpoint.
            //
            // This is important when a player completes Case 1,
            // returns to the lobby, and later dies during Case 2:
            // Case 1 rewards must survive, while Case 2 pickups must
            // be discarded on death.
            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance
                    .CommitCurrentProgressAsDayCheckpoint();

                Debug.Log(
                    "[Teleport] Successful case return committed as "
                    + "the new day checkpoint."
                );
            }
            else
            {
                Debug.LogWarning(
                    "[Teleport] GameResetManager is missing. "
                    + "Case return checkpoint was not committed."
                );
            }
        }
    }


    // =========================================================
    // REBIND SCENE SYSTEMS
    // =========================================================

    private void RebindSceneSystems()
    {
        // -----------------------------------------------------
        // GAME FLOW
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance
                .RefreshSceneReferences();
        }


        // -----------------------------------------------------
        // CASE MANAGER
        // -----------------------------------------------------

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance
                .RefreshSceneReferences();
        }
    }


    // =========================================================
    // LOADING SCREEN
    // =========================================================

    private void EnsureLoadingScreen()
    {
        if (loadingScreenManager == null)
        {
            loadingScreenManager =
                LoadingScreenManager.Instance;
        }


        if (loadingScreenManager == null)
        {
            loadingScreenManager =
                FindAnyObjectByType<LoadingScreenManager>();
        }


        if (loadingScreenManager == null)
        {
            Debug.LogWarning(
                "[Teleport] LoadingScreenManager could not be found."
            );
        }
    }


    // =========================================================
    // RESTORE GAMEPLAY
    // =========================================================

    private void RestoreGameplayState()
    {
        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            if (
                pendingTeleportType
                == TeleportPortal.TeleportType.EnterCase
            )
            {
                GameStateManager.Instance.SetGameMode(
                    GameStateManager.GameMode.Case
                );
            }
            else
            {
                GameStateManager.Instance.SetGameMode(
                    GameStateManager.GameMode.Lobby
                );
            }
        }


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }


        // -----------------------------------------------------
        // HUD
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                true
            );
        }


        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }


    // =========================================================
    // FAILURE
    // =========================================================

    private void FailSceneTeleport()
    {
        Debug.LogError(
            "[Teleport] Cross-scene teleport failed."
        );


        EnsureLoadingScreen();

        if (loadingScreenManager != null)
        {
            loadingScreenManager.Hide();
        }


        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }


        ResetTeleportState();
    }


    // =========================================================
    // DAY
    // =========================================================

    private void ChangeDay(int day)
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetDay(day);
        }


        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.SetTheaterDay(
                day
            );
        }


        DebugTeleport(
            "Day changed to " + day
        );
    }


    // =========================================================
    // TELEPORT EFFECTS
    // =========================================================

    private void PlayTeleportEffect(
        TeleportPortal portal)
    {
        if (portal == null)
        {
            return;
        }


        if (portal.GetTeleportEffect() != null)
        {
            Instantiate(
                portal.GetTeleportEffect(),
                portal.transform.position,
                portal.transform.rotation
            );
        }


        if (portal.GetTeleportSound() != null)
        {
            AudioSource.PlayClipAtPoint(
                portal.GetTeleportSound(),
                portal.transform.position
            );
        }
    }


    // =========================================================
    // RESET TELEPORT STATE
    // =========================================================

    private void ResetTeleportState()
    {
        if (pendingPortal != null)
        {
            pendingPortal.ResetTeleport();
        }


        teleportingPlayer = null;

        pendingPortal = null;

        pendingSceneName = null;

        pendingDestinationID = null;

        pendingTeleportType =
            TeleportPortal.TeleportType.Normal;
    }


    // =========================================================
    // DEBUG LOG
    // =========================================================

    private void DebugTeleport(
        string message)
    {
        if (!debugTeleport)
        {
            return;
        }


        Debug.Log(
            "[Teleport] " + message
        );
    }


    // =========================================================
    // F KEYS
    // =========================================================

    private void Update()
    {
        HandleDebugFKeys();
    }


    private void HandleDebugFKeys()
    {
        // Debug teleports must never interrupt the first-day tutorial.
        // F12 is handled by TutorialManager and remains available to skip it.
        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.IsTutorialRunning)
            return;

        if (GameFlowManager.Instance != null &&
            !GameFlowManager.Instance.AreDebugHotkeysEnabled())
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }


        // =====================================================
        // F1 - LOBBY DAY 1
        // =====================================================

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f1LobbyDay1,
                f1Target,
                1,
                false
            );
        }


        // =====================================================
        // F2 - LEVEL 1
        // =====================================================

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f2Level1,
                f2Target,
                1,
                true
            );
        }


        // =====================================================
        // F3 - LOBBY DAY 2
        // =====================================================

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f3LobbyDay2,
                f3Target,
                2,
                false
            );
        }


        // =====================================================
        // F4 - LEVEL 2
        // =====================================================

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f4Level2,
                f4Target,
                2,
                true
            );
        }


        // =====================================================
        // F5 - LOBBY DAY 3
        // =====================================================

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f5LobbyDay3,
                f5Target,
                3,
                false
            );
        }


        // =====================================================
        // F6 - LEVEL 3
        // =====================================================

        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f6Level3,
                f6Target,
                3,
                true
            );
        }


        // =====================================================
        // F7 - LOBBY DAY 4
        // =====================================================

        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f7LobbyDay4,
                f7Target,
                4,
                false
            );
        }


        // =====================================================
        // F8 - TOGGLE PORTAL VISIBILITY
        // =====================================================

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            DebugTogglePortals();
        }


        // =====================================================
        // F10 - FINAL BOSS
        // =====================================================

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            DebugStartFinalBossTeleport();
        }
    }


    // =========================================================
    // F10 FINAL BOSS DEBUG TELEPORT
    // =========================================================

    private void DebugStartFinalBossTeleport()
    {
        SkipActiveCutscene();

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Debug Teleport] GameStateManager is missing. " +
                "F10 final boss could not be started."
            );
            return;
        }

        // Persist final-play completion BEFORE the scene transition so
        // GameFlowManager can correctly rebuild BossChanges when the
        // destination scene loads.
        GameStateManager.Instance.SetDay(4);
        GameStateManager.Instance.CompleteFinalPlay();

        pendingDebugFinalBoss = true;

        if (f10FinalBoss != null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                Debug.LogError(
                    "[Debug Teleport] Player not found for F10 final boss teleport."
                );
                pendingDebugFinalBoss = false;
                return;
            }

            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );

            MovePlayerToDestination(
                player,
                f10FinalBoss
            );

            RestoreGameplayState();
            FinishPendingDebugFinalBoss();
            return;
        }

        if (f10Target != null &&
            !string.IsNullOrWhiteSpace(f10Target.sceneName) &&
            !string.IsNullOrWhiteSpace(f10Target.destinationID))
        {
            DebugTeleportCrossScene(
                f10Target.sceneName,
                f10Target.destinationID,
                4,
                false
            );

            return;
        }

        Debug.LogWarning(
            "[Debug Teleport] F10 has no teleport destination configured. " +
            "Assign either F10 Final Boss or the F10 cross-scene target. " +
            "Starting the boss at the current location instead."
        );

        pendingDebugFinalBoss = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ApplyLobbyChangesToCurrentScene();
            GameFlowManager.Instance.StartBossFight();
        }
    }

    private void FinishPendingDebugFinalBoss()
    {
        if (!pendingDebugFinalBoss)
        {
            return;
        }

        pendingDebugFinalBoss = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ApplyLobbyChangesToCurrentScene();
            GameFlowManager.Instance.StartBossFight();
        }
        else if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.BossFight
            );
        }
    }


    // =========================================================
    // DEBUG TELEPORT OVERRIDE
    // =========================================================

    private void DebugTeleportOverride(
        TeleportDestination destination,
        DebugTarget crossSceneTarget,
        int day,
        bool level)
    {
        // Match the normal Day 1 transition: return control, then begin the
        // tutorial once F1 has placed the player in the lobby.
        pendingDebugDay1Tutorial = !level && day == 1;

        // =====================================================
        // STOP ANY ACTIVE CUTSCENE FIRST
        // =====================================================

        SkipActiveCutscene();


        // =====================================================
        // CROSS-SCENE DEBUG TARGET
        // =====================================================

        if (destination == null)
        {
            if (
                crossSceneTarget == null
                || string.IsNullOrWhiteSpace(
                    crossSceneTarget.sceneName
                )
                || string.IsNullOrWhiteSpace(
                    crossSceneTarget.destinationID
                )
            )
            {
                Debug.LogError(
                    "[Debug Teleport] No destination configured."
                );

                return;
            }


            DebugTeleportCrossScene(
                crossSceneTarget.sceneName,
                crossSceneTarget.destinationID,
                day,
                level
            );


            return;
        }


        // =====================================================
        // SAME-SCENE DEBUG TARGET
        // =====================================================

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (player == null)
        {
            Debug.LogError(
                "[Debug Teleport] Player not found."
            );

            return;
        }


        // -----------------------------------------------------
        // DAY
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetDay(
                day
            );


            GameStateManager.Instance.SetGameMode(
                level
                    ? GameStateManager.GameMode.Case
                    : GameStateManager.GameMode.Lobby
            );

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ApplyLobbyChangesToCurrentScene();
            }
        }


        // -----------------------------------------------------
        // START THE DEBUG CASE
        // -----------------------------------------------------
        // Normal level portals start the CaseManager as part of the
        // level-entry flow. Debug F2/F4/F6 must do the same thing;
        // otherwise the level music owned by CaseManager never starts.
        if (level && CaseManager.Instance != null)
        {
            CaseManager.Instance.RefreshSceneReferences();
            CaseManager.Instance.StartCase(day);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevelMusic(day);
            }
        }

        // -----------------------------------------------------
        // MOVE PLAYER
        // -----------------------------------------------------

        MovePlayerToDestination(
            player,
            destination
        );


        // -----------------------------------------------------
        // RESTORE GAMEPLAY
        // -----------------------------------------------------

        RestoreGameplayState();


        // -----------------------------------------------------
        // RETURN TO LOBBY
        // -----------------------------------------------------

        if (
            !level
            && CaseManager.Instance != null
        )
        {
            CaseManager.Instance.DeactivateCase();
        }

        if (pendingDebugFinalBoss)
        {
            FinishPendingDebugFinalBoss();
        }

        StartPendingDebugDay1Tutorial();
    }

    private void StartPendingDebugDay1Tutorial()
    {
        if (!pendingDebugDay1Tutorial)
            return;

        pendingDebugDay1Tutorial = false;
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.BeginDay1Tutorial();
        }
        else
        {
            Debug.LogError(
                "[Debug Teleport] F1 reached Day 1, but no TutorialManager exists."
            );
        }
    }


    // =========================================================
    // DEBUG CROSS-SCENE TELEPORT
    // =========================================================

    private void DebugTeleportCrossScene(
        string sceneName,
        string destinationID,
        int day,
        bool level)
    {
        // =====================================================
        // FIND PLAYER
        // =====================================================

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (player == null)
        {
            Debug.LogError(
                "[Debug Teleport] Player not found."
            );

            return;
        }


        // =====================================================
        // STOP ACTIVE CUTSCENE
        // =====================================================

        SkipActiveCutscene();


        // =====================================================
        // VALIDATE SCENE NAME
        // =====================================================

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "[Debug Teleport] Scene name is empty."
            );

            return;
        }


        // =====================================================
        // VALIDATE DESTINATION
        // =====================================================

        if (string.IsNullOrWhiteSpace(destinationID))
        {
            Debug.LogError(
                "[Debug Teleport] Destination ID is empty."
            );

            return;
        }


        // =====================================================
        // PREVENT DOUBLE TELEPORT
        // =====================================================

        if (teleportingPlayer != null)
        {
            Debug.LogWarning(
                "[Debug Teleport] A teleport is already in progress."
            );

            return;
        }


        // =====================================================
        // DAY / GAME STATE
        // =====================================================

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetDay(
                day
            );


            GameStateManager.Instance.SetGameMode(
                level
                    ? GameStateManager.GameMode.Case
                    : GameStateManager.GameMode.Lobby
            );

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ApplyLobbyChangesToCurrentScene();
            }
        }


        // =====================================================
        // KEEP PLAYER ALIVE
        // =====================================================

        DontDestroyOnLoad(
            player
        );

        teleportingPlayer = player;


        // =====================================================
        // CLEAR OLD TELEPORT DATA
        // =====================================================

        pendingPortal = null;


        // =====================================================
        // STORE TARGET
        // =====================================================

        pendingSceneName =
            sceneName;

        pendingDestinationID =
            destinationID;


        pendingTeleportType =
            level
                ? TeleportPortal.TeleportType.EnterCase
                : TeleportPortal.TeleportType.ReturnFromCase;


        // DEBUG level-entry teleports must use the same checkpoint
        // rules as normal level portals.

        if (
            pendingTeleportType ==
            TeleportPortal.TeleportType.EnterCase
        )
        {
            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance
                    .CreateCaseEntrySnapshot();
            }
            else
            {
                Debug.LogWarning(
                    "[Debug Teleport] GameResetManager is missing. " +
                    "No pre-case inventory checkpoint was captured."
                );
            }
        }


        // =====================================================
        // DISABLE PLAYER CONTROL
        // =====================================================

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.NoMovementNoCamera
            );
        }


        // =====================================================
        // SHOW LOADING SCREEN
        // =====================================================

        EnsureLoadingScreen();


        if (loadingScreenManager != null)
        {
            loadingScreenManager.Show();
        }
        else
        {
            Debug.LogWarning(
                "[Debug Teleport] LoadingScreenManager "
                + "could not be found."
            );
        }


        // =====================================================
        // DEBUG LOG
        // =====================================================

        DebugTeleport(
            "DEBUG: Loading scene '"
            + sceneName
            + "' for destination '"
            + destinationID
            + "'."
        );


        // =====================================================
        // START CROSS-SCENE LOAD
        // =====================================================

        StartCoroutine(
            LoadSceneAndFinishTeleport(
                sceneName,
                destinationID,
                pendingTeleportType,
                level ? day : 0
            )
        );
    }


    // =========================================================
    // STOP ACTIVE CUTSCENE
    // =========================================================

    private void SkipActiveCutscene()
    {
        // -----------------------------------------------------
        // DAY SCREEN
        // -----------------------------------------------------

        DayScreenCutscene dayScreen =
            DayScreenCutscene.Instance != null
                ? DayScreenCutscene.Instance
                : FindAnyObjectByType<DayScreenCutscene>(
                    FindObjectsInactive.Include
                );

        if (dayScreen != null && dayScreen.IsPlaying)
        {
            dayScreen.StopDayScreen();

            DebugTeleport(
                "Active day screen stopped."
            );
        }


        // -----------------------------------------------------
        // FIRST-GAME / INTRO CUTSCENE
        // -----------------------------------------------------

        bool skippedOpeningSequence = false;

        FirstGameCutscene firstGameCutscene =
            FindAnyObjectByType<FirstGameCutscene>();

        if (firstGameCutscene != null &&
            firstGameCutscene.IsSequenceRunning)
        {
            firstGameCutscene.StopSequence();
            skippedOpeningSequence = true;

            DebugTeleport(
                "Active first-game cutscene stopped."
            );
        }

        IntroCutscene introCutscene =
            FindAnyObjectByType<IntroCutscene>();

        if (introCutscene != null && introCutscene.IsPlaying)
        {
            introCutscene.StopCutscene();
            skippedOpeningSequence = true;

            DebugTeleport(
                "Active intro cutscene stopped."
            );
        }

        // Match F9: skipping the opening sequence must continue
        // into Day 1 (lobby music + Day 1 screen).
        if (skippedOpeningSequence &&
            GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.IntroFinished();
            return;
        }


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
        }


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }
    }


    // =========================================================
    // ENTER-CASE ID RESOLUTION
    // =========================================================

    /// <summary>
    /// Resolves which CaseManager case to start for an EnterCase
    /// teleport. Portal caseID fields in the lobby were historically
    /// off-by-one (0/1/2); portalLevel and destination names are 1/2/3.
    /// </summary>
    private static int ResolveEnterCaseID(
        TeleportPortal portal,
        string sceneName,
        string destinationID)
    {
        if (portal != null)
        {
            // portalLevel is the authoritative level index (1/2/3).
            // Some lobby portals still have stale caseID values that are
            // off-by-one, so prefer portalLevel whenever it is set.
            int portalLevel = portal.GetConfiguredPortalLevel();
            if (portalLevel > 0)
            {
                return portalLevel;
            }

            portalLevel = portal.GetPortalLevel();
            if (portalLevel > 0)
            {
                return portalLevel;
            }

            int configuredCaseID = portal.GetCaseID();
            if (configuredCaseID > 0)
            {
                return configuredCaseID;
            }
        }

        int fromDestination =
            InferCaseIDFromNames(sceneName, destinationID);

        if (fromDestination > 0)
        {
            return fromDestination;
        }

        return 0;
    }


    private static int InferCaseIDFromNames(
        string sceneName,
        string destinationID)
    {
        string combined =
            ((sceneName ?? string.Empty) + " " +
             (destinationID ?? string.Empty)).ToLowerInvariant();

        if (combined.Contains("level1") ||
            combined.Contains("level_1") ||
            combined.Contains("level 1"))
        {
            return 1;
        }

        if (combined.Contains("level2") ||
            combined.Contains("level_2") ||
            combined.Contains("level 2"))
        {
            return 2;
        }

        if (combined.Contains("level3") ||
            combined.Contains("level_3") ||
            combined.Contains("level 3"))
        {
            return 3;
        }

        return 0;
    }


    // =========================================================
    // DEBUG UNLOCK
    // =========================================================

    private void DebugTogglePortals()
    {
        Debug.Log(
            "[Debug Teleport] F8 pressed. Toggling portal visibility."
        );

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance
                .ToggleDebugPortals();
        }
        else
        {
            Debug.LogWarning(
                "[Debug Teleport] PortalProgressionManager is missing."
            );
        }
    }
}
