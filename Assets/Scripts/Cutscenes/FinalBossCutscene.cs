using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays the final boss ending cutscene and then hands control back to
/// GameFlowManager so the credits can begin.
/// </summary>
public class FinalBossCutscene : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Settings")]
    [SerializeField] private bool renderToMainCamera = true;

    [Header("Debug")]
    [SerializeField] private bool debugCutscene = true;

    private bool playing;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;

        if (renderToMainCamera)
        {
            SetupCamera();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        playing = false;
    }

    public void Play()
    {
        if (playing)
        {
            return;
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning(
                "[FinalBossCutscene] VideoPlayer is missing. " +
                "Finishing the final cutscene immediately."
            );

            Finish();
            return;
        }

        playing = true;

        Subscribe();
        SetupCamera();

        videoPlayer.Stop();
        videoPlayer.Prepare();
    }

    private void HandlePrepared(VideoPlayer player)
    {
        if (!playing || player != videoPlayer)
        {
            return;
        }

        player.Play();

        DebugCutscene("Final cutscene started.");
    }

    private void HandleFinished(VideoPlayer player)
    {
        if (player != videoPlayer)
        {
            return;
        }

        Finish();
    }

    private void HandleError(VideoPlayer player, string message)
    {
        if (player != videoPlayer)
        {
            return;
        }

        Debug.LogError(
            "[FinalBossCutscene] Video error: " + message
        );

        Finish();
    }

    private void Finish()
    {
        if (!playing)
        {
            // Also allow Finish() to be used when no VideoPlayer exists.
            if (videoPlayer != null)
            {
                return;
            }
        }

        playing = false;
        Unsubscribe();

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.FinalCutsceneFinished();
        }
    }

    private void SetupCamera()
    {
        if (videoPlayer == null ||
            !renderToMainCamera ||
            Camera.main == null)
        {
            return;
        }

        videoPlayer.renderMode =
            VideoRenderMode.CameraNearPlane;

        videoPlayer.targetCamera =
            Camera.main;

        videoPlayer.targetCameraAlpha = 1f;
    }

    private void Subscribe()
    {
        if (videoPlayer == null)
        {
            return;
        }

        Unsubscribe();

        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.loopPointReached += HandleFinished;
        videoPlayer.errorReceived += HandleError;
    }

    private void Unsubscribe()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= HandlePrepared;
        videoPlayer.loopPointReached -= HandleFinished;
        videoPlayer.errorReceived -= HandleError;
    }

    private void DebugCutscene(string message)
    {
        if (!debugCutscene)
        {
            return;
        }

        Debug.Log(
            "[FinalBossCutscene] " + message,
            this
        );
    }
}
