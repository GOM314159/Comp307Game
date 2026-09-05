using UnityEngine;
using System.Collections;

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


    // =========================================================
    // DEBUG HOTKEYS
    // =========================================================
    //
    // These are intentionally commented out.
    //
    // F1 = Case 1 start
    // F2 = Case 2 start
    // F3 = Case 3 start
    // F4 = Case 1 theater return
    // F5 = Case 2 theater return
    // F6 = Case 3 theater return
    //
    // =========================================================

    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugTeleportTo(case1StartDestination);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugTeleportTo(case2StartDestination);
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugTeleportTo(case3StartDestination);
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            DebugTeleportTo(theaterCase1ReturnDestination);
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            DebugTeleportTo(theaterCase2ReturnDestination);
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            DebugTeleportTo(theaterCase3ReturnDestination);
        }
        */
    }


    // =========================================================
    // DEBUG TELEPORT
    // =========================================================

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