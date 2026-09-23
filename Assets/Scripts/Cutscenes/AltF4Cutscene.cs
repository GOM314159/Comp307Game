using UnityEngine;
using UnityEngine.Video;

public class AltF4ExitCutscene : MonoBehaviour
{


    [Header("Exit Cutscene Video")]

    [SerializeField]
    private VideoPlayer videoPlayer;



    [Header("Settings")]

    [SerializeField]
    private bool enableAltF4ExitCutscene = true;

    [SerializeField]
    private bool renderToMainCamera = true;

    [SerializeField]
    private bool debugExitCutscene = true;

    private bool exitSequenceRunning = false;

    private bool applicationIsQuitting = false;
    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError(
                "[AltF4ExitCutscene] No VideoPlayer assigned."
            );

            return;
        }



        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;

        videoPlayer.loopPointReached -=
            OnVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;

        videoPlayer.loopPointReached +=
            OnVideoFinished;

        videoPlayer.errorReceived +=
            OnVideoError;

        if (renderToMainCamera)
        {
            SetupCamera();
        }
    }


    private void OnEnable()
    {
        Application.wantsToQuit +=
            HandleApplicationWantsToQuit;
    }


    private void OnDisable()
    {
        Application.wantsToQuit -=
            HandleApplicationWantsToQuit;
    }


    private bool HandleApplicationWantsToQuit()
    {

        if (applicationIsQuitting)
        {
            return true;
        }



        if (!enableAltF4ExitCutscene)
        {
            return true;
        }

        if (exitSequenceRunning)
        {
            // The user pressed Alt+F4 again while the video wa playing lol


            DebugExit(
                "Quit request ignored because exit cutscene " +
                "is already playing."
            );

            return false;
        }


        DebugExit(
            "Alt+F4 / application quit detected. " +
            "Starting exit cutscene."
        );


        exitSequenceRunning = true;

        StartExitCutscene();

        return false;
    }

    private void StartExitCutscene()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene
            );
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                false
            );
        }


        if (videoPlayer == null)
        {
            Debug.LogError(
                "[AltF4ExitCutscene] VideoPlayer is missing. " +
                "Closing application."
            );

            QuitApplication();

            return;
        }

        SetupCamera();

        videoPlayer.Stop();

        videoPlayer.Prepare();

        videoPlayer.prepareCompleted -=
            OnVideoPrepared;

        videoPlayer.prepareCompleted +=
            OnVideoPrepared;
    }
    private void OnVideoPrepared(VideoPlayer player)
    {
        player.prepareCompleted -=
            OnVideoPrepared;

        DebugExit(
            "Exit video prepared. Starting playback."
        );

        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        DebugExit(
            "Exit video finished. Closing application."
        );

        QuitApplication();
    }

    private void OnVideoError(
        VideoPlayer player,
        string message)
    {
        Debug.LogError(
            "[AltF4ExitCutscene] Video error: "
            + message
        );


        QuitApplication();
    }

    private void QuitApplication()
    {
        if (applicationIsQuitting)
        {
            return;
        }

        applicationIsQuitting = true;

        DebugExit(
            "Calling Application.Quit()."
        );

        Application.Quit();
    }

    private void SetupCamera()
    {
        if (!renderToMainCamera)
        {
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[AltF4ExitCutscene] Main Camera not found."
            );

            return;
        }

        videoPlayer.renderMode =
            VideoRenderMode.CameraNearPlane;

        videoPlayer.targetCamera =
            Camera.main;

        videoPlayer.targetCameraAlpha = 1f;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugExit(string message)
    {
        if (!debugExitCutscene)
        {
            return;
        }

        Debug.Log(
            "[AltF4ExitCutscene] " + message
        );
    }


    private void OnDestroy()
    {
        Application.wantsToQuit -=
            HandleApplicationWantsToQuit;

        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -=
            OnVideoPrepared;

        videoPlayer.loopPointReached -=
            OnVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;
    }
}
