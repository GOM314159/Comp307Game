using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Gargamel's final death/cutscene controller.
/// IMPORTANT: Gargamel uses FinalBossHealth, not the normal Health component.
/// Player and manager references are discovered at runtime where required.
/// </summary>
public class GargamelDeathScript : MonoBehaviour
{
    [Header("Boss")]
    [Tooltip("Automatically finds FinalBossHealth on this object, children, parents, or in the scene.")]
    [SerializeField] private FinalBossHealth bossHealth;

    [Header("Normal Ending")]
    [SerializeField] private VideoPlayer normalEndingVideo;

    [Header("Secret Ending")]
    [SerializeField] private VideoPlayer secretEndingVideo;

    [Header("Final Game Screen")]
    [Tooltip("Optional direct reference. Leave empty to find by object name.")]
    [SerializeField] private GameObject finalGameScreen;

    [Tooltip(
        "Exact GameObject name of the final end screen UI in the loaded scene " +
        "(e.g. EndScreen). Used when the direct reference is missing."
    )]
    [SerializeField] private string finalGameScreenObjectName = "EndScreen";

    [Header("Settings")]
    [SerializeField] private bool playOnDeath = true;

    [Header("Debug")]
    [SerializeField] private bool debugDeath = true;

    private bool endingStarted;
    private bool subscribedToHealth;
    private VideoPlayer activeVideo;

    private void Awake()
    {
        ResolveBossHealth();
        ResolveFinalGameScreen();

        SetupVideo(normalEndingVideo);
        SetupVideo(secretEndingVideo);

        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ResolveBossHealth();
        ResolveFinalGameScreen();
        SubscribeToBossHealth();
    }

    private void Start()
    {
        // Late bind in case FinalBossHealth / EndScreen are added later.
        ResolveBossHealth();
        ResolveFinalGameScreen();
        SubscribeToBossHealth();
    }

    private void OnDisable()
    {
        UnsubscribeFromBossHealth();

        UnsubscribeFromVideo(normalEndingVideo);
        UnsubscribeFromVideo(secretEndingVideo);
    }

    private void ResolveBossHealth()
    {
        if (bossHealth != null)
        {
            return;
        }

        bossHealth = GetComponent<FinalBossHealth>();

        if (bossHealth == null)
        {
            bossHealth = GetComponentInChildren<FinalBossHealth>(true);
        }

        if (bossHealth == null)
        {
            bossHealth = GetComponentInParent<FinalBossHealth>();
        }

        if (bossHealth == null)
        {
            bossHealth = FindAnyObjectByType<FinalBossHealth>(
                FindObjectsInactive.Include
            );
        }

        if (bossHealth == null)
        {
            Debug.LogError(
                "[GargamelDeathScript] FinalBossHealth could not be found " +
                "on " + gameObject.name +
                " or in the scene. Death cutscene will not trigger.",
                this
            );
            return;
        }

        if (debugDeath)
        {
            Debug.Log(
                "[GargamelDeathScript] Bound to FinalBossHealth on " +
                bossHealth.gameObject.name +
                ".",
                this
            );
        }
    }

    private void ResolveFinalGameScreen()
    {
        if (finalGameScreen != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(finalGameScreenObjectName))
        {
            return;
        }

        string targetName = finalGameScreenObjectName.Trim();

        finalGameScreen =
            FindSceneObjectByNameAcrossLoadedScenes(targetName);

        if (finalGameScreen == null)
        {
            // Also catch inactive UI objects that name search can miss.
            GameObject[] allObjects =
                Resources.FindObjectsOfTypeAll<GameObject>();

            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject candidate = allObjects[i];

                if (candidate == null ||
                    candidate.name != targetName ||
                    !candidate.scene.IsValid() ||
                    !candidate.scene.isLoaded)
                {
                    continue;
                }

                finalGameScreen = candidate;
                break;
            }
        }

        if (finalGameScreen == null)
        {
            Debug.LogWarning(
                "[GargamelDeathScript] Final game screen '" +
                targetName +
                "' was not found in any loaded scene.",
                this
            );
            return;
        }

        if (debugDeath)
        {
            Debug.Log(
                "[GargamelDeathScript] Bound final game screen '" +
                finalGameScreen.name +
                "'.",
                this
            );
        }
    }

    private GameObject FindSceneObjectByNameAcrossLoadedScenes(
        string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            for (int r = 0; r < roots.Length; r++)
            {
                GameObject found =
                    FindChildObjectByName(roots[r].transform, objectName);

                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private GameObject FindChildObjectByName(
        Transform parent,
        string objectName)
    {
        if (parent.name == objectName)
        {
            return parent.gameObject;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found =
                FindChildObjectByName(parent.GetChild(i), objectName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void SubscribeToBossHealth()
    {
        if (bossHealth == null || subscribedToHealth)
        {
            return;
        }

        bossHealth.OnDied += HandleBossDeath;
        subscribedToHealth = true;

        // If the boss already died before we subscribed, fire immediately.
        if (bossHealth.IsDead())
        {
            HandleBossDeath();
        }
    }

    private void UnsubscribeFromBossHealth()
    {
        if (bossHealth == null || !subscribedToHealth)
        {
            return;
        }

        bossHealth.OnDied -= HandleBossDeath;
        subscribedToHealth = false;
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

        if (debugDeath)
        {
            Debug.Log(
                "[GargamelDeathScript] Final boss death sequence started.",
                this
            );
        }

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
            FinishEndingVideo(video);
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
        FinishEndingVideo(player);
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
        FinishEndingVideo(player);
    }

    private void FinishEndingVideo(VideoPlayer endingVideo)
    {
        if (endingVideo != null)
        {
            endingVideo.Stop();
            endingVideo.gameObject.SetActive(false);
        }

        ResolveFinalGameScreen();

        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[GargamelDeathScript] Ending finished, but no final game " +
                "screen could be shown. Assign Final Game Screen or set " +
                "Final Game Screen Object Name (currently '" +
                finalGameScreenObjectName +
                "').",
                this
            );
        }

        // GameFlowManager owns the persistent end-of-game transition.
        // It will open Credits and then return to MainMenu.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.FinalCutsceneFinished();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Credits
            );
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        ResolveFinalGameScreen();

        if (finalGameScreen != null)
        {
            finalGameScreen.SetActive(false);
        }

        if (bossHealth != null)
        {
            bossHealth.ResetBossHealth();
        }
    }

    public FinalBossHealth GetBossHealth()
    {
        ResolveBossHealth();
        return bossHealth;
    }
}
