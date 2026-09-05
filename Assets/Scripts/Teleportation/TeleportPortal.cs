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

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (destination == null)
        {
            Debug.LogError(
                "[Teleport] No destination assigned to "
                + gameObject.name
            );

            return;
        }

        if (TeleportManager.Instance == null)
        {
            Debug.LogError(
                "[Teleport] TeleportManager not found."
            );

            return;
        }

        teleporting = true;

        TeleportManager.Instance.Teleport(
            this,
            other.gameObject
        );
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