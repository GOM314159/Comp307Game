using UnityEngine;
using UnityEngine.Video;

public class GachaWinCutscene : MonoBehaviour
{
    [Header("Gacha win video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject cutsceneScreen;
    [SerializeField] private bool openCreditsWhenFinished = true;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        if (cutsceneScreen != null)
        {
            cutsceneScreen.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    public void Play()
    {
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

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }

        if (cutsceneScreen != null)
        {
            cutsceneScreen.SetActive(true);
        }

        if (videoPlayer == null || videoPlayer.clip == null)
        {
            Finish();
            return;
        }

        videoPlayer.Stop();
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        Finish();
    }

    private void Finish()
    {
        if (cutsceneScreen != null)
        {
            cutsceneScreen.SetActive(false);
        }

        if (openCreditsWhenFinished &&
            GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OpenCredits();
        }
    }
}
