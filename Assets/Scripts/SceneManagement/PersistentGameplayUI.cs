using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Keeps shared gameplay UI alive while gameplay scenes change.
///
/// This object should exist once in GameTestScene.
/// Do NOT duplicate it into Level1/Level2/Level3.
///
/// It provides shared UI references to scene-local interactables.
/// </summary>
public class PersistentGameplayUI : MonoBehaviour
{
    public static PersistentGameplayUI Instance { get; private set; }


    // =========================================================
    // SHARED UI
    // =========================================================

    [Header("Shared Gameplay UI")]

    [Tooltip(
        "The TextMeshProUGUI object used by all WorldInteractables " +
        "to display the interaction prompt."
    )]
    [SerializeField]
    private TextMeshProUGUI interactionText;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // SINGLETON
        // -----------------------------------------------------

        if (Instance != null &&
            Instance != this)
        {
            // A duplicate scene can contain the only live copy of the
            // objective overlay. Move that subtree under the surviving UI
            // root before removing the duplicate root, or day transitions
            // can destroy the HUD along with the duplicate Canvas.
            ObjectiveAndDayOverlay duplicateOverlay =
                GetComponentInChildren<ObjectiveAndDayOverlay>(true);
            if (duplicateOverlay != null &&
                Instance.GetComponentInChildren<ObjectiveAndDayOverlay>(true) == null)
            {
                Canvas persistentCanvas = Instance.GetComponentInChildren<Canvas>(true);
                Transform destination = persistentCanvas != null
                    ? persistentCanvas.transform
                    : Instance.transform;
                duplicateOverlay.transform.SetParent(destination, false);
                Debug.Log(
                    "[PersistentGameplayUI] Adopted ObjectiveAndDayOverlay " +
                    "from a duplicate UI root before removing it."
                );
            }

            // Preserve shared HUD labels before destroying the duplicate root.
            // Otherwise the surviving persistent root can be left without a
            // DayText/ObjectiveText, making the overlay blank in later scenes.
            Canvas textDestination = Instance.GetComponentInChildren<Canvas>(true);
            Transform textParent = textDestination != null
                ? textDestination.transform
                : Instance.transform;
            TMP_Text[] duplicateTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text duplicateText in duplicateTexts)
            {
                if (duplicateText == null ||
                    (duplicateText.gameObject.name != "DayText" &&
                     duplicateText.gameObject.name != "ObjectiveText"))
                {
                    continue;
                }

                if (Instance.FindText(duplicateText.gameObject.name) != null)
                    continue;

                duplicateText.transform.SetParent(textParent, false);
                Debug.Log(
                    "[PersistentGameplayUI] Adopted " +
                    duplicateText.gameObject.name +
                    " from a duplicate UI root before removing it."
                );
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;


        // -----------------------------------------------------
        // PERSIST ACROSS SCENES
        // -----------------------------------------------------

        // This object is currently authored beneath the scene's Cutscenes
        // root. DontDestroyOnLoad only persists root GameObjects, so detach
        // the UI subtree first; otherwise the overlay is destroyed on the
        // first Single scene load and cached references become stale.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;

        EnsurePersistentUICamera();


        // -----------------------------------------------------
        // VALIDATE
        // -----------------------------------------------------

        if (interactionText == null)
        {
            Debug.LogWarning(
                "[PersistentGameplayUI] Interaction Text " +
                "has not been assigned."
            );
        }
    }


    // =========================================================
    // INTERACTION TEXT
    // =========================================================

    public TextMeshProUGUI GetInteractionText()
    {
        return interactionText;
    }

    /// <summary>Finds a named TMP label anywhere beneath the persistent UI root.</summary>
    public TMP_Text FindText(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return null;
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.gameObject.name == objectName)
                return text;
        }
        return null;
    }

    public ObjectiveAndDayOverlay GetObjectiveOverlay()
    {
        return GetComponentInChildren<ObjectiveAndDayOverlay>(true);
    }

    /// <summary>
    /// After every scene load make sure the objective/day overlay (and the
    /// parents it lives under) is still active. The overlay is what keeps the
    /// Day tracker alive in the lobby and in every level.
    /// </summary>
    private void EnsurePersistentUICamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            camera = FindAnyObjectByType<Camera>();

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            // The persistent gameplay Canvas is authored with a zero local
            // scale in GamePlayTest. Its descendants remain active and TMP
            // reports enabled, but Unity cannot render any of that UI.
            // Screen-space root canvases should retain a normal transform;
            // CanvasScaler handles their display scaling.
            if (canvas.isRootCanvas &&
                canvas.renderMode != RenderMode.WorldSpace &&
                (Mathf.Abs(canvas.transform.localScale.x) < 0.0001f ||
                 Mathf.Abs(canvas.transform.localScale.y) < 0.0001f ||
                 Mathf.Abs(canvas.transform.localScale.z) < 0.0001f))
            {
                Vector3 previousScale = canvas.transform.localScale;
                canvas.transform.localScale = Vector3.one;
                Debug.LogWarning(
                    "[PersistentGameplayUI] Restored screen-space root Canvas " +
                    canvas.name + " scale from " + previousScale + " to (1, 1, 1). " +
                    "A zero scale prevented DayText and ObjectiveText from rendering.",
                    canvas
                );
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (canvas.worldCamera != camera)
                    canvas.worldCamera = camera;
            }

            if (!canvas.enabled)
                canvas.enabled = true;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ObjectiveAndDayOverlay overlay = GetObjectiveOverlay();

        // Some levels contain the overlay in the scene instead of under the
        // persistent root. Adopt that overlay before the scene-local objects
        // are lost, so DayText has one surviving owner across every scene.
        if (overlay == null)
        {
            ObjectiveAndDayOverlay[] sceneOverlays =
                FindObjectsByType<ObjectiveAndDayOverlay>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < sceneOverlays.Length; i++)
            {
                ObjectiveAndDayOverlay candidate = sceneOverlays[i];
                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;
                if (candidate.gameObject.scene != scene)
                    continue;

                Canvas destinationCanvas = GetComponentInChildren<Canvas>(true);
                Transform destination = destinationCanvas != null
                    ? destinationCanvas.transform
                    : transform;
                candidate.transform.SetParent(destination, false);
                overlay = candidate;
                break;
            }
        }

        if (overlay == null) return;

        Transform current = overlay.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }

        EnsurePersistentUICamera();

        if (overlay != null)
        {
            overlay.SetOverlayVisible(true);
            overlay.RefreshNow();
        }

        // Start the level's case as early as possible so its objective overlay
        // is relevant immediately on entry.
        CaseSceneBinding binding =
            FindAnyObjectByType<CaseSceneBinding>(
                FindObjectsInactive.Include
            );
        if (binding != null &&
            CaseManager.Instance != null &&
            GameStateManager.Instance != null &&
            !GameStateManager.Instance.IsCaseActive())
        {
            CaseManager.Instance.StartCase(binding.GetCaseID());

            ObjectiveAndDayOverlay[] overlays =
                FindObjectsByType<ObjectiveAndDayOverlay>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
            foreach (ObjectiveAndDayOverlay sceneOverlay in overlays)
            {
                if (sceneOverlay != null)
                {
                    sceneOverlay.SetOverlayVisible(true);
                    sceneOverlay.RefreshNow();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }
}
