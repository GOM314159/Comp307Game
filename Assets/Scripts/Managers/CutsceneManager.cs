using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using System;
using System.Collections;

// =============================================================
// CUTSCENE MANAGER
// =============================================================
//
// Full-screen video playback for Strings & Statutes.
//
// Integrates with the existing systems rather than duplicating
// them:
//
//   GameStateManager   -> switches to GameMode.Cutscene and
//                         restores the previous mode after.
//
//   PlayerStateManager -> switches to PlayerState.Cutscene, which
//                         PlayerLook / PlayerMovement already
//                         respect via CanLook() / CanMove().
//
//   AudioManager       -> remembers the current music/ambience,
//                         stops them, and restores them after.
//
// Renders to a RenderTexture on a screen-space Canvas, so it is
// unaffected by the URP render pipeline setup.
//
// =============================================================

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }


    // =========================================================
    // SKIP
    // =========================================================

    [Header("Skip")]

    [SerializeField] private bool allowSkip = true;

    // Seconds at the start where skip input is ignored, so a
    // player mashing Interact does not blow past the cutscene.
    [SerializeField] [Min(0f)] private float skipLockoutSeconds = 0.75f;

    // 0 = a single press skips. Above 0 = the key must be held.
    [SerializeField] [Min(0f)] private float holdToSkipSeconds = 0.6f;

    // Optional. PlayerControls has no Skip action yet, so this is
    // left empty and Escape / Start is used instead. Add a Skip
    // action to PlayerControls.inputactions and drag it here to
    // make it rebindable.
    [SerializeField] private InputActionReference skipAction;


    // =========================================================
    // FADES
    // =========================================================

    [Header("Fades")]

    [SerializeField] [Min(0f)] private float fadeInSeconds = 0.35f;
    [SerializeField] [Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField] private Color fadeColor = Color.black;


    // =========================================================
    // GAME INTEGRATION
    // =========================================================

    [Header("Game Integration")]

    // Sets GameMode.Cutscene for the duration and restores the
    // previous mode afterwards.
    [SerializeField] private bool driveGameStateManager = true;

    // Sets PlayerState.Cutscene for the duration and restores the
    // previous state afterwards.
    [SerializeField] private bool drivePlayerStateManager = true;

    // Stops music and ambience during the cutscene, then restores
    // whatever was playing before it started.
    [SerializeField] private bool silenceAudioManager = true;

    // Time.timeScale is set to 0 so puppets and timers do not run
    // behind the video. VideoPlayer has its own clock, so playback
    // is unaffected.
    [SerializeField] private bool pauseGameTime = true;

    // Hides the first-person arms. They are still rendered while
    // frozen otherwise, floating in shot over the video.
    [SerializeField] private GameObject[] hideDuringCutscene;


    // =========================================================
    // ROBUSTNESS
    // =========================================================

    [Header("Robustness")]

    // Give up if the video has not finished preparing in this many
    // seconds. Without it, a missing or corrupt file leaves the
    // player on a black screen with no way out.
    [SerializeField] [Min(1f)] private float prepareTimeoutSeconds = 10f;

    [SerializeField] private bool interruptOnNewRequest = false;


    // =========================================================
    // OPTIONAL WIRING
    // =========================================================

    [Header("Optional Wiring")]

    // Leave all of these empty and the overlay is built at runtime.
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RawImage videoSurface;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private GameObject skipPrompt;
    [SerializeField] private Image skipProgressFill;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool debugCutscene = true;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnCutsceneStarted;

    // True when the cutscene was skipped rather than watched.
    public event Action<bool> OnCutsceneFinished;


    // =========================================================
    // RUNTIME STATE
    // =========================================================

    private VideoPlayer videoPlayer;
    private AudioSource videoAudio;
    private RenderTexture renderTexture;
    private Image fadeImage;
    private AspectRatioFitter aspectFitter;
    private GameObject backdrop;

    private bool isPlaying;
    private bool videoReachedEnd;
    private bool errored;
    private string lastError;
    private bool skipRequested;
    private Coroutine playRoutine;

    private bool stateOverridden;
    private float cachedTimeScale = 1f;
    private CursorLockMode cachedLockMode;
    private bool cachedCursorVisible;
    private GameStateManager.GameMode cachedGameMode;
    private PlayerStateManager cachedPlayerState;
    private PlayerStateManager.PlayerState cachedPlayerStateValue;
    private AudioManager.MusicType cachedMusic;
    private AudioManager.AmbienceType cachedAmbience;

    // Override to supply your own skip input.
    public Func<bool> SkipHeldOverride { get; set; }

    public static bool IsCutscenePlaying
    {
        get { return Instance != null && Instance.isPlaying; }
    }


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

        BuildOverlay();

        BuildVideoPlayer();

        SetOverlayVisible(false);
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        ReleaseRenderTexture();
    }


    // =========================================================
    // PUBLIC API
    // =========================================================

    public void Play(VideoClip clip, Action<bool> onFinished = null)
    {
        if (clip == null)
        {
            Debug.LogError(
                "[Cutscene] Play called with a null VideoClip.");

            if (onFinished != null)
            {
                onFinished(false);
            }

            return;
        }

        BeginPlayback(clip, null, onFinished);
    }


    // Plays from StreamingAssets or any URL, keeping large video
    // files out of the asset database.
    public void Play(string url, Action<bool> onFinished = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError(
                "[Cutscene] Play called with an empty URL.");

            if (onFinished != null)
            {
                onFinished(false);
            }

            return;
        }

        BeginPlayback(null, url, onFinished);
    }


    // Convenience for the intro flow. GameFlowManager.StartIntro()
    // has already set GameMode.Cutscene by the time this runs.
    public void PlayIntro(VideoClip clip)
    {
        Play(clip, delegate
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.IntroFinished();
            }
        });
    }


    // Convenience for the ending. Routes into the existing
    // CompleteFinalCutscene() -> OpenCredits() flow.
    public void PlayFinalCutscene(VideoClip clip)
    {
        Play(clip, delegate
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.FinalCutsceneFinished();
            }
        });
    }


    public void StopCurrent()
    {
        if (!isPlaying)
        {
            return;
        }

        skipRequested = true;
    }


    // =========================================================
    // PLAYBACK
    // =========================================================

    private void BeginPlayback(
        VideoClip clip,
        string url,
        Action<bool> onFinished)
    {
        if (isPlaying)
        {
            if (!interruptOnNewRequest)
            {
                DebugCutscene(
                    "Already playing, request ignored.");

                if (onFinished != null)
                {
                    onFinished(false);
                }

                return;
            }

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            RestoreGameplayState();
        }

        playRoutine = StartCoroutine(
            PlayRoutine(clip, url, onFinished));
    }


    private IEnumerator PlayRoutine(
        VideoClip clip,
        string url,
        Action<bool> onFinished)
    {
        isPlaying = true;
        videoReachedEnd = false;
        skipRequested = false;
        errored = false;
        lastError = null;

        CacheAndOverrideGameplayState();

        if (OnCutsceneStarted != null)
        {
            OnCutsceneStarted();
        }

        if (clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;

            DebugCutscene("Preparing clip: " + clip.name);
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;

            DebugCutscene("Preparing url: " + url);
        }

        // Prepare fully before anything is shown. Playing without
        // this gives a black first frame and a hitch.
        videoPlayer.Prepare();

        float startedAt = Time.realtimeSinceStartup;

        while (!videoPlayer.isPrepared && !errored)
        {
            if (Time.realtimeSinceStartup - startedAt
                > prepareTimeoutSeconds)
            {
                Debug.LogError(
                    "[Cutscene] Timed out after "
                    + prepareTimeoutSeconds
                    + "s preparing the video. Aborting.");

                errored = true;
            }

            yield return null;
        }

        if (errored)
        {
            // Bail out before the overlay is ever shown, so the
            // player simply carries on instead of being stranded.
            Debug.LogError(
                "[Cutscene] Aborted. " + lastError);

            FinishUp(onFinished, false);

            yield break;
        }

        AllocateRenderTexture(
            (int)videoPlayer.width,
            (int)videoPlayer.height);

        videoPlayer.targetTexture = renderTexture;
        videoSurface.texture = renderTexture;

        if (aspectFitter != null && videoPlayer.height > 0)
        {
            aspectFitter.aspectRatio =
                (float)videoPlayer.width / videoPlayer.height;
        }

        if (skipPrompt != null)
        {
            skipPrompt.SetActive(false);
        }

        if (skipProgressFill != null)
        {
            skipProgressFill.fillAmount = 0f;
        }

        EnableSkipAction(true);

        // Fade through black properly: the overlay comes up fully
        // transparent, gameplay fades out under it, and only then
        // does the video appear. Showing an opaque overlay first
        // would hard cut to black.
        SetVideoContentVisible(false);

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
        }

        SetOverlayVisible(true);

        yield return Fade(0f, 1f, fadeInSeconds);

        SetVideoContentVisible(true);

        videoPlayer.Play();

        yield return Fade(1f, 0f, fadeInSeconds);

        float elapsed = 0f;
        float held = 0f;
        bool skipped = false;
        bool promptShown = false;

        while (!videoReachedEnd && !errored)
        {
            float dt = Time.unscaledDeltaTime;

            elapsed += dt;

            if (allowSkip && elapsed >= skipLockoutSeconds)
            {
                if (!promptShown && skipPrompt != null)
                {
                    skipPrompt.SetActive(true);

                    promptShown = true;
                }

                if (IsSkipHeld())
                {
                    held += dt;

                    if (skipProgressFill != null
                        && holdToSkipSeconds > 0f)
                    {
                        skipProgressFill.fillAmount =
                            Mathf.Clamp01(held / holdToSkipSeconds);
                    }

                    if (held >= holdToSkipSeconds)
                    {
                        skipped = true;

                        break;
                    }
                }
                else
                {
                    held = 0f;

                    if (skipProgressFill != null)
                    {
                        skipProgressFill.fillAmount = 0f;
                    }
                }
            }

            if (skipRequested)
            {
                skipped = true;

                break;
            }

            yield return null;
        }

        if (errored)
        {
            Debug.LogError(
                "[Cutscene] Playback error: " + lastError);
        }

        DebugCutscene(skipped ? "Skipped." : "Finished.");

        yield return Fade(0f, 1f, fadeOutSeconds);

        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoPlayer.url = null;
        videoPlayer.targetTexture = null;
        videoSurface.texture = null;

        ReleaseRenderTexture();

        EnableSkipAction(false);

        if (skipPrompt != null)
        {
            skipPrompt.SetActive(false);
        }

        SetVideoContentVisible(false);

        // Restore before the reveal, so the first visible frame is
        // already live gameplay rather than a frozen one. This also
        // puts Time.timeScale back before any scene load runs, since
        // GameFlowManager.LoadSceneRoutine uses WaitForSeconds.
        RestoreGameplayState();

        yield return Fade(1f, 0f, fadeOutSeconds);

        SetOverlayVisible(false);

        FinishUp(onFinished, skipped);
    }


    private void FinishUp(
        Action<bool> onFinished,
        bool skipped)
    {
        RestoreGameplayState();

        isPlaying = false;
        playRoutine = null;

        // Callbacks last, so anything they start sees a settled
        // state.
        if (OnCutsceneFinished != null)
        {
            OnCutsceneFinished(skipped);
        }

        if (onFinished != null)
        {
            onFinished(skipped);
        }
    }


    // =========================================================
    // INPUT
    // =========================================================

    private bool IsSkipHeld()
    {
        if (SkipHeldOverride != null)
        {
            return SkipHeldOverride();
        }

        if (skipAction != null && skipAction.action != null)
        {
            return skipAction.action.IsPressed();
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null
            && (keyboard.escapeKey.isPressed
                || keyboard.spaceKey.isPressed))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;

        if (gamepad != null
            && (gamepad.startButton.isPressed
                || gamepad.buttonEast.isPressed))
        {
            return true;
        }

        return false;
    }


    private void EnableSkipAction(bool enable)
    {
        if (skipAction == null || skipAction.action == null)
        {
            return;
        }

        if (enable)
        {
            skipAction.action.Enable();
        }
        else
        {
            skipAction.action.Disable();
        }
    }


    // =========================================================
    // GAME STATE
    // =========================================================

    private void CacheAndOverrideGameplayState()
    {
        stateOverridden = true;

        cachedTimeScale = Time.timeScale;
        cachedLockMode = Cursor.lockState;
        cachedCursorVisible = Cursor.visible;

        if (driveGameStateManager
            && GameStateManager.Instance != null)
        {
            cachedGameMode =
                GameStateManager.Instance.GetGameMode();

            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene);
        }

        if (drivePlayerStateManager)
        {
            cachedPlayerState =
                FindFirstObjectByType<PlayerStateManager>();

            if (cachedPlayerState != null)
            {
                cachedPlayerStateValue =
                    cachedPlayerState.GetCurrentState();

                cachedPlayerState.SetState(
                    PlayerStateManager.PlayerState.Cutscene);
            }
        }

        if (silenceAudioManager
            && AudioManager.Instance != null)
        {
            cachedMusic =
                AudioManager.Instance.GetCurrentMusic();

            cachedAmbience =
                AudioManager.Instance.GetCurrentAmbience();

            AudioManager.Instance.StopMusic();

            AudioManager.Instance.StopAmbience();
        }

        SetHiddenObjectsActive(false);

        if (pauseGameTime)
        {
            Time.timeScale = 0f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // Called mid-routine so gameplay resumes under the fade, and
    // again from FinishUp on the error path. Guarded so the second
    // call is a no-op rather than reapplying stale cached values.
    private void RestoreGameplayState()
    {
        if (!stateOverridden)
        {
            return;
        }

        stateOverridden = false;

        if (pauseGameTime)
        {
            // Never restore a 0, which would leave the game frozen
            // if a cutscene fired while already paused.
            Time.timeScale =
                Mathf.Approximately(cachedTimeScale, 0f)
                    ? 1f
                    : cachedTimeScale;
        }

        SetHiddenObjectsActive(true);

        if (silenceAudioManager
            && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(cachedMusic);

            AudioManager.Instance.PlayAmbience(cachedAmbience);
        }

        if (drivePlayerStateManager
            && cachedPlayerState != null)
        {
            cachedPlayerState.SetState(cachedPlayerStateValue);

            cachedPlayerState = null;
        }

        if (driveGameStateManager
            && GameStateManager.Instance != null)
        {
            // Only restore if nothing else changed the mode while
            // the cutscene ran. GameFlowManager may have already
            // moved the game on.
            if (GameStateManager.Instance.IsGameMode(
                    GameStateManager.GameMode.Cutscene))
            {
                GameStateManager.Instance.SetGameMode(
                    cachedGameMode);
            }
        }

        Cursor.lockState = cachedLockMode;
        Cursor.visible = cachedCursorVisible;
    }


    private void SetHiddenObjectsActive(bool active)
    {
        if (hideDuringCutscene == null)
        {
            return;
        }

        for (int i = 0; i < hideDuringCutscene.Length; i++)
        {
            if (hideDuringCutscene[i] != null)
            {
                hideDuringCutscene[i].SetActive(active);
            }
        }
    }


    // =========================================================
    // SETUP
    // =========================================================

    private void BuildVideoPlayer()
    {
        videoPlayer = gameObject.GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoAudio = gameObject.GetComponent<AudioSource>();

        if (videoAudio == null)
        {
            videoAudio = gameObject.AddComponent<AudioSource>();
        }

        videoAudio.playOnAwake = false;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Route through an AudioSource so the AudioManager volume
        // settings apply. Direct mode bypasses all of them, so a
        // player who muted the game still gets a loud cutscene.
        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.AudioSource;

        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudio);

        videoPlayer.loopPointReached += OnLoopPointReached;
        videoPlayer.errorReceived += OnErrorReceived;
    }


    private void OnLoopPointReached(VideoPlayer source)
    {
        videoReachedEnd = true;
    }


    private void OnErrorReceived(
        VideoPlayer source,
        string message)
    {
        errored = true;
        lastError = message;
    }


    private void BuildOverlay()
    {
        if (overlayCanvas != null
            && videoSurface != null
            && fadeGroup != null)
        {
            aspectFitter =
                videoSurface.GetComponent<AspectRatioFitter>();

            fadeImage = fadeGroup.GetComponent<Image>();

            return;
        }

        GameObject canvasObject = new GameObject(
            "CutsceneOverlay",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32000;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform backdropRect = NewFullScreenChild(
            canvasObject.transform, "Backdrop");

        Image backdropImage =
            backdropRect.gameObject.AddComponent<Image>();

        backdropImage.color = Color.black;

        backdrop = backdropRect.gameObject;

        RectTransform surfaceRect = NewFullScreenChild(
            canvasObject.transform, "VideoSurface");

        videoSurface =
            surfaceRect.gameObject.AddComponent<RawImage>();

        aspectFitter =
            surfaceRect.gameObject.AddComponent<AspectRatioFitter>();

        aspectFitter.aspectMode =
            AspectRatioFitter.AspectMode.FitInParent;

        aspectFitter.aspectRatio = 16f / 9f;

        RectTransform fadeRect = NewFullScreenChild(
            canvasObject.transform, "Fade");

        fadeImage = fadeRect.gameObject.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = false;

        fadeGroup = fadeRect.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = false;
    }


    private static RectTransform NewFullScreenChild(
        Transform parent,
        string childName)
    {
        GameObject child = new GameObject(
            childName, typeof(RectTransform));

        RectTransform rect =
            child.GetComponent<RectTransform>();

        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return rect;
    }


    private void SetOverlayVisible(bool visible)
    {
        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(visible);
        }
    }


    // Toggles the backdrop and video surface together, leaving the
    // fade layer alone, so a fade can run over live gameplay before
    // the video is revealed.
    private void SetVideoContentVisible(bool visible)
    {
        if (backdrop != null)
        {
            backdrop.SetActive(visible);
        }

        if (videoSurface != null)
        {
            videoSurface.gameObject.SetActive(visible);
        }
    }


    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        if (fadeImage != null)
        {
            fadeImage.color = fadeColor;
        }

        if (duration <= 0f)
        {
            fadeGroup.alpha = to;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Unscaled, because Time.timeScale is 0 during a
            // cutscene.
            elapsed += Time.unscaledDeltaTime;

            fadeGroup.alpha = Mathf.Lerp(
                from, to, Mathf.Clamp01(elapsed / duration));

            yield return null;
        }

        fadeGroup.alpha = to;
    }


    private void AllocateRenderTexture(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            width = 1920;
            height = 1080;
        }

        if (renderTexture != null
            && renderTexture.width == width
            && renderTexture.height == height)
        {
            return;
        }

        ReleaseRenderTexture();

        renderTexture = new RenderTexture(
            width, height, 0, RenderTextureFormat.ARGB32);

        renderTexture.name = "CutsceneRT";
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Bilinear;

        renderTexture.Create();
    }


    private void ReleaseRenderTexture()
    {
        if (renderTexture == null)
        {
            return;
        }

        if (renderTexture.IsCreated())
        {
            renderTexture.Release();
        }

        Destroy(renderTexture);

        renderTexture = null;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugCutscene(string message)
    {
        if (!debugCutscene)
        {
            return;
        }

        Debug.Log("[Cutscene] " + message);
    }
}
