using UnityEngine;

/// <summary>
/// Persistent loading-screen controller used by scene transitions.
///
/// IMPORTANT SETUP NOTE:
/// This script now controls ONE thing: the GameObject that the
/// assigned CanvasGroup lives on ("LoadingGroup"). That GameObject
/// gets SetActive(true/false) directly, which guarantees everything
/// under it -- including any Image/background component sitting
/// directly on LoadingGroup itself -- is fully hidden, independent
/// of whatever the CanvasGroup's alpha happens to be.
///
/// Previously this script had a separate "Loading Root" field
/// pointing at a CHILD object ("Loading"), while the CanvasGroup
/// (and the actual red background Image) lived on the PARENT
/// ("LoadingGroup"). SetActive(false) on the child never touched
/// the parent's own Image, so hiding relied entirely on alpha
/// reaching 0 -- with no fallback if that ever failed.
///
/// In the Inspector: only "Canvas Group" needs to be assigned
/// (to LoadingGroup's CanvasGroup component). The old "Loading Root"
/// field is gone -- Unity will just drop that old serialized
/// reference silently, no action needed there.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("Loading Screen")]
    [Tooltip(
        "The CanvasGroup on the panel that contains EVERYTHING that " +
        "should show/hide together (background image, spinner, text, " +
        "etc). Its GameObject is what actually gets SetActive'd."
    )]
    [SerializeField]
    private CanvasGroup canvasGroup;

    private GameObject PanelRoot =>
        canvasGroup != null ? canvasGroup.gameObject : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup == null)
        {
            Debug.LogWarning(
                "[LoadingScreenManager] Canvas Group is not assigned. " +
                "The loading screen cannot be shown or hidden."
            );
        }

        HideImmediate();
    }

    public void Show()
    {
        if (PanelRoot != null)
        {
            PanelRoot.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // SetActive(false) is the real guarantee here -- it hides
        // the whole panel (including any Image on this same object)
        // regardless of what alpha ends up being.
        if (PanelRoot != null)
        {
            PanelRoot.SetActive(false);
        }
    }

    private void HideImmediate()
    {
        Hide();
    }
}