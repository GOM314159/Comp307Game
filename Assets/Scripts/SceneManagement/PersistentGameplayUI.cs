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

        overlay.RefreshNow();
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
