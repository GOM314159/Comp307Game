using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Gargamel's final death/cutscene controller.
/// IMPORTANT: Gargamel uses FinalBossHealth, not the normal Health component.
/// Player and manager references are discovered at runtime where required.
/// </summary>
public class GargamelDeathScript : MonoBehaviour
{
    [Header("Boss")]
    [Tooltip("Automatically uses the FinalBossHealth on Gargamel.")]
    [SerializeField] private FinalBossHealth bossHealth;

    [Header("Normal Ending")]
    [SerializeField] private VideoPlayer normalEndingVideo;

    [Header("Secret Ending")]
    [SerializeField] private VideoPlayer secretEndingVideo;

    [Header("Final Game Screen")]
    [SerializeField] private GameObject finalGameScreen;

    [Header("Settings")]
    [SerializeField] private bool playOnDeath = true;

    [Header("Debug")]
    [SerializeField] private bool debugDeath = true;

    private bool endingStarted;
    private VideoPlayer activeVideo;

    private void Awake()
    {
        if (bossHealth == null)
        {
            bossHealth = GetComponent<FinalBossHealth>();
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
        if (bossHealth == null)
        {
            bossHealth = GetComponent<FinalBossHealth>();
        }

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
        TriggerDeathSequence();
    }

    /// <summary>
    /// Can also be called manually by another final-boss system.
    /// </summary>
    public void TriggerDeathSequence()
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
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.UnlockSecretEnding1();
            }

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
            FinishEndingVideo();
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
        FinishEndingVideo();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        if (player != activeVideo)
        {
            return;
        }

        Debug.LogError(
            "[GargamelDeathScript] Video error: " + message
        );

        UnsubscribeFromVideo(player);
        activeVideo = null;
        FinishEndingVideo();
    }

    private void FinishEndingVideo()
    {
        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(true);
        }

        // GameFlowManager owns the persistent end-of-game transition.
        // It will open Credits and then return to MainMenu.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.FinalCutsceneFinished();
            return;
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
            video.renderMode = VideoRenderMode.CameraNearPlane;
            video.targetCamera = Camera.main;
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
