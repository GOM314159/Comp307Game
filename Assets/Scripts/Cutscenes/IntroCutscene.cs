using UnityEngine;
using UnityEngine.Video;

public class IntroCutscene : MonoBehaviour
{
    [Header("Video Player")]
    [SerializeField] private VideoPlayer videoPlayer;


    private bool hasFinished = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer =
                GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning(
                "[IntroCutscene] No VideoPlayer found. " +
                "Returning to Lobby."
            );

            FinishCutscene();

            return;
        }

        // Listen for the video finishing.
        videoPlayer.loopPointReached += OnVideoFinished;


        Debug.Log(
            "[IntroCutscene] Intro cutscene started."
        );
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

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

        if (hasFinished)
        {
            return;
        }

        Debug.Log(
            "[IntroCutscene] Playing intro cutscene."
        );

        videoPlayer.Play();
    }


    // =========================================================
    // VIDEO FINISHED
    // =========================================================

    private void OnVideoFinished(VideoPlayer player)
    {
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

        Debug.Log(
            "[IntroCutscene] Intro cutscene finished."
        );

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.IntroFinished();
        }
        else
        {
            Debug.LogError(
                "[IntroCutscene] GameFlowManager.Instance is missing."
            );
        }
    }
}