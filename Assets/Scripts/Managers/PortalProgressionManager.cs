using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalProgressionManager : MonoBehaviour
{
    public static PortalProgressionManager Instance { get; private set; }

    [Header("Legacy / Optional References")]

    [SerializeField]
    private GameObject level1Portal;

    [SerializeField]
    private GameObject level2Portal;

    [SerializeField]
    private GameObject level3Portal;

    [SerializeField]
    private GameObject level4Portal;

    [Header("Debug")]

    [SerializeField]
    private bool debugPortalProgression = true;

    private readonly HashSet<int> revealedLevels =
        new HashSet<int>();

    private readonly HashSet<int> usedLevels =
        new HashSet<int>();

    private bool allPortalsUnlocked = false;

    private bool initialized = false;

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
    }

    private void Start()
    {
        initialized = true;

        RefreshCurrentScenePortals();
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
        if (!initialized)
        {
            return;
        }

        StartCoroutine(
            RefreshAfterSceneLoad()
        );
    }

    private System.Collections.IEnumerator
        RefreshAfterSceneLoad()
    {
        yield return null;

        RefreshCurrentScenePortals();
    }

    // =========================================================
    // REVEAL
    // =========================================================

    public void RevealPortalForDay(int day)
    {
        if (day < 1)
        {
            return;
        }

        if (allPortalsUnlocked)
        {
            return;
        }

        revealedLevels.Add(day);

        usedLevels.Remove(day);

        RefreshCurrentScenePortals();

        DebugPortal(
            "Level "
            + day
            + " portal revealed."
        );
    }

    // =========================================================
    // USED
    // =========================================================

    public void NotifyPortalUsed(
        TeleportPortal portal)
    {
        if (portal == null)
        {
            return;
        }

        if (allPortalsUnlocked)
        {
            return;
        }

        int level =
            portal.GetPortalLevel();

        if (level <= 0)
        {
            return;
        }

        usedLevels.Add(level);

        RefreshCurrentScenePortals();

        DebugPortal(
            "Level "
            + level
            + " portal used and hidden."
        );
    }

    // =========================================================
    // F8
    // =========================================================

    public void UnlockAllPortals()
    {
        allPortalsUnlocked = true;

        RefreshCurrentScenePortals();

        DebugPortal(
            "DEBUG: All level portals unlocked."
        );
    }

    // =========================================================
    // HIDE ALL
    // =========================================================

    public void HideAllPortals()
    {
        if (allPortalsUnlocked)
        {
            return;
        }

        revealedLevels.Clear();

        usedLevels.Clear();

        RefreshCurrentScenePortals();

        DebugPortal(
            "All level portals hidden."
        );
    }

    // =========================================================
    // RESET CURRENT DAY
    // =========================================================

    public void ResetForDay(int day)
    {
        if (day < 1)
        {
            return;
        }

        revealedLevels.Remove(day);

        usedLevels.Remove(day);

        RefreshCurrentScenePortals();

        DebugPortal(
            "Portal progression reset for Day "
            + day
        );
    }

    // =========================================================
    // RESET ENTIRE GAME
    // =========================================================

    public void ResetForGame()
    {
        revealedLevels.Clear();

        usedLevels.Clear();

        allPortalsUnlocked = false;

        RefreshCurrentScenePortals();

        DebugPortal(
            "Portal progression completely reset."
        );
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshCurrentScenePortals()
    {
        TeleportPortal[] portals =
            FindObjectsByType<TeleportPortal>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (TeleportPortal portal in portals)
        {
            if (portal == null)
            {
                continue;
            }

            int level =
                portal.GetPortalLevel();

            if (level <= 0)
            {
                continue;
            }

            bool visible =
                allPortalsUnlocked ||
                (
                    revealedLevels.Contains(level) &&
                    !usedLevels.Contains(level)
                );

            portal.gameObject.SetActive(visible);
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugPortal(
        string message)
    {
        if (!debugPortalProgression)
        {
            return;
        }

        Debug.Log(
            "[PortalProgression] "
            + message
        );
    }
}