using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance { get; private set; }


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugTeleport = true;


    // =========================================================
    // DEBUG DESTINATIONS
    // =========================================================
    //
    // These are intentionally here so the F-key debug system
    // can be enabled later without modifying the teleport code.
    //
    // =========================================================

    [Header("Debug Destinations")]

    [SerializeField]
    private TeleportDestination case1StartDestination;

    [SerializeField]
    private TeleportDestination case2StartDestination;

    [SerializeField]
    private TeleportDestination case3StartDestination;

    [SerializeField]
    private TeleportDestination theaterCase1ReturnDestination;

    [SerializeField]
    private TeleportDestination theaterCase2ReturnDestination;

    [SerializeField]
    private TeleportDestination theaterCase3ReturnDestination;


    // =========================================================
    // CURRENT TELEPORT
    // =========================================================

    private GameObject teleportingPlayer;

    private TeleportPortal pendingPortal;


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
    }


    // =========================================================
    // TELEPORT
    // =========================================================

    public void Teleport(
        TeleportPortal portal,
        GameObject player)
    {
        if (portal == null)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (teleportingPlayer != null)
        {
            return;
        }

        teleportingPlayer = player;

        pendingPortal = portal;


        DebugTeleport(
            "Teleport requested."
        );


        // -----------------------------------------------------
        // EFFECT
        // -----------------------------------------------------

        PlayTeleportEffect(portal);


        // -----------------------------------------------------
        // DAY
        // -----------------------------------------------------

        if (portal.ChangesDay())
        {
            ChangeDay(
                portal.GetDestinationDay()
            );
        }


        // -----------------------------------------------------
        // CASE STATE
        // -----------------------------------------------------

        HandleCaseState(portal);


        // -----------------------------------------------------
        // TELEPORT
        // -----------------------------------------------------

        StartCoroutine(
            FinishTeleportAfterDelay(
                portal.GetTeleportDelay()
            )
        );
    }


    // =========================================================
    // FINISH TELEPORT
    // =========================================================

    private IEnumerator FinishTeleportAfterDelay(
        float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        yield return FinishTeleport();
    }


    private IEnumerator FinishTeleport()
    {
        yield return null;


        // -----------------------------------------------------
        // DESTINATION
        // -----------------------------------------------------

        if (pendingPortal == null)
        {
            ResetTeleportState();

            yield break;
        }


        TeleportDestination destination =
            pendingPortal.GetDestination();


        if (destination == null)
        {
            Debug.LogError(
                "[Teleport] Destination is missing."
            );

            ResetTeleportState();

            yield break;
        }


        // -----------------------------------------------------
        // CHARACTER CONTROLLER
        // -----------------------------------------------------

        CharacterController controller =
            teleportingPlayer.GetComponent<CharacterController>();


        if (controller != null)
        {
            controller.enabled = false;
        }


        // -----------------------------------------------------
        // MOVE PLAYER
        // -----------------------------------------------------

        teleportingPlayer.transform.position =
            destination.GetSpawnPosition();

        teleportingPlayer.transform.rotation =
            destination.GetSpawnRotation();


        // -----------------------------------------------------
        // RE-ENABLE CONTROLLER
        // -----------------------------------------------------

        if (controller != null)
        {
            controller.enabled = true;
        }


        DebugTeleport(
            "Player teleported to "
            + destination.gameObject.name
        );


        // -----------------------------------------------------
        // FUTURE EFFECTS / ANIMATION
        // -----------------------------------------------------
        //
        // This section is intentionally left available for:
        //
        // - Screen fade
        // - Fade to black
        // - Teleport animation
        // - Particles
        // - Camera effects
        // - Sound
        // - Loading effects
        // - Player animation
        //
        // -----------------------------------------------------


        ResetTeleportState();
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


        // -----------------------------------------------------
        // ENTER CASE
        // -----------------------------------------------------

        if (portal.GetTeleportType() ==
            TeleportPortal.TeleportType.EnterCase)
        {
            int caseID =
                portal.GetCaseID();


            if (caseID < 0)
            {
                Debug.LogWarning(
                    "[Teleport] Enter Case teleport "
                    + "has no valid Case ID."
                );

                return;
            }


            DebugTeleport(
                "Activating Case "
                + caseID
            );


            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.StartCase(
                    caseID
                );
            }
            else
            {
                Debug.LogError(
                    "[Teleport] CaseManager not found."
                );
            }


            return;
        }


        // -----------------------------------------------------
        // RETURN FROM CASE
        // -----------------------------------------------------

        if (portal.GetTeleportType() ==
            TeleportPortal.TeleportType.ReturnFromCase)
        {
            DebugTeleport(
                "Deactivating current case."
            );


            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.DeactivateCase();
            }

            if (GameResetManager.Instance != null)
            {
                GameResetManager.Instance
                    .CommitCurrentProgressAsDayCheckpoint();

                DebugTeleport(
                    "Case return checkpoint saved."
                );
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetGameMode(
                    GameStateManager.GameMode.Lobby
                );
            }
        }
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
            TheaterManager.Instance.SetTheaterDay(day);
        }


        DebugTeleport(
            "Day changed to Day "
            + day
        );
    }


    // =========================================================
    // EFFECTS
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
    // RESET
    // =========================================================

    private void ResetTeleportState()
    {
        if (pendingPortal != null)
        {
            pendingPortal.ResetTeleport();
        }


        teleportingPlayer = null;

        pendingPortal = null;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugTeleport(
        string message)
    {
        if (!debugTeleport)
        {
            return;
        }

        Debug.Log(
            "[Teleport] "
            + message
        );
    }


    private void Update()
    {
        HandleDebugFKeys();
    }

    private void HandleDebugFKeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f1LobbyDay1,
                1,
                false
            );
        }
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f2Level1,
                1,
                true
            );
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f3LobbyDay2,
                2,
                false
            );
        }
        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f4Level2,
                2,
                true
            );
        }

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f5LobbyDay3,
                3,
                false
            );
        }

        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f6Level3,
                3,
                true
            );
        }

        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            DebugTeleportOverride(
                f7LobbyDay4,
                4,
                false
            );
        }

        // F8 = unlock every level portal.
        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            DebugUnlockAllPortals();
        }
    }


    // =========================================================
    // FORCE DEBUG LOCATION
    // =========================================================

    private void DebugTeleportOverride(
        TeleportDestination destination,
        int day,
        bool level)
    {
        if (destination == null)
        {
            Debug.LogError(
                "[Debug Teleport] Destination is not assigned."
            );

            return;
        }


        GameObject player =
            GameObject.FindGameObjectWithTag("Player");


        if (player == null)
        {
            Debug.LogError(
                "[Debug Teleport] Player not found."
            );

            return;
        }


        // =====================================================
        // SKIP / CANCEL ANY ACTIVE CUTSCENE
        // =====================================================

        SkipActiveCutscene();


        // =====================================================
        // SET DAY
        // =====================================================

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetDay(day);
        }


        // =====================================================
        // SET GAME MODE
        // =====================================================

        if (GameStateManager.Instance != null)
        {
            if (level)
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


        // =====================================================
        // CHARACTER CONTROLLER
        // =====================================================

        CharacterController controller =
            player.GetComponent<CharacterController>();


        if (controller != null)
        {
            controller.enabled = false;
        }


        // =====================================================
        // TELEPORT
        // =====================================================

        player.transform.SetPositionAndRotation(
            destination.GetSpawnPosition(),
            destination.GetSpawnRotation()
        );


        // =====================================================
        // RE-ENABLE CHARACTER CONTROLLER
        // =====================================================

        if (controller != null)
        {
            controller.enabled = true;
        }


        // =====================================================
        // RESET MOVEMENT
        // =====================================================

        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();


        if (movement != null)
        {
            movement.ResetMovementAfterTeleport();
        }


        // =====================================================
        // RESET PLAYER STATE
        // =====================================================

        PlayerStateManager playerState =
            player.GetComponent<PlayerStateManager>();


        if (playerState != null)
        {
            playerState.ResetToDefault();
        }


        // =====================================================
        // RESTORE GAMEPLAY HUD / CURSOR
        // =====================================================
        // SkipCurrentCutscene() stops the video, but intentionally does
        // not decide what the gameplay HUD should look like. The old
        // debug path therefore left toolbar/health/stamina hidden.

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        // =====================================================
        // LOBBY
        // =====================================================

        if (!level)
        {
            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.DeactivateCase();
            }
        }


        // =====================================================
        // DEBUG MESSAGE
        // =====================================================

        if (level)
        {
            Debug.Log(
                "[Debug Teleport] Forced to Level "
                + day
                + "."
            );
        }
        else
        {
            Debug.Log(
                "[Debug Teleport] Forced to Lobby Day "
                + day
                + "."
            );
        }
    }

    // =========================================================
    // SKIP ACTIVE CUTSCENE
    // =========================================================

    private void SkipActiveCutscene()
    {
        Debug.Log(
            "[Debug Teleport] Skipping active cutscene."
        );


        // =====================================================
        // STOP ACTIVE CUTSCENES
        // =====================================================

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SkipCurrentCutscene();
        }


        // =====================================================
        // RESET PLAYER STATE
        // =====================================================

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");


        if (player != null)
        {
            PlayerStateManager playerState =
                player.GetComponent<PlayerStateManager>();


            if (playerState != null)
            {
                playerState.ResetToDefault();
            }
        }
    }


    // =========================================================
    // DEBUG - UNLOCK ALL PORTALS (F8)
    // =========================================================

    private void DebugUnlockAllPortals()
    {
        Debug.Log("[Debug Teleport] F8 pressed. Unlocking all portals.");

        // F8 should also safely break out of a cutscene if it is pressed
        // while a video is playing, just like the other debug keys.
        SkipActiveCutscene();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
        }

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            PlayerStateManager playerState =
                player.GetComponent<PlayerStateManager>();

            if (playerState != null)
            {
                playerState.ResetToDefault();
            }
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance.UnlockAllPortals();
        }
        else
        {
            Debug.LogError(
                "[Debug Teleport] PortalProgressionManager is missing. " +
                "F8 cannot unlock the portals."
            );
        }
    }


    private void DebugTeleportTo(
        TeleportDestination destination)
    {
        if (teleportingPlayer == null)
        {
            return;
        }


        if (destination == null)
        {
            Debug.LogWarning(
                "[Teleport Debug] Destination not assigned."
            );

            return;
        }


        CharacterController controller =
            teleportingPlayer.GetComponent<CharacterController>();


        if (controller != null)
        {
            controller.enabled = false;
        }


        teleportingPlayer.transform.position =
            destination.GetSpawnPosition();

        teleportingPlayer.transform.rotation =
            destination.GetSpawnRotation();


        if (controller != null)
        {
            controller.enabled = true;
        }


        DebugTeleport(
            "Debug teleported to "
            + destination.gameObject.name
        );
    }
}