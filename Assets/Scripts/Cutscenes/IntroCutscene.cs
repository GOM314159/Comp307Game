using UnityEngine;
using UnityEngine.Video;

public class IntroCutscene : MonoBehaviour
{
    // =========================================================
    // VIDEO PLAYER
    // =========================================================

    [Header("Video Player")]

    [SerializeField]
    private VideoPlayer videoPlayer;


    // =========================================================
    // STATE
    // =========================================================

    private bool hasFinished = false;
    private bool isPlaying = false;

    private System.Action finishCallback;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError(
                "[IntroCutscene] No VideoPlayer found."
            );

            return;
        }

        // -----------------------------------------------------
        // VIDEO SETTINGS
        // -----------------------------------------------------

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        // -----------------------------------------------------
        // FORCE VIDEO TO RENDER TO THE CAMERA
        // -----------------------------------------------------

        if (Camera.main != null)
        {
            videoPlayer.renderMode =
                VideoRenderMode.CameraNearPlane;

            videoPlayer.targetCamera =
                Camera.main;

            videoPlayer.targetCameraAlpha = 1f;
        }
        else
        {
            Debug.LogWarning(
                "[IntroCutscene] Main Camera not found."
            );
        }

        // -----------------------------------------------------
        // EVENTS
        // -----------------------------------------------------

        videoPlayer.loopPointReached +=
            OnVideoFinished;

        videoPlayer.errorReceived +=
            OnVideoError;

        videoPlayer.prepareCompleted +=
            OnVideoPrepared;

        // -----------------------------------------------------
        // START STOPPED
        // -----------------------------------------------------

        videoPlayer.Stop();
    }


    // =========================================================
    // PLAY INTRO
    // =========================================================

    public void PlayIntro()
    {
        if (videoPlayer == null)
        {
            Debug.LogError(
                "[IntroCutscene] Cannot play intro. " +
                "VideoPlayer is missing."
            );

            FinishCutscene();

            return;
        }

        if (isPlaying)
        {
            return;
        }

        hasFinished = false;
        isPlaying = true;

        Debug.Log(
            "[IntroCutscene] Preparing intro video."
        );

        // -----------------------------------------------------
        // MAKE SURE CAMERA OUTPUT IS SET
        // -----------------------------------------------------

        if (Camera.main != null)
        {
            videoPlayer.renderMode =
                VideoRenderMode.CameraNearPlane;

            videoPlayer.targetCamera =
                Camera.main;

            videoPlayer.targetCameraAlpha = 1f;
        }

        // -----------------------------------------------------
        // PREPARE VIDEO
        // -----------------------------------------------------
        SetupCamera();
        videoPlayer.Stop();
        videoPlayer.Prepare();
    }


    // =========================================================
    // VIDEO PREPARED
    // =========================================================

    private void OnVideoPrepared(VideoPlayer player)
    {
        Debug.Log(
            "[IntroCutscene] Video prepared. Starting playback."
        );

        player.Play();
    }


    // =========================================================
    // VIDEO FINISHED
    // =========================================================

    private void OnVideoFinished(VideoPlayer player)
    {
        FinishCutscene();
    }


    // =========================================================
    // VIDEO ERROR
    // =========================================================

    private void OnVideoError(
        VideoPlayer player,
        string message)
    {
        Debug.LogError(
            "[IntroCutscene] Video error: "
            + message
        );

        FinishCutscene();
    }


    // =========================================================
    // FINISH
    // =========================================================

    private void FinishCutscene()
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;
        isPlaying = false;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        Debug.Log(
            "[IntroCutscene] Intro cutscene finished."
        );


        // =========================================================
        // CUSTOM CALLBACK
        // =========================================================

        if (finishCallback != null)
        {
            System.Action callback =
                finishCallback;

            finishCallback = null;

            callback.Invoke();

            return;
        }


        // =========================================================
        // NORMAL GAME FLOW
        // =========================================================

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.IntroFinished();
        }
    }

    private void SetupCamera()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[IntroCutscene] Main Camera not found."
            );

            return;
        }

        videoPlayer.renderMode =
            VideoRenderMode.CameraNearPlane;

        videoPlayer.targetCamera =
            Camera.main;

        videoPlayer.targetCameraAlpha = 1f;
    }

    public void PlayIntroWithoutFlowCallback(
    System.Action onFinished)
    {
        if (videoPlayer == null)
        {
            Debug.LogError(
                "[IntroCutscene] VideoPlayer is missing."
            );

            onFinished?.Invoke();

            return;
        }

        hasFinished = false;
        isPlaying = true;

        finishCallback = onFinished;

        SetupCamera();

        videoPlayer.Stop();
        videoPlayer.Prepare();
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.loopPointReached -=
            OnVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;

        videoPlayer.prepareCompleted -=
            OnVideoPrepared;
    }
}