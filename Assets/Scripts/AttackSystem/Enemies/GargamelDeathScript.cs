using System.Collections;
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

    [Header("Debug")]
    [SerializeField] private bool debugDeath = true;

    [Header("miniDolls")]
    [SerializeField] private GameObject [] miniDolls;

    private bool endingStarted;
    private bool subscribedToHealth;
    private VideoPlayer activeVideo;
    private GameObject finalGameScreen;
    private float playbackStartedAt;

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
            finalGameScreen = FindEndScreenInPersistentScene(targetName);
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
                GetHierarchyPath(finalGameScreen.transform) + "' in scene '" +
                finalGameScreen.scene.name +
                "'.",
                this
            );
        }
    }

    private GameObject FindEndScreenInPersistentScene(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate != null && candidate.name == objectName &&
                candidate.scene.IsValid() && candidate.scene.isLoaded &&
                candidate.scene.name == "DontDestroyOnLoad")
            {
                if (debugDeath)
                    Debug.Log("[GargamelDeathScript] Found persistent end screen at '" +
                        GetHierarchyPath(candidate.transform) + "'.", candidate);
                return candidate;
            }
        }
        return null;
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null) return "<missing>";
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
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
        Debug.Log("[GargamelDeathScript] FinalBossHealth.OnDied was received.", this);
        disableDolls();
        TriggerDeathSequence();
    }

    private void disableDolls()
    {

        foreach (GameObject miniDoll in miniDolls)
        {
            if (miniDoll != null)
            miniDoll.SetActive(false);
        }

    }

    /// <summary>
    /// Can also be called manually by another final-boss system.
    /// </summary>
    public void TriggerDeathSequence()
    {
        // A stale started flag must not strand a new run. Only suppress a
        // duplicate if playback is active or the end screen is already open.
        ResolveFinalGameScreen();
        if (endingStarted &&
            ((activeVideo != null && activeVideo.isPlaying) ||
             (finalGameScreen != null && finalGameScreen.activeInHierarchy)))
        {
            Debug.Log("[GargamelDeathScript] Ignoring duplicate death trigger; ending is already active or complete.", this);
            return;
        }

        endingStarted = true;
        Debug.Log("[GargamelDeathScript] Starting final boss death sequence.", this);

        // Death can happen while the pause UI is open. Clear it and restore
        // scaled time before starting the VideoPlayer and ending UI.
        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.ContinueButton();
        }
        Time.timeScale = 1f;
        Debug.Log("[GargamelDeathScript] Pause state cleared; timeScale=" + Time.timeScale + ".", this);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
            Debug.Log("[GargamelDeathScript] Stopped background music for the final boss ending.", this);
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.DefeatFinalBoss();
        }

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

        Debug.Log("[GargamelDeathScript] Ending playback request dispatched.", this);
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
        playbackStartedAt = Time.realtimeSinceStartup;
        video.Prepare();
        StartCoroutine(WatchEndingVideoCompletion(video));
        if (video.isPrepared && !video.isPlaying)
        {
            Debug.Log("[GargamelDeathScript] Clip was already prepared; starting playback directly.", video);
            playbackStartedAt = Time.realtimeSinceStartup;
            video.Play();
        }
    }

    private IEnumerator WatchEndingVideoCompletion(VideoPlayer video)
    {
        float prepareDeadline = Time.realtimeSinceStartup + 20f;
        while (activeVideo == video && !video.isPrepared && !video.isPlaying &&
               Time.realtimeSinceStartup < prepareDeadline)
        {
            yield return null;
        }

        if (activeVideo != video)
            yield break;

        if (!video.isPrepared && !video.isPlaying)
        {
            Debug.LogError("[GargamelDeathScript] Ending video did not prepare within 20 seconds; moving to the end screen.", video);
            UnsubscribeFromVideo(video);
            activeVideo = null;
            FinishEndingVideo(video);
            yield break;
        }

        while (activeVideo == video)
        {
            double expectedLength = video.length > 0d
                ? video.length
                : (video.clip != null ? video.clip.length : 0d);
            if (expectedLength > 0d &&
                Time.realtimeSinceStartup >= playbackStartedAt + expectedLength + 3d)
            {
                Debug.LogWarning("[GargamelDeathScript] Ending video exceeded its expected duration; forcing completion. time=" +
                    video.time + "/" + expectedLength + ", isPlaying=" + video.isPlaying + ".", video);
                OnVideoFinished(video);
                yield break;
            }

            bool finalFrameReached =
                video.frameCount > 0 &&
                video.frameCount != ulong.MaxValue &&
                video.frame >= 0 &&
                (ulong)video.frame >= video.frameCount - 1UL;
            bool playbackTimeReachedEnd =
                expectedLength > 0d && video.time >= expectedLength - 0.15d;

            if (video.isPrepared && (finalFrameReached || playbackTimeReachedEnd))
            {
                Debug.LogWarning("[GargamelDeathScript] Video reached its final frame/time (isPlaying=" +
                    video.isPlaying + ") without a loopPointReached callback; completing the ending. frame=" +
                    video.frame + "/" + video.frameCount + ", time=" + video.time + "/" + video.length + ".", video);
                OnVideoFinished(video);
                yield break;
            }
            yield return null;
        }
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (player != activeVideo)
        {
            return;
        }

        Debug.Log("[GargamelDeathScript] Ending clip prepared; starting playback.", player);
        playbackStartedAt = Time.realtimeSinceStartup;
        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        if (player != activeVideo)
        {
            return;
        }

        Debug.Log("[GargamelDeathScript] Ending clip reached its end.", player);
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusicImmediate();
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

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusicImmediate();

        UnsubscribeFromVideo(player);
        activeVideo = null;
        FinishEndingVideo(player);
    }

    private void FinishEndingVideo(VideoPlayer endingVideo)
    {
        if (finalGameScreen != null && finalGameScreen.activeInHierarchy)
        {
            Debug.Log("[GargamelDeathScript] End screen is already active; duplicate finish call ignored.", this);
            return;
        }

        Debug.Log("[GargamelDeathScript] FinishEndingVideo entered. Video='" +
            (endingVideo != null ? endingVideo.gameObject.name : "<none>") + "'.", this);
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
            StartCoroutine(ShowFinalGameScreenAndVerify());
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

    private void ShowFinalGameScreen()
    {
        Transform current = finalGameScreen.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                Debug.LogWarning("[GargamelDeathScript] Enabling inactive end screen hierarchy object '" +
                    current.name + "'.", current);
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
        finalGameScreen.SetActive(true);

        Canvas[] canvases = finalGameScreen.GetComponentsInParent<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && !canvas.enabled)
            {
                Debug.LogWarning("[GargamelDeathScript] Enabling disabled Canvas '" +
                    GetHierarchyPath(canvas.transform) + "' for the end screen.", canvas);
                canvas.enabled = true;
            }
        }

        CanvasGroup[] groups = finalGameScreen.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup group in groups)
        {
            if (group != null && group.alpha < 1f)
            {
                Debug.LogWarning("[GargamelDeathScript] Restoring CanvasGroup alpha on '" +
                    GetHierarchyPath(group.transform) + "' for the end screen.", group);
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator ShowFinalGameScreenAndVerify()
    {
        ShowFinalGameScreen();
        yield return null;
        Canvas.ForceUpdateCanvases();

        Canvas canvas = finalGameScreen != null
            ? finalGameScreen.GetComponentInParent<Canvas>(true)
            : null;
        bool visibleInHierarchy = finalGameScreen != null && finalGameScreen.activeInHierarchy;
        bool canvasEnabled = canvas != null && canvas.enabled;

        Debug.Log("[GargamelDeathScript] End screen verification: path='" +
            (finalGameScreen != null ? GetHierarchyPath(finalGameScreen.transform) : "<missing>") +
            "', activeSelf=" + (finalGameScreen != null && finalGameScreen.activeSelf) +
            ", activeInHierarchy=" + visibleInHierarchy +
            ", canvas=" + (canvas != null ? canvas.name : "<missing>") +
            ", canvasEnabled=" + canvasEnabled + ".", this);

        if (!visibleInHierarchy || !canvasEnabled)
        {
            Debug.LogError("[GargamelDeathScript] End screen is still not renderable after activation.", this);
        }
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
