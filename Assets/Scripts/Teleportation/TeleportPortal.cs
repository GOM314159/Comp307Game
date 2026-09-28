using UnityEngine;
using System.Collections;
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

    [Tooltip("Used for same-scene teleporting. Leave empty for cross-scene teleporting.")]
    [SerializeField]
    private TeleportDestination destination;

    [Tooltip("Scene to load. Leave empty to preserve the original same-scene behaviour.")]
    [SerializeField]
    private string destinationSceneName;

    [Tooltip("Destination ID inside the destination scene. Must match TeleportDestination.destinationID.")]
    [SerializeField]
    private string destinationID;


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

    [Header("Portal Progression")]

    [Tooltip("0 = not a level-entry portal. 1/2/3/4 = the corresponding level portal.")]
    [Min(0)]
    [SerializeField]
    private int portalLevel = 0;


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
    // PORTAL VISIBILITY
    // =========================================================

    private void Awake()
    {
        if (GetConfiguredPortalLevel() > 0 || IsLikelyLevelPortalByName())
        {
            if (PortalProgressionManager.Instance == null ||
                !PortalProgressionManager.Instance.ShouldPortalBeVisible(
                    GetEffectivePortalLevelForVisibility()))
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        if (PortalProgressionManager.Instance == null)
            return;

        int level = GetEffectivePortalLevelForVisibility();

        if (level > 0 &&
            !PortalProgressionManager.Instance.ShouldPortalBeVisible(level))
        {
            StartCoroutine(DisableIfStillLocked(level));
        }
    }

    private IEnumerator DisableIfStillLocked(int level)
    {
        yield return null;

        if (this == null || !gameObject.activeInHierarchy)
            yield break;

        if (PortalProgressionManager.Instance == null ||
            !PortalProgressionManager.Instance.ShouldPortalBeVisible(level))
        {
            gameObject.SetActive(false);
        }
    }

    private bool IsLikelyLevelPortalByName()
    {
        string n = gameObject.name.ToLowerInvariant();

        return n.Contains("level1portal") ||
               n.Contains("level2portal") ||
               n.Contains("level3portal") ||
               n.Contains("level4portal");
    }

    private int GetEffectivePortalLevelForVisibility()
    {
        if (PortalProgressionManager.Instance != null)
        {
            return PortalProgressionManager.Instance
                .GetEffectivePortalLevel(this);
        }

        return GetConfiguredPortalLevel();
    }

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

        if (!HasValidDestination())
        {
            Debug.LogError(
                "[Teleport] No valid destination configured on "
                + gameObject.name
                + ". Assign a same-scene destination OR a destination scene name + destination ID."
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

        if (
    teleportType ==
    TeleportType.ReturnFromCase
)
        {
            if (ControllerLightManager.Instance != null)
            {
                ControllerLightManager.Instance
                    .OnReturnPortalEntered();
            }
        }


        // =====================================================
        // LEVEL PORTAL PROGRESSION
        // =====================================================
        // The entry portal is a one-use portal for the current level.
        // Hide it as soon as the player enters it.

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance.NotifyPortalUsed(this);
        }


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

    public string GetDestinationSceneName()
    {
        return destinationSceneName;
    }

    public string GetDestinationID()
    {
        return destinationID;
    }

    public bool HasCrossSceneDestination()
    {
        return !string.IsNullOrWhiteSpace(destinationSceneName)
            && !string.IsNullOrWhiteSpace(destinationID);
    }

    public bool HasValidDestination()
    {
        return destination != null || HasCrossSceneDestination();
    }


    public TeleportType GetTeleportType()
    {
        return teleportType;
    }


    public int GetCaseID()
    {
        return caseID;
    }

    public int GetConfiguredPortalLevel()
    {
        return portalLevel;
    }

    public int GetPortalLevel()
    {
        if (portalLevel > 0)
            return portalLevel;

        if (PortalProgressionManager.Instance != null)
        {
            return PortalProgressionManager.Instance
                .GetEffectivePortalLevel(this);
        }

        return 0;
    }

    /// <summary>
    /// Called only by PortalProgressionManager.
    /// </summary>
    public void SetProgressionVisibility(bool visible)
    {
        if (visible)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }
        else
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
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
