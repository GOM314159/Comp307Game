using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

/// <summary>
/// Four day-transition cutscenes that play after the lobby has loaded.
/// Assign Day 1..Day 4 VideoClips in order.
/// </summary>
public class DayScreenCutscene : MonoBehaviour
{
    public static DayScreenCutscene Instance { get; private set; }

    [Header("Day Videos")]
    [Tooltip("Element 0 = Day 1, Element 1 = Day 2, Element 2 = Day 3, Element 3 = Day 4.")]
    [SerializeField]
    private List<VideoClip> dayVideos = new List<VideoClip>(4);

    [Header("Playback")]
    [SerializeField] private bool playAudio = true;
    [SerializeField] private bool debugDayScreens = true;

    [Header("After Day Screen")]
    [Tooltip("Optional points indicator object to show briefly once the day-screen video finishes.")]
    [SerializeField] private GameObject pointsIndicator;
    [SerializeField, Min(0f)] private float pointsIndicatorDuration = 4f;
    private Coroutine pointsIndicatorRoutine;

    public static event Action<int> OnDayScreenFinished;

    [Header("Optional Inspector-Owned Video Presentation")]
    [Tooltip("Assign a dedicated full-screen overlay Canvas here. If assigned, the script will use it instead of creating a Canvas at runtime.")]
    [SerializeField] private Canvas assignedCanvas;
    [Tooltip("RawImage under the Canvas, anchored to all four screen edges.")]
    [SerializeField] private RawImage assignedVideoImage;
    [Tooltip("VideoPlayer attached to this day-screen presentation.")]
    [SerializeField] private VideoPlayer assignedVideoPlayer;
    [Tooltip("RenderTexture used by the VideoPlayer and RawImage.")]
    [SerializeField] private RenderTexture assignedRenderTexture;
    [Tooltip("AudioSource for the assigned VideoPlayer; optional if audio is disabled.")]
    [SerializeField] private AudioSource assignedAudioSource;

    private Canvas canvas;
    private RawImage rawImage;
    private RenderTexture renderTexture;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private bool ownsRenderTexture;
    private Coroutine playRoutine;
    private bool isPlaying;
    private int playingDay = -1;

    private GameStateManager.GameMode previousGameMode =
        GameStateManager.GameMode.Lobby;
    private PlayerStateManager.PlayerState previousPlayerState =
        PlayerStateManager.PlayerState.AllMovement;
    private float previousTimeScale = 1f;
    private bool previousTimeScaleCaptured;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (pointsIndicator != null) pointsIndicator.SetActive(false);

        // Prefer explicitly attached Inspector objects. This lets the Canvas/RawImage
        // use Unity's RectTransform stretch anchors rather than relying on runtime sizing.
        if (assignedCanvas != null && assignedVideoImage != null && assignedVideoPlayer != null)
        {
            canvas = assignedCanvas;
            rawImage = assignedVideoImage;
            videoPlayer = assignedVideoPlayer;
            renderTexture = assignedRenderTexture != null ? assignedRenderTexture : videoPlayer.targetTexture;
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(Screen.width > 0 ? Screen.width : 1920,
                    Screen.height > 0 ? Screen.height : 1080, 0, RenderTextureFormat.ARGB32);
                renderTexture.name = "DayScreenVideo_RenderTexture_Runtime";
                renderTexture.Create();
                ownsRenderTexture = true;
            }
            audioSource = assignedAudioSource != null ? assignedAudioSource : videoPlayer.GetComponent<AudioSource>();
            ConfigureAssignedPresentation();
        }
        else
        {
            BuildPresentation();
        }
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (ownsRenderTexture && renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    public void PlayDayScreenAfterLobbyLoad(int day)
    {
        if (day < 1 || day > dayVideos.Count)
        {
            DebugDay("No configured day screen for Day " + day + ".");
            BeginDay1TutorialIfNeeded(day);
            return;
        }

        VideoClip clip = dayVideos[day - 1];
        if (clip == null)
        {
            DebugDay("Day " + day + " has no assigned VideoClip.");
            BeginDay1TutorialIfNeeded(day);
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayAfterLobbyLoad(clip, day));
    }

    private void BeginDay1TutorialIfNeeded(int day)
    {
        if (day != 1) return;
        StartCoroutine(BeginDay1TutorialNextFrame());
    }

    private IEnumerator BeginDay1TutorialNextFrame()
    {
        // Let GameFlowManager finish restoring the lobby spawn first.
        yield return null;
        TutorialManager tutorial = TutorialManager.Instance;
        if (tutorial == null)
            tutorial = FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (tutorial != null)
            tutorial.BeginDay1Tutorial();
        else
            Debug.LogError("[DayScreenCutscene] Day 1 has no video and no TutorialManager is available.");
    }

    private IEnumerator PlayAfterLobbyLoad(VideoClip clip, int day)
    {
        playingDay = day;

        // GameFlow hides LoadingScreenManager before this call. Waiting one
        // frame plus EndOfFrame guarantees the day screen appears afterward.
        yield return null;
        yield return new WaitForEndOfFrame();

        isPlaying = true;

        previousTimeScale = Time.timeScale;
        previousTimeScaleCaptured = true;

        if (GameStateManager.Instance != null)
        {
            previousGameMode = GameStateManager.Instance.GetGameMode();
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene
            );
        }

        if (PlayerStateManager.Instance != null)
        {
            previousPlayerState =
                PlayerStateManager.Instance.GetCurrentState();
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }

        // Day screens are treated exactly like other gameplay cutscenes:
        // the player cannot see or use the mouse cursor while the video is
        // on screen.
        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.HideCursorForGameplay();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
        }

        Show();

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;

        if (playAudio)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.EnableAudioTrack(0, true);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        bool prepared = false;
        bool preparationFailed = false;

        void Prepared(VideoPlayer source)
        {
            prepared = true;
        }

        void PreparationError(VideoPlayer source, string message)
        {
            preparationFailed = true;
            Debug.LogError(
                "[DayScreen] Day " + day + " prepare error: " + message
            );
        }

        videoPlayer.prepareCompleted += Prepared;
        videoPlayer.errorReceived += PreparationError;
        videoPlayer.Prepare();

        float elapsed = 0f;
        const float prepareTimeout = 10f;
        while (!prepared && !preparationFailed && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        videoPlayer.prepareCompleted -= Prepared;
        videoPlayer.errorReceived -= PreparationError;

        if (!prepared || preparationFailed)
        {
            Finish(day);
            yield break;
        }

        bool finished = false;
        bool playbackFailed = false;

        void Finished(VideoPlayer source)
        {
            finished = true;
        }

        void PlaybackError(VideoPlayer source, string message)
        {
            playbackFailed = true;
            Debug.LogError(
                "[DayScreen] Day " + day + " playback error: " + message
            );
        }

        videoPlayer.loopPointReached += Finished;
        videoPlayer.errorReceived += PlaybackError;
        videoPlayer.Play();

        DebugDay("Playing Day " + day + " screen.");

        while (!finished && !playbackFailed)
        {
            yield return null;
        }

        videoPlayer.loopPointReached -= Finished;
        videoPlayer.errorReceived -= PlaybackError;

        Finish(day);
    }

    private void Finish(int day)
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        isPlaying = false;
        Hide();

        if (previousTimeScaleCaptured)
        {
            Time.timeScale = previousTimeScale;
            previousTimeScaleCaptured = false;
        }

        // Day screens are a transition presentation after the lobby loads.
        // Always return to the lobby state after the video, rather than
        // leaving the player in Cutscene state if playback ended normally.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }

        // Keep the cursor locked/hidden when the day screen ends so the
        // transition back to gameplay cannot briefly expose the mouse.
        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.HideCursorForGameplay();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Lobby
            );
            AudioManager.Instance.PlayAmbience(
                AudioManager.AmbienceType.Lobby
            );
        }

        if (pointsIndicatorRoutine != null)
            StopCoroutine(pointsIndicatorRoutine);
        if (pointsIndicator != null)
        {
            pointsIndicator.SetActive(true);
            pointsIndicatorRoutine = StartCoroutine(HidePointsIndicatorAfterDelay());
        }

        // Notify post-return lobby systems only after the day-screen has ended
        // and the player has been restored to lobby control.
        OnDayScreenFinished?.Invoke(day);

        // Day 1 is followed by the interactive tutorial. It starts only
        // after the day video has completely finished and lobby state/music
        // have been restored.
        if (day == 1)
        {
            TutorialManager tutorial = TutorialManager.Instance;
            if (tutorial == null) tutorial = FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
            if (tutorial != null) tutorial.BeginDay1Tutorial();
            else Debug.LogError("[DayScreenCutscene] Day 1 ended, but no TutorialManager exists in the loaded scene or persistent managers.");
        }

        // Reassert gameplay mouse focus after state and UI callbacks finish.
        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.HideCursorForGameplay();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playRoutine = null;
        playingDay = -1;
    }

    private IEnumerator HidePointsIndicatorAfterDelay()
    {
        yield return new WaitForSecondsRealtime(pointsIndicatorDuration);
        if (pointsIndicator != null) pointsIndicator.SetActive(false);
        pointsIndicatorRoutine = null;
    }

    private void ConfigureAssignedPresentation()
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7000;
        canvas.pixelPerfect = false;

        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rawImage.raycastTarget = false;
        // This RawImage is only the UI surface that displays the live video output,
        // not a still-image asset. The VideoPlayer writes each video frame to this texture.
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.gameObject.SetActive(false);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void BuildPresentation()
    {
        GameObject canvasObject = new GameObject("DayScreenCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = 7000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("DayScreenVideoImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        rawImage = imageObject.AddComponent<RawImage>();
        rawImage.raycastTarget = false;

        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetAsLastSibling();

        renderTexture = new RenderTexture(
            1920,
            1080,
            0,
            RenderTextureFormat.ARGB32
        );
        renderTexture.Create();
        ownsRenderTexture = true;
        rawImage.texture = renderTexture;

        GameObject videoObject = new GameObject("DayScreenVideoPlayer");
        videoObject.transform.SetParent(transform, false);

        videoPlayer = videoObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.Stretch;

        audioSource = videoObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        imageObject.SetActive(false);
    }

    private void Show()
    {
        if (rawImage != null)
        {
            rawImage.gameObject.SetActive(true);
        }
    }

    private void Hide()
    {
        if (rawImage != null)
        {
            rawImage.gameObject.SetActive(false);
        }
    }

    public void StopDayScreen()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (isPlaying)
        {
            Finish(playingDay);
        }
        else
        {
            Hide();
        }
    }

    private void DebugDay(string message)
    {
        if (debugDayScreens)
        {
            Debug.Log("[DayScreen] " + message);
        }
    }
}
