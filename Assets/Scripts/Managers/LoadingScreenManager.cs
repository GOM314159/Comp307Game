using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Persistent loading-screen controller.
///
/// The assigned LoadingGroup is moved under a dedicated persistent
/// Screen Space - Overlay Canvas so the actual Image remains available
/// after the scene changes. This is important when the LoadingGroup was
/// originally a child of a scene Canvas.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("Loading Screen")]
    [Tooltip("CanvasGroup on your LoadingGroup. The Image/Sprite can be on this object or a child.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Optional: assign the Image that should display the loading artwork. Leave empty if the Image is on LoadingGroup and you want automatic discovery.")]
    [SerializeField] private Image loadingImage;

    [Tooltip("Optional tint. White displays the sprite exactly as imported.")]
    [SerializeField] private Color imageTint = Color.white;

    [SerializeField] private int sortingOrder = 5000;

    private GameObject persistentCanvasObject;
    private Canvas persistentCanvas;
    private readonly Dictionary<Canvas, bool> canvasStatesBeforeLoading =
        new Dictionary<Canvas, bool>();
    private bool gameplayCanvasesHidden;

    /// <summary>True while the loading screen has the gameplay canvases hidden.</summary>
    public bool AreGameplayCanvasesHidden => gameplayCanvasesHidden;

    private GameObject PanelRoot => canvasGroup != null ? canvasGroup.gameObject : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // This controller is authored under the scene's Cutscenes root.
        // Detach it before persisting so it survives Single scene loads.
        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);

        PreparePersistentLoadingCanvas();
        HideImmediate();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void PreparePersistentLoadingCanvas()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("[LoadingScreenManager] CanvasGroup is not assigned. Assign LoadingGroup's CanvasGroup.");
            return;
        }

        if (loadingImage == null)
        {
            loadingImage = canvasGroup.GetComponent<Image>();

            if (loadingImage == null)
                loadingImage = canvasGroup.GetComponentInChildren<Image>(true);
        }

        // If LoadingGroup is already under a persistent Canvas, do nothing.
        if (canvasGroup.transform.root.GetComponent<Canvas>() != null &&
            canvasGroup.transform.root.gameObject.scene.name == "DontDestroyOnLoad")
        {
            ApplyImageSetup();
            return;
        }

        persistentCanvasObject = new GameObject("Persistent Loading Canvas");
        persistentCanvas = persistentCanvasObject.AddComponent<Canvas>();
        persistentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        persistentCanvas.sortingOrder = sortingOrder;
        persistentCanvasObject.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(persistentCanvasObject);

        // Preserve the LoadingGroup itself, including its Image and CanvasGroup.
        canvasGroup.transform.SetParent(persistentCanvasObject.transform, false);

        RectTransform rect = canvasGroup.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        ApplyImageSetup();
    }

    private void ApplyImageSetup()
    {
        if (loadingImage == null)
            return;

        // Do not animate/tint the sprite. The loading artwork should be visible
        // exactly as assigned; CanvasGroup controls the show/hide state.
        Color c = imageTint;
        c.a = 1f;
        loadingImage.color = c;
        loadingImage.raycastTarget = false;
        loadingImage.preserveAspect = false;
    }

    public void Show()
    {
        if (PanelRoot == null)
            return;

        PanelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (loadingImage != null)
        {
            Color c = imageTint;
            c.a = 1f;
            loadingImage.color = c;
        }

        HideGameplayCanvases();
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (PanelRoot != null)
            PanelRoot.SetActive(false);

        RestoreGameplayCanvases();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (gameplayCanvasesHidden)
            HideGameplayCanvases();
    }

    private void HideGameplayCanvases()
    {
        if (!gameplayCanvasesHidden)
        {
            gameplayCanvasesHidden = true;
            canvasStatesBeforeLoading.Clear();
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || IsLoadingCanvas(canvas))
                continue;

            if (!canvasStatesBeforeLoading.ContainsKey(canvas))
                canvasStatesBeforeLoading.Add(canvas, canvas.enabled);

            canvas.enabled = false;
        }
    }

    private bool IsLoadingCanvas(Canvas canvas)
    {
        if (canvas == persistentCanvas)
            return true;

        return canvasGroup != null &&
            (canvas.transform == canvasGroup.transform ||
             canvas.transform.IsChildOf(canvasGroup.transform));
    }

    private void RestoreGameplayCanvases()
    {
        if (!gameplayCanvasesHidden)
            return;

        foreach (KeyValuePair<Canvas, bool> state in canvasStatesBeforeLoading)
        {
            if (state.Key != null)
                state.Key.enabled = state.Value;
        }

        canvasStatesBeforeLoading.Clear();
        gameplayCanvasesHidden = false;
    }

    private void HideImmediate()
    {
        Hide();
    }
}
