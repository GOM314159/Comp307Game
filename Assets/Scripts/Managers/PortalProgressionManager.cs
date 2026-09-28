using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls which level-entry portal is visible.
/// Normal progression is driven ONLY by the theater play finishing.
/// Day 1 -> Level 1, Day 2 -> Level 2, Day 3 -> Level 3, Day 4 -> Level 4.
/// Entering that portal hides it immediately.
/// F8 toggles a debug override: first press shows all level portals;
/// second press returns to normal theater-driven progression.
/// </summary>
public class PortalProgressionManager : MonoBehaviour
{
    public static PortalProgressionManager Instance { get; private set; }

    [Header("Optional Explicit Portal References")]
    [Tooltip("Optional. These identify level portals even if their TeleportPortal Portal Level is 0.")]
    [SerializeField] private GameObject level1Portal;
    [SerializeField] private GameObject level2Portal;
    [SerializeField] private GameObject level3Portal;
    [SerializeField] private GameObject level4Portal;

    [Header("Debug")]
    [SerializeField] private bool debugPortalProgression = true;

    // -1 = no theater play has revealed a portal.
    // 1..4 = the portal revealed by the most recently completed theater play.
    private int revealedDay = -1;
    private bool revealedPortalUsed = false;

    // F8 debug override. It does not modify theater progression.
    private bool debugAllPortalsVisible = false;
    private bool initialized = false;

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
    }

    private void Start()
    {
        // Do NOT reset revealedDay here. A theater play can finish during
        // startup before this persistent manager's Start callback runs.
        // Resetting here would erase the freshly revealed portal.
        initialized = true;
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
        if (!initialized)
            return;

        StartCoroutine(RefreshAfterSceneLoad());
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;
        RefreshCurrentScenePortals();
    }

    /// <summary>
    /// Used by TeleportPortal to decide whether a level-entry portal may be active.
    /// </summary>
    public bool ShouldPortalBeVisible(int level)
    {
        if (level <= 0)
            return true;

        if (!initialized)
            return false;

        if (debugAllPortalsVisible)
            return true;

        return level == revealedDay && !revealedPortalUsed;
    }

    /// <summary>
    /// Gets the level for a portal. Explicit inspector value wins. If it is 0,
    /// the manager falls back to the optional references and standard names such
    /// as Level1Portal, Level2Portal, etc. This prevents an unassigned Portal Level
    /// from causing all of the level portals to behave like ordinary portals.
    /// </summary>
    public int GetEffectivePortalLevel(TeleportPortal portal)
    {
        if (portal == null)
            return 0;

        int configuredLevel = portal.GetConfiguredPortalLevel();
        if (configuredLevel > 0)
            return configuredLevel;

        GameObject obj = portal.gameObject;

        if (level1Portal == obj || IsNamedPortal(obj, 1)) return 1;
        if (level2Portal == obj || IsNamedPortal(obj, 2)) return 2;
        if (level3Portal == obj || IsNamedPortal(obj, 3)) return 3;
        if (level4Portal == obj || IsNamedPortal(obj, 4)) return 4;

        return 0;
    }

    private bool IsNamedPortal(GameObject obj, int level)
    {
        if (obj == null)
            return false;

        string n = obj.name.ToLowerInvariant();
        string number = level.ToString();

        return n == "level" + number + "portal" ||
               n.Contains("level" + number + "portal");
    }

    // =========================================================
    // THEATER PROGRESSION
    // =========================================================

    /// <summary>
    /// Called by ChairPlayController only after that day's theater video finishes.
    /// Only this day's portal becomes visible.
    /// </summary>
    public void RevealPortalForDay(int day)
    {
        if (day < 1 || day > 4)
            return;

        if (debugAllPortalsVisible)
        {
            DebugPortal("Day " + day + " theater finished while F8 debug visibility is active.");
            return;
        }

        revealedDay = day;
        revealedPortalUsed = false;

        RefreshCurrentScenePortals();
        StartCoroutine(RefreshAfterReveal());

        DebugPortal("Day " + day + " theater finished -> ONLY Level " + day + " portal revealed.");
    }

    /// <summary>
    /// Called immediately when a level-entry portal is entered.
    /// </summary>
    public void NotifyPortalUsed(TeleportPortal portal)
    {
        if (portal == null)
            return;

        int level = GetEffectivePortalLevel(portal);
        if (level <= 0)
            return;

        if (debugAllPortalsVisible)
        {
            // Using an F8-forced portal returns the system to the normal state.
            debugAllPortalsVisible = false;
            revealedDay = -1;
            revealedPortalUsed = false;
            RefreshCurrentScenePortals();

            DebugPortal("F8-forced portal used -> debug override disabled and level portals hidden.");
            return;
        }

        if (level == revealedDay)
        {
            revealedPortalUsed = true;
            RefreshCurrentScenePortals();

            DebugPortal("Level " + level + " portal used -> portal hidden.");
        }
    }

    private IEnumerator RefreshAfterReveal()
    {
        // Allow any portal OnEnable/scene callbacks to finish before applying
        // the final progression state one more time.
        yield return null;
        RefreshCurrentScenePortals();
    }

    // =========================================================
    // F8 DEBUG
    // =========================================================

    public void ToggleDebugPortals()
    {
        debugAllPortalsVisible = !debugAllPortalsVisible;
        RefreshCurrentScenePortals();

        DebugPortal(
            debugAllPortalsVisible
                ? "F8 ON: ALL level portals forced visible."
                : "F8 OFF: returning to theater-driven portal progression."
        );
    }

    // Compatibility for any existing code that calls this method.
    public void UnlockAllPortals()
    {
        debugAllPortalsVisible = true;
        RefreshCurrentScenePortals();
        DebugPortal("DEBUG: all level portals forced visible.");
    }

    public void HideAllPortals()
    {
        debugAllPortalsVisible = false;
        revealedDay = -1;
        revealedPortalUsed = false;
        RefreshCurrentScenePortals();
        DebugPortal("DEBUG: all level portals hidden.");
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetForDay(int day)
    {
        if (day < 1)
            return;

        if (revealedDay == day)
        {
            revealedDay = -1;
            revealedPortalUsed = false;
        }

        debugAllPortalsVisible = false;
        RefreshCurrentScenePortals();

        DebugPortal("Portal progression reset for Day " + day + ".");
    }

    public void ResetForGame()
    {
        revealedDay = -1;
        revealedPortalUsed = false;
        debugAllPortalsVisible = false;
        RefreshCurrentScenePortals();
        DebugPortal("Portal progression completely reset.");
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void RefreshPortals()
    {
        RefreshCurrentScenePortals();
    }

    private void RefreshCurrentScenePortals()
    {
        TeleportPortal[] portals = FindObjectsByType<TeleportPortal>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TeleportPortal portal in portals)
        {
            if (portal == null)
                continue;

            int level = GetEffectivePortalLevel(portal);

            // Level 0 is an ordinary/return portal and is not controlled here.
            if (level <= 0)
                continue;

            portal.SetProgressionVisibility(ShouldPortalBeVisible(level));
        }
    }

    private void DebugPortal(string message)
    {
        if (!debugPortalProgression)
            return;

        Debug.Log("[PortalProgression] " + message);
    }
}
