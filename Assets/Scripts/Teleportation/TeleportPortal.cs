using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    // =========================================================
    // TELEPORT TYPE
    // =========================================================

    public enum TeleportType
    {
        Normal,
        EnterCase,
        ReturnFromCase
    }


    // =========================================================
    // DESTINATION
    // =========================================================

    [Header("Destination")]

    [Tooltip("Drag the TeleportDestination object from the Hierarchy here.")]
    [SerializeField]
    private TeleportDestination destination;


    // =========================================================
    // TELEPORT TYPE
    // =========================================================

    [Header("Teleport Type")]

    [SerializeField]
    private TeleportType teleportType =
        TeleportType.Normal;


    // =========================================================
    // CASE SETTINGS
    // =========================================================

    [Header("Case Settings")]

    [Tooltip("Case ID activated when entering a case.")]
    [SerializeField]
    private int caseID = -1;


    // =========================================================
    // DAY PROGRESSION
    // =========================================================

    [Header("Day Progression")]

    [Tooltip("Should this teleport change the current day?")]
    [SerializeField]
    private bool changesDay = false;

    [Tooltip("Day the player will be on after using this teleport.")]
    [Min(1)]
    [SerializeField]
    private int destinationDay = 1;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [Tooltip("Tag used to identify the player.")]
    [SerializeField]
    private string playerTag = "Player";


    // =========================================================
    // RETURN PORTAL HEALTH
    // =========================================================

    [Header("Return Portal Health")]

    [Tooltip(
        "If enabled, entering this portal will restore the " +
        "player to maximum health in Normal Mode. " +
        "Hardcore Mode will never restore health."
    )]
    [SerializeField]
    private bool restoreHealthOnNormalReturn = true;


    [Tooltip("Enable debug messages for return portal health.")]
    [SerializeField]
    private bool debugHealthRestore = true;


    // =========================================================
    // EFFECTS / ANIMATION
    // =========================================================

    [Header("Effects / Animation")]

    [Tooltip("Optional delay before the teleport occurs.")]
    [Min(0f)]
    [SerializeField]
    private float teleportDelay = 0f;

    [Tooltip("Optional effect spawned when teleporting.")]
    [SerializeField]
    private GameObject teleportEffect;

    [Tooltip("Optional sound played when teleporting.")]
    [SerializeField]
    private AudioClip teleportSound;


    // =========================================================
    // STATE
    // =========================================================

    private bool teleporting = false;


    // =========================================================
    // COLLISION
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (teleporting)
        {
            return;
        }


        // -----------------------------------------------------
        // PLAYER CHECK
        // -----------------------------------------------------

        if (!other.CompareTag(playerTag))
        {
            return;
        }


        // -----------------------------------------------------
        // DESTINATION CHECK
        // -----------------------------------------------------

        if (destination == null)
        {
            Debug.LogError(
                "[Teleport] No destination assigned to "
                + gameObject.name
            );

            return;
        }


        // -----------------------------------------------------
        // TELEPORT MANAGER CHECK
        // -----------------------------------------------------

        if (TeleportManager.Instance == null)
        {
            Debug.LogError(
                "[Teleport] TeleportManager not found."
            );

            return;
        }


        // =====================================================
        // RETURN PORTAL HEALTH
        // =====================================================

        HandleReturnPortalHealth(
            other.gameObject
        );


        // =====================================================
        // START TELEPORT
        // =====================================================

        teleporting = true;

        TeleportManager.Instance.Teleport(
            this,
            other.gameObject
        );
    }


    // =========================================================
    // RETURN PORTAL HEALTH
    // =========================================================

    private void HandleReturnPortalHealth(
        GameObject playerObject
    )
    {
        // -----------------------------------------------------
        // ONLY RETURN PORTALS HEAL
        // -----------------------------------------------------

        if (
            teleportType !=
            TeleportType.ReturnFromCase
        )
        {
            return;
        }


        // -----------------------------------------------------
        // OPTIONAL SETTING
        // -----------------------------------------------------

        if (!restoreHealthOnNormalReturn)
        {
            return;
        }


        // -----------------------------------------------------
        // GAME STATE MANAGER
        // -----------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning(
                "[Teleport] GameStateManager.Instance " +
                "is missing. Cannot determine difficulty."
            );

            return;
        }


        // =====================================================
        // HARDCORE
        // =====================================================
        //
        // Hardcore intentionally does NOTHING.
        //
        // The player's current health is preserved.
        //
        // =====================================================

        if (GameStateManager.Instance.IsHardcore())
        {
            if (debugHealthRestore)
            {
                Debug.Log(
                    "[Teleport] ReturnFromCase entered in " +
                    "HARDCORE mode. Player health NOT restored."
                );
            }

            return;
        }


        // =====================================================
        // NORMAL
        // =====================================================

        if (GameStateManager.Instance.IsNormal())
        {
            PlayerHealth health =
                playerObject.GetComponent<PlayerHealth>();


            if (health == null)
            {
                Debug.LogWarning(
                    "[Teleport] PlayerHealth component " +
                    "could not be found on the player."
                );

                return;
            }


            health.ResetHealth();


            if (debugHealthRestore)
            {
                Debug.Log(
                    "[Teleport] ReturnFromCase entered in " +
                    "NORMAL mode. Player health restored " +
                    "to maximum."
                );
            }
        }
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public TeleportDestination GetDestination()
    {
        return destination;
    }


    public TeleportType GetTeleportType()
    {
        return teleportType;
    }


    public int GetCaseID()
    {
        return caseID;
    }


    public bool ChangesDay()
    {
        return changesDay;
    }


    public int GetDestinationDay()
    {
        return destinationDay;
    }


    public float GetTeleportDelay()
    {
        return teleportDelay;
    }


    public GameObject GetTeleportEffect()
    {
        return teleportEffect;
    }


    public AudioClip GetTeleportSound()
    {
        return teleportSound;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetTeleport()
    {
        teleporting = false;
    }
}
