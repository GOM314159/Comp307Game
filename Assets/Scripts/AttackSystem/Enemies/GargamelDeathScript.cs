using UnityEngine;
using UnityEngine.Video;

public class GargamelDeathScript : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField]
    private Health bossHealth;

    [Header("Normal Ending")]
    [SerializeField]
    private VideoPlayer normalEndingVideo;

    [Header("Secret Ending")]
    [SerializeField]
    private VideoPlayer secretEndingVideo;

    [Header("Final Game Screen")]
    [SerializeField]
    private GameObject finalGameScreen;

    [Header("Settings")]
    [SerializeField]
    private bool playOnDeath = true;

    private bool endingStarted;
    private VideoPlayer activeVideo;

    private void Awake()
    {
        if (bossHealth == null)
        {
            bossHealth = GetComponent<Health>();
        }

        SetupVideo(normalEndingVideo);
        SetupVideo(secretEndingVideo);

        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied += HandleBossDeath;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied -= HandleBossDeath;
        }

        UnsubscribeFromVideo(normalEndingVideo);
        UnsubscribeFromVideo(secretEndingVideo);
    }

    private void HandleBossDeath()
    {
        if (endingStarted || !playOnDeath)
        {
            return;
        }

        endingStarted = true;

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

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }

        bool secretEnding =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.HasCollectedAllExodiaPieces();

        if (secretEnding)
        {
            PlayVideo(secretEndingVideo);
        }
        else
        {
            PlayVideo(normalEndingVideo);
        }
    }

    private void PlayVideo(VideoPlayer video)
    {
        if (video == null)
        {
            ShowFinalGameScreen();
            return;
        }

        activeVideo = video;

        SubscribeToVideo(video);

        video.Stop();
        video.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (player != activeVideo)
        {
            return;
        }

        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        if (player != activeVideo)
        {
            return;
        }

        UnsubscribeFromVideo(player);

        activeVideo = null;

        ShowFinalGameScreen();
    }

    private void OnVideoError(
        VideoPlayer player,
        string message
    )
    {
        if (player != activeVideo)
        {
            return;
        }

        Debug.LogError(
            "[GargamelDeathScript] Video error: "
            + message
        );

        UnsubscribeFromVideo(player);

        activeVideo = null;

        ShowFinalGameScreen();
    }

    private void ShowFinalGameScreen()
    {
        // Final game screen to be added later

        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(true);
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Credits
            );
        }
    }

    private void SetupVideo(VideoPlayer video)
    {
        if (video == null)
        {
            return;
        }

        video.playOnAwake = false;
        video.isLooping = false;
        video.waitForFirstFrame = true;
        video.skipOnDrop = false;

        if (Camera.main != null)
        {
            video.renderMode =
                VideoRenderMode.CameraNearPlane;

            video.targetCamera =
                Camera.main;

            video.targetCameraAlpha = 1f;
        }
    }

    private void SubscribeToVideo(VideoPlayer video)
    {
        video.prepareCompleted += OnVideoPrepared;
        video.loopPointReached += OnVideoFinished;
        video.errorReceived += OnVideoError;
    }

    private void UnsubscribeFromVideo(VideoPlayer video)
    {
        if (video == null)
        {
            return;
        }

        video.prepareCompleted -= OnVideoPrepared;
        video.loopPointReached -= OnVideoFinished;
        video.errorReceived -= OnVideoError;
    }

    public void ResetDeathSequence()
    {
        endingStarted = false;

        if (activeVideo != null)
        {
            activeVideo.Stop();
            UnsubscribeFromVideo(activeVideo);
            activeVideo = null;
        }

        if (normalEndingVideo != null)
        {
            normalEndingVideo.Stop();
        }

        if (secretEndingVideo != null)
        {
            secretEndingVideo.Stop();
        }

        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(false);
        }
    }
}