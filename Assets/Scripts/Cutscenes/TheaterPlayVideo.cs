using UnityEngine;
using UnityEngine.Video;

public class TheaterPlayVideo : MonoBehaviour
{
    [Header("Video")]

    [SerializeField]
    private VideoPlayer videoPlayer;


    [Header("Settings")]

    [SerializeField]
    private bool renderToMainCamera = true;


    [SerializeField]
    private bool debugVideo = true;


    public bool IsPlaying
    {
        get
        {
            return videoPlayer != null &&
                   videoPlayer.isPlaying;
        }
    }


    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer =
                GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError(
                "[TheaterPlayVideo] VideoPlayer missing."
            );

            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        if (renderToMainCamera)
        {
            SetupCamera();
        }
    }


    // =========================================================
    // PLAY
    // =========================================================

    public void Play(
        System.Action onFinished)
    {
        if (videoPlayer == null)
        {
            Debug.LogError(
                "[TheaterPlayVideo] Cannot play. " +
                "VideoPlayer missing."
            );

            onFinished?.Invoke();

            return;
        }

        videoPlayer.loopPointReached -=
            HandleFinished;

        videoPlayer.errorReceived -=
            HandleError;

        finishCallback = onFinished;

        videoPlayer.loopPointReached +=
            HandleFinished;

        videoPlayer.errorReceived +=
            HandleError;


        SetupCamera();

        videoPlayer.Stop();

        videoPlayer.Prepare();

        videoPlayer.prepareCompleted -=
            HandlePrepared;

        videoPlayer.prepareCompleted +=
            HandlePrepared;
    }


    // =========================================================
    // PREPARED
    // =========================================================

    private void HandlePrepared(
        VideoPlayer player)
    {
        player.prepareCompleted -=
            HandlePrepared;

        DebugVideo(
            "Video prepared. Playing."
        );

        player.Play();
    }


    // =========================================================
    // FINISHED
    // =========================================================

    private void HandleFinished(
        VideoPlayer player)
    {
        DebugVideo(
            "Theater video finished."
        );

        CleanupEvents();

        System.Action callback =
            finishCallback;

        finishCallback = null;

        callback?.Invoke();
    }


    // =========================================================
    // ERROR
    // =========================================================

    private void HandleError(
        VideoPlayer player,
        string message)
    {
        Debug.LogError(
            "[TheaterPlayVideo] Video error: "
            + message
        );

        CleanupEvents();

        System.Action callback =
            finishCallback;

        finishCallback = null;

        callback?.Invoke();
    }


    // =========================================================
    // STOP
    // =========================================================

    public void Stop()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.Stop();

        CleanupEvents();

        finishCallback = null;
    }


    // =========================================================
    // CAMERA
    // =========================================================

    private void SetupCamera()
    {
        if (Camera.main == null)
        {
            return;
        }

        videoPlayer.renderMode =
            VideoRenderMode.CameraNearPlane;

        videoPlayer.targetCamera =
            Camera.main;

        videoPlayer.targetCameraAlpha = 1f;
    }


    // =========================================================
    // EVENTS
    // =========================================================

    private void CleanupEvents()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -=
            HandlePrepared;

        videoPlayer.loopPointReached -=
            HandleFinished;

        videoPlayer.errorReceived -=
            HandleError;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugVideo(string message)
    {
        if (!debugVideo)
        {
            return;
        }

        Debug.Log(
            "[TheaterPlayVideo] " + message
        );
    }


    // =========================================================
    // CALLBACK
    // =========================================================

    private System.Action finishCallback;
}