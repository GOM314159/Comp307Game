using UnityEngine;

/// <summary>
/// Attach this to the "Managers" GameObject in GamePlayTest -- the
/// parent that holds TeleportManager, CaseManager, GameStateManager,
/// LoadingScreenManager, and every other manager as children.
///
/// DontDestroyOnLoad only works on a GameObject with no parent.
/// Every individual manager script was already calling
/// DontDestroyOnLoad(gameObject) on itself in its own Awake(), but
/// since they are all children of "Managers", Unity silently refused
/// every single one of those calls -- none of them were actually
/// persisting. Marking the PARENT with DontDestroyOnLoad persists it
/// and everything underneath it in one call.
///
/// This script's Awake() should run before you rely on any child
/// manager's Instance elsewhere, but Unity does not guarantee parent
/// Awake() runs before children's Awake() by default. That's fine
/// here -- DontDestroyOnLoad just needs to be called at some point
/// during this GameObject's first frame, and doesn't depend on the
/// children having done anything yet.
/// </summary>
public class ManagersRoot : MonoBehaviour
{
    private static ManagersRoot instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}