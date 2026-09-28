using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Persistent Alt+F4 exit cutscene controller.
///
/// IMPORTANT:
/// - This object persists across scene changes.
/// - The exit cutscene is only intercepted when Alt+F4 was actually
///   detected by the Input System.
/// - Normal in-game Quit buttons call AllowNormalQuit() and bypass
///   the cutscene.
/// </summary>
public class AltF4ExitCutscene : MonoBehaviour
{
    public static AltF4ExitCutscene Instance { get; private set; }

    [Header("Exit Cutscene Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Settings")]
    [SerializeField] private bool enableAltF4ExitCutscene = true;
    [SerializeField] private bool debugExitCutscene = true;

    [Header("Detection")]
    [Tooltip("How long the Alt+F4 detection flag remains valid for the wantsToQuit callback.")]
    [SerializeField] private float altF4DetectionWindow = 0.5f;

    private bool allowNormalQuit;
    private bool exitSequenceRunning;
    private bool applicationIsQuitting;
    private float altF4DetectedUntil = -1f;

    private Canvas exitCanvas;
    private RawImage exitVideoImage;
    private RenderTexture exitRenderTexture;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("[AltF4ExitCutscene] No VideoPlayer assigned.");
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.prepareCompleted -= OnVideoPrepared;

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnEnable()
    {
        Application.wantsToQuit += HandleApplicationWantsToQuit;
    }

    private void OnDisable()
    {
        Application.wantsToQuit -= HandleApplicationWantsToQuit;
    }

    private void Update()
    {
        if (exitSequenceRunning || applicationIsQuitting)
            return;

        if (!enableAltF4ExitCutscene)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.leftAltKey.wasPressedThisFrame ||
            keyboard.rightAltKey.wasPressedThisFrame)
        {
            // Do not start the cutscene here. The OS quit request is
            // what must be intercepted. We only remember that Alt+F4
            // was pressed so wantsToQuit can distinguish it from a
            // normal Application.Quit().
            if (keyboard.f4Key.isPressed)
            {
                MarkAltF4Detected();
            }
        }

        // Some keyboards report F4 first and Alt in the same frame.
        if (keyboard.f4Key.wasPressedThisFrame &&
            (keyboard.leftAltKey.isPressed ||
             keyboard.rightAltKey.isPressed))
        {
            MarkAltF4Detected();
        }
    }

    private void MarkAltF4Detected()
    {
        altF4DetectedUntil =
            Time.unscaledTime + Mathf.Max(0.1f, altF4DetectionWindow);

        DebugExit("Alt + F4 detected.");
    }

    public void AllowNormalQuit()
    {
        allowNormalQuit = true;
        applicationIsQuitting = true;
        Application.Quit();
    }

    private bool HandleApplicationWantsToQuit()
    {
        if (applicationIsQuitting)
            return true;

        if (allowNormalQuit)
            return true;

        // This is the important distinction: normal Application.Quit()
        // calls are allowed. Only a recently detected Alt+F4 is blocked.
        bool altF4WasPressed =
            Time.unscaledTime <= altF4DetectedUntil;

        if (!enableAltF4ExitCutscene || !altF4WasPressed)
        {
            DebugExit("Normal quit request allowed.");
            return true;
        }

        if (exitSequenceRunning)
        {
            DebugExit("Additional quit request ignored.");
            return false;
        }

        exitSequenceRunning = true;
        StartExitCutscene();
        return false;
    }

    private void StartExitCutscene()
    {
        Time.timeScale = 1f;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene);
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        HideExistingUI();
        CreateFullscreenVideoUI();

        videoPlayer.Stop();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = exitRenderTexture;

        // Re-prepare every time so the same persistent VideoPlayer can
        // play the clip after any number of scene changes.
        videoPlayer.Prepare();
    }

    private void CreateFullscreenVideoUI()
    {
        DestroyExitUIOnly();

        GameObject canvasObject =
            new GameObject("AltF4ExitCutsceneCanvas");

        canvasObject.transform.SetParent(transform, false);

        exitCanvas = canvasObject.AddComponent<Canvas>();
        exitCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        exitCanvas.sortingOrder = 32767;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject =
            new GameObject("ExitVideo");

        imageObject.transform.SetParent(
            canvasObject.transform, false);

        exitVideoImage = imageObject.AddComponent<RawImage>();

        RectTransform rect = exitVideoImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        exitRenderTexture = new RenderTexture(
            Mathf.Max(1, Screen.width),
            Mathf.Max(1, Screen.height),
            0,
            RenderTextureFormat.ARGB32);

        exitRenderTexture.name = "AltF4ExitVideoRenderTexture";
        exitRenderTexture.Create();

        exitVideoImage.texture = exitRenderTexture;
    }

    private void HideExistingUI()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }

        Canvas[] canvases =
            FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas == exitCanvas)
                continue;

            canvas.enabled = false;
        }
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (!exitSequenceRunning)
            return;

        DebugExit("Exit video prepared. Playing.");
        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        DebugExit("Exit video finished. Quitting.");
        QuitApplication();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError(
            "[AltF4ExitCutscene] Video error: " + message);

        // Never leave the application trapped because a video failed.
        QuitApplication();
    }

    private void QuitApplication()
    {
        if (applicationIsQuitting)
            return;

        applicationIsQuitting = true;
        Application.Quit();
    }

    private void DestroyExitUIOnly()
    {
        if (exitCanvas != null)
        {
            Destroy(exitCanvas.gameObject);
            exitCanvas = null;
            exitVideoImage = null;
        }

        if (exitRenderTexture != null)
        {
            exitRenderTexture.Release();
            Destroy(exitRenderTexture);
            exitRenderTexture = null;
        }
    }

    private void DebugExit(string message)
    {
        if (debugExitCutscene)
            Debug.Log("[AltF4ExitCutscene] " + message);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Application.wantsToQuit -= HandleApplicationWantsToQuit;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        DestroyExitUIOnly();
    }
}
