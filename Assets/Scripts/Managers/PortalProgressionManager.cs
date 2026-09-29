using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent progression authority for the level-entry portals.
///
/// IMPORTANT:
/// This manager intentionally contains NO serialized references to
/// scene-local portal GameObjects. Portals are discovered from the
/// currently loaded scene by their TeleportPortal component.
///
/// This prevents Unity from losing scene-object drag/drop references
/// when the persistent manager survives a scene change.
/// </summary>
public class PortalProgressionManager : MonoBehaviour
{
    public static PortalProgressionManager Instance { get; private set; }

    [Header("Progression")]
    [Tooltip(
        "The level portal currently unlocked by a completed theater play. " +
        "-1 means no level portal is unlocked."
    )]
    [SerializeField]
    private int revealedDay = -1;

    private int GetAuthoritativeRevealedDay()
    {
        if (GameStateManager.Instance != null)
        {
            int storedDay = GameStateManager.Instance.GetRevealedPortalDay();

            if (storedDay >= -1)
            {
                revealedDay = storedDay;
            }
        }

        return revealedDay;
    }

    private void StoreRevealedDay(int day)
    {
        revealedDay = day;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetRevealedPortalDay(day);
        }
    }

    [Header("Debug")]
    [SerializeField]
    private bool debugPortalProgression = true;

    [SerializeField]
    private bool debugAllPortalsVisible = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SyncProgressionFromGameState();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        SyncProgressionFromGameState();
        RefreshCurrentScenePortals();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncProgressionFromGameState();
        StartCoroutine(RefreshAfterSceneLoad());
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        SyncProgressionFromGameState();

        // Let the newly loaded scene finish Awake/OnEnable first.
        yield return null;
        RefreshCurrentScenePortals();

        // One additional frame protects against portals that are enabled
        // by another scene-local initialization script.
        yield return null;
        RefreshCurrentScenePortals();
    }

    // =========================================================
    // VISIBILITY
    // =========================================================

    public bool ShouldPortalBeVisible(int level)
    {
        if (level <= 0)
        {
            return true;
        }

        if (debugAllPortalsVisible)
        {
            return true;
        }

        return level == GetAuthoritativeRevealedDay();
    }

    public int GetRevealedDay()
    {
        return revealedDay;
    }

    // =========================================================
    // PORTAL IDENTIFICATION
    // =========================================================

    public int GetEffectivePortalLevel(TeleportPortal portal)
    {
        if (portal == null)
        {
            return 0;
        }

        int configuredLevel =
            portal.GetConfiguredPortalLevel();

        if (configuredLevel > 0)
        {
            return configuredLevel;
        }

        GameObject obj = portal.gameObject;

        if (IsNamedPortal(obj, 1))
        {
            return 1;
        }

        if (IsNamedPortal(obj, 2))
        {
            return 2;
        }

        if (IsNamedPortal(obj, 3))
        {
            return 3;
        }

        if (IsNamedPortal(obj, 4))
        {
            return 4;
        }

        return 0;
    }

    private bool IsNamedPortal(GameObject obj, int level)
    {
        if (obj == null)
        {
            return false;
        }

        string n =
            obj.name.ToLowerInvariant();

        string number =
            level.ToString();

        return n == "level" + number + "portal" ||
               n.Contains("level" + number + "portal");
    }

    // =========================================================
    // PLAY PROGRESSION
    // =========================================================

    public void RevealPortalForDay(int day)
    {
        if (day < 1)
        {
            Debug.LogWarning(
                "[PortalProgression] Cannot reveal an invalid day: "
                + day
            );

            return;
        }

        StoreRevealedDay(day);
        debugAllPortalsVisible = false;

        DebugPortal(
            "Day " + day +
            " play completed. Level " + day +
            " portal is now unlocked and will remain open until the day is completed."
        );

        RefreshCurrentScenePortals();
        StartCoroutine(RefreshAfterReveal());
    }

    private IEnumerator RefreshAfterReveal()
    {
        yield return null;
        RefreshCurrentScenePortals();

        yield return null;
        RefreshCurrentScenePortals();
    }

    public void NotifyPortalUsed(TeleportPortal portal)
    {
        if (portal == null)
        {
            return;
        }

        int level =
            GetEffectivePortalLevel(portal);

        if (level <= 0)
        {
            return;
        }

        if (level == revealedDay)
        {
            DebugPortal(
                "Level " + level +
                " portal used. It remains unlocked until the day is completed."
            );
        }
    }

    // =========================================================
    // DAY / GAME RESET
    // =========================================================

    /// <summary>
    /// Called when a day is restarted because of death.
    /// The currently unlocked portal MUST remain unlocked.
    /// </summary>
    public void ResetForDay(int day)
    {
        debugAllPortalsVisible = false;

        RefreshCurrentScenePortals();

        DebugPortal(
            "Day " + day +
            " restarted. Keeping revealed portal state at Level " +
            revealedDay + "."
        );
    }

    /// <summary>
    /// Called only when a day has genuinely been completed.
    /// The current portal closes and the next portal remains locked
    /// until its theater play is watched.
    /// </summary>
    public void CompleteDay(int day)
    {
        if (GetAuthoritativeRevealedDay() == day)
        {
            StoreRevealedDay(-1);
        }

        debugAllPortalsVisible = false;

        RefreshCurrentScenePortals();

        DebugPortal(
            "Day " + day +
            " completed. Its portal is now closed."
        );
    }

    /// <summary>
    /// Called for a brand-new game/main-menu reset.
    /// </summary>
    public void ResetForGame()
    {
        StoreRevealedDay(-1);
        debugAllPortalsVisible = false;

        RefreshCurrentScenePortals();

        DebugPortal(
            "Portal progression completely reset. All level portals are locked."
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    public void ToggleDebugPortals()
    {
        debugAllPortalsVisible =
            !debugAllPortalsVisible;

        RefreshCurrentScenePortals();

        DebugPortal(
            debugAllPortalsVisible
                ? "F8 ON: all level portals forced visible."
                : "F8 OFF: normal portal progression restored."
        );
    }

    public void UnlockAllPortals()
    {
        debugAllPortalsVisible = true;
        RefreshCurrentScenePortals();
    }

    public void HideAllPortals()
    {
        debugAllPortalsVisible = false;
        StoreRevealedDay(-1);
        RefreshCurrentScenePortals();
    }

    public void RefreshPortals()
    {
        RefreshCurrentScenePortals();
    }

    private void SyncProgressionFromGameState()
    {
        if (GameStateManager.Instance != null)
        {
            revealedDay =
                GameStateManager.Instance.GetRevealedPortalDay();
        }
    }

    // =========================================================
    // SCENE REFRESH
    // =========================================================

    private void RefreshCurrentScenePortals()
    {
        TeleportPortal[] portals =
            FindObjectsByType<TeleportPortal>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int visibleCount = 0;
        int hiddenCount = 0;

        foreach (TeleportPortal portal in portals)
        {
            if (portal == null)
            {
                continue;
            }

            int level =
                GetEffectivePortalLevel(portal);

            if (level <= 0)
            {
                continue;
            }

            bool visible =
                ShouldPortalBeVisible(level);

            portal.SetProgressionVisibility(visible);

            if (visible)
            {
                visibleCount++;
            }
            else
            {
                hiddenCount++;
            }
        }

        DebugPortal(
            "Scene '" +
            SceneManager.GetActiveScene().name +
            "' portal refresh: " +
            visibleCount +
            " visible, " +
            hiddenCount +
            " hidden, revealedDay=" +
            revealedDay +
            "."
        );
    }

    private void DebugPortal(string message)
    {
        if (!debugPortalProgression)
        {
            return;
        }

        Debug.Log(
            "[PortalProgression] " +
            message
        );
    }
}
