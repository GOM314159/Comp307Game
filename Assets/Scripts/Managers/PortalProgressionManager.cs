using UnityEngine;

/// <summary>
/// Controls the visibility of the case-entry portals.
///
/// Flow:
/// - At startup every level portal is hidden.
/// - Completing the Day 1 play reveals Level 1.
/// - Entering Level 1 hides it.
/// - Completing the Day 2 play reveals Level 2.
/// - Entering Level 2 hides it.
/// - And so on.
/// 
/// F8 (handled by TeleportManager) can unlock every portal for debugging.
/// </summary>
public class PortalProgressionManager : MonoBehaviour
{
    public static PortalProgressionManager Instance { get; private set; }

    [Header("Level Portals")]
    [SerializeField] private GameObject level1Portal;
    [SerializeField] private GameObject level2Portal;
    [SerializeField] private GameObject level3Portal;
    [SerializeField] private GameObject level4Portal;

    [Header("Debug")]
    [SerializeField] private bool debugPortalProgression = true;

    private bool allPortalsUnlocked = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HideAllPortals();
    }

    /// <summary>
    /// Reveals the portal for the supplied day after that day's play
    /// has finished.
    /// </summary>
    public void RevealPortalForDay(int day)
    {
        if (allPortalsUnlocked)
        {
            return;
        }

        GameObject portal = GetPortalForDay(day);

        if (portal == null)
        {
            Debug.LogWarning(
                "[PortalProgression] No portal assigned for Day " + day + "."
            );
            return;
        }

        portal.SetActive(true);

        DebugPortal(
            "Level " + day + " portal revealed after the play."
        );
    }

    /// <summary>
    /// Called by TeleportPortal when the player actually enters a
    /// level portal.
    /// </summary>
    public void NotifyPortalUsed(TeleportPortal portal)
    {
        if (allPortalsUnlocked || portal == null)
        {
            return;
        }

        GameObject portalObject = portal.gameObject;

        if (portalObject == level1Portal ||
            portalObject == level2Portal ||
            portalObject == level3Portal ||
            portalObject == level4Portal)
        {
            portalObject.SetActive(false);

            DebugPortal(
                portalObject.name + " used. Portal hidden."
            );
        }
    }

    /// <summary>
    /// Makes every configured level portal visible.
    /// Intended for the F8 debug key.
    /// </summary>
    public void UnlockAllPortals()
    {
        allPortalsUnlocked = true;

        SetPortalActive(level1Portal, true);
        SetPortalActive(level2Portal, true);
        SetPortalActive(level3Portal, true);
        SetPortalActive(level4Portal, true);

        DebugPortal("DEBUG: All level portals unlocked.");
    }

    public void HideAllPortals()
    {
        if (allPortalsUnlocked)
        {
            return;
        }

        SetPortalActive(level1Portal, false);
        SetPortalActive(level2Portal, false);
        SetPortalActive(level3Portal, false);
        SetPortalActive(level4Portal, false);

        DebugPortal("All level portals hidden.");
    }

    private GameObject GetPortalForDay(int day)
    {
        switch (day)
        {
            case 1:
                return level1Portal;

            case 2:
                return level2Portal;

            case 3:
                return level3Portal;

            case 4:
                return level4Portal;

            default:
                return null;
        }
    }

    private void SetPortalActive(GameObject portal, bool active)
    {
        if (portal != null)
        {
            portal.SetActive(active);
        }
    }

    private void DebugPortal(string message)
    {
        if (!debugPortalProgression)
        {
            return;
        }

        Debug.Log("[PortalProgression] " + message);
    }
}
