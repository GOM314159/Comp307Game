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
    [Tooltip("Scene to search first. If the UI has moved to DontDestroyOnLoad, name lookup falls back to all loaded scenes.")]
    [SerializeField] private string finalGameScreenSceneName = "GamePlayTest";
    [Tooltip(
        "Exact GameObject name of the final end screen UI."
    )]
    [SerializeField] private string finalGameScreenObjectName = "EndScreen";

    [Header("Settings")]
    [SerializeField] private bool playOnDeath = true;

    [Header("Debug")]
    [SerializeField] private bool debugDeath = true;

    private bool endingStarted;
    private bool subscribedToHealth;
    private VideoPlayer activeVideo;
    private GameObject finalGameScreen;

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

        if (!string.IsNullOrWhiteSpace(finalGameScreenSceneName))
        {
            Scene preferredScene = SceneManager.GetSceneByName(finalGameScreenSceneName.Trim());
            if (preferredScene.IsValid() && preferredScene.isLoaded)
            {
                finalGameScreen = FindSceneObjectByName(preferredScene, targetName);
                if (debugDeath)
                {
                    Debug.Log("[GargamelDeathScript] Searched scene '" + preferredScene.name +
                        "' for end screen '" + targetName + "': " + (finalGameScreen != null ? "found" : "not found") + ".", this);
                }
            }
            else if (debugDeath)
            {
                Debug.LogWarning("[GargamelDeathScript] Configured end screen scene '" +
                    finalGameScreenSceneName + "' is not loaded. Will search all loaded scenes.", this);
            }
        }

        if (finalGameScreen == null)
        {
            finalGameScreen = FindSceneObjectByNameAcrossLoadedScenes(targetName);
        }

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

    private GameObject FindSceneObjectByNameAcrossLoadedScenes(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject found = FindSceneObjectByName(scene, objectName);
            if (found != null) return found;
        }

        return null;
    }

    private GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject found = FindChildObjectByName(roots[i].transform, objectName);
            if (found != null) return found;
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

        Debug.Log("[GargamelDeathScript] Boss death received. Playing " +
            (secretEnding ? "Exodia" : "normal") + " ending.", this);

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
            Debug.LogError("[GargamelDeathScript] The selected ending VideoPlayer is not assigned. Continuing to the end screen.", this);
            FinishEndingVideo(video);
            return;
        }

        if (video.clip == null)
        {
            Debug.LogError("[GargamelDeathScript] Ending VideoPlayer '" + video.gameObject.name +
                "' has no VideoClip. Continuing to the end screen.", video);
            FinishEndingVideo(video);
            return;
        }

        activeVideo = video;
        video.gameObject.SetActive(true);
        video.enabled = true;
        video.playOnAwake = false;
        video.isLooping = false;
        video.renderMode = VideoRenderMode.CameraNearPlane;
        video.targetCameraAlpha = 1f;
        if (Camera.main != null)
            video.targetCamera = Camera.main;
        SubscribeToVideo(video);
        video.Stop();
        Debug.Log("[GargamelDeathScript] Preparing ending clip '" + video.clip.name +
            "' on active player '" + video.gameObject.name + "'.", video);
        video.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (player != activeVideo)
        {
            return;
        }

        Debug.Log("[GargamelDeathScript] Ending clip prepared; starting playback.", player);
        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        if (player != activeVideo)
        {
            return;
        }

        Debug.Log("[GargamelDeathScript] Ending clip reached its end.", player);
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
        HideEndingVideo(normalEndingVideo);
        HideEndingVideo(secretEndingVideo);

        Debug.Log("[GargamelDeathScript] Ending video finished. Video players hidden; resolving end screen.", this);

        ResolveFinalGameScreen();

        // GameFlowManager owns the persistent end-of-game transition.
        // Completing the cutscene marks the run complete, but switches to
        // Credits mode. The interactive end screen belongs to the lobby UI,
        // so restore that mode before activating it.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.FinalCutsceneFinished();
        }
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(GameStateManager.GameMode.Lobby);
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(PlayerStateManager.PlayerState.Cutscene);
        }

        if (finalGameScreen != null)
        {
            // Let mode-change listeners finish updating canvases before
            // showing this UI. Some of them hide gameplay presentation roots.
            finalGameScreen.SetActive(true);
            Debug.Log("[GargamelDeathScript] End screen activation requested. activeSelf=" +
                finalGameScreen.activeSelf + ", activeInHierarchy=" + finalGameScreen.activeInHierarchy +
                ", parent=" + (finalGameScreen.transform.parent != null ? finalGameScreen.transform.parent.name : "<root>"), this);
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HideEndingVideo(VideoPlayer video)
    {
        if (video == null)
        {
            Debug.LogWarning("[GargamelDeathScript] One ending VideoPlayer reference is missing.", this);
            return;
        }

        video.Stop();
        video.targetCameraAlpha = 0f;
        video.renderMode = VideoRenderMode.APIOnly;
        video.enabled = false;
        video.gameObject.SetActive(false);
        Debug.Log("[GargamelDeathScript] Hid ending video '" + video.gameObject.name +
            "': activeSelf=" + video.gameObject.activeSelf + ", enabled=" + video.enabled +
            ", cameraAlpha=" + video.targetCameraAlpha, video);
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
            PrepareVideoForReplay(normalEndingVideo);
        }

        if (secretEndingVideo != null)
        {
            secretEndingVideo.Stop();
            PrepareVideoForReplay(secretEndingVideo);
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

    private static void PrepareVideoForReplay(VideoPlayer video)
    {
        video.gameObject.SetActive(true);
        video.enabled = true;
        video.targetCameraAlpha = 1f;
        video.renderMode = VideoRenderMode.CameraNearPlane;
        if (Camera.main != null)
            video.targetCamera = Camera.main;
    }
}
