using UnityEngine;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Plays the Gacha secret-ending video from the VideoPlayer on this GameObject
/// (or a child), without loading a separate scene. Persistent interaction text
/// can be found by a configurable name if an Inspector reference is lost.
/// </summary>
public class GachaWinCutscene : MonoBehaviour
{
    [Header("Gacha win video")]
    [Tooltip("Optional. If empty, the script searches this object and its children for a VideoPlayer.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Optional full-screen/video presentation root. Leave empty if this GameObject itself is the video presentation.")]
    [SerializeField] private GameObject cutsceneScreen;
    [SerializeField] private bool openCreditsWhenFinished = false;
    [Header("Dragon Ball ending destination")]
    [Tooltip("Optional EndScreen root. If empty, searches loaded objects for exactly 'EndScreen'.") ]
    [SerializeField] private GameObject endScreen;
    [SerializeField] private string endScreenObjectName = "EndScreen";

    [Header("Persistent interaction prompt lookup")]
    [Tooltip("Optional direct reference. If missing, the script searches active/inactive loaded objects by the name below.")]
    [SerializeField] private GameObject interactionTextObject;
    [Tooltip("Exact GameObject name of the persistent interaction prompt. This is a string so it can be re-found after scene changes.")]
    [SerializeField] private string interactionTextObjectName = "InteractionText";
    [Tooltip("Also searches TMP text objects by this text-object name if the GameObject name search does not find a match.")]
    [SerializeField] private string interactionTMPObjectName = "InteractionText";

    private bool promptWasActive;
    private bool promptStateCaptured;
    private bool isPlaying;

    private void Awake()
    {
        ResolveVideoPlayer();
        ResolveInteractionText();
        ResolveEndScreen();
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        if (cutsceneScreen != null && cutsceneScreen != gameObject)
            cutsceneScreen.SetActive(false);
    }


    private void ResolveEndScreen()
    {
        if (endScreen != null) return;
        if (string.IsNullOrWhiteSpace(endScreenObjectName)) return;
        GameObject found = GameObject.Find(endScreenObjectName);
        if (found != null) { endScreen = found; return; }
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (item != null && item.gameObject.scene.IsValid() && item.name == endScreenObjectName)
            { endScreen = item.gameObject; return; }
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void ResolveVideoPlayer()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        if (videoPlayer == null)
            Debug.LogError("[GachaWinCutscene] No VideoPlayer found on this object or its children.", this);
    }

    private void ResolveInteractionText()
    {
        if (interactionTextObject != null) return;
        if (!string.IsNullOrWhiteSpace(interactionTextObjectName))
        {
            GameObject active = GameObject.Find(interactionTextObjectName);
            if (active != null) { interactionTextObject = active; return; }
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform item in all)
            {
                if (item == null || item.gameObject.scene.IsValid() == false) continue;
                if (item.name == interactionTextObjectName) { interactionTextObject = item.gameObject; return; }
            }
        }
        if (!string.IsNullOrWhiteSpace(interactionTMPObjectName))
        {
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (TMP_Text item in texts)
            {
                if (item == null || !item.gameObject.scene.IsValid()) continue;
                if (item.gameObject.name == interactionTMPObjectName || item.name == interactionTMPObjectName)
                { interactionTextObject = item.gameObject; return; }
            }
        }
    }

    /// <summary>Allows GachaManager to provide the VideoPlayer through its Inspector field.</summary>
    public void SetVideoPlayer(VideoPlayer assignedPlayer)
    {
        if (assignedPlayer == null) return;
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer = assignedPlayer;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    public void Play()
    {
        if (isPlaying) return;
        ResolveVideoPlayer();
        ResolveInteractionText();
        isPlaying = true;

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.UnlockSecretEnding2();
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetGameMode(GameStateManager.GameMode.Cutscene);
        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.SetState(PlayerStateManager.PlayerState.Cutscene);
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.SetGameplayHUDVisible(false);

        if (interactionTextObject != null)
        {
            promptWasActive = interactionTextObject.activeSelf;
            promptStateCaptured = true;
            interactionTextObject.SetActive(false);
        }
        if (cutsceneScreen != null && cutsceneScreen != gameObject)
            cutsceneScreen.SetActive(true);

        if (videoPlayer == null || videoPlayer.clip == null)
        {
            Debug.LogError("[GachaWinCutscene] Assign a VideoClip to the attached VideoPlayer before calling Play().", this);
            Finish();
            return;
        }
        videoPlayer.Stop();
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer source) => Finish();

    public void Finish()
    {
        if (!isPlaying && !promptStateCaptured) return;
        isPlaying = false;
        if (videoPlayer != null) videoPlayer.Stop();
        if (cutsceneScreen != null && cutsceneScreen != gameObject) cutsceneScreen.SetActive(false);
        if (promptStateCaptured && interactionTextObject != null)
            interactionTextObject.SetActive(promptWasActive);
        promptStateCaptured = false;
        ResolveEndScreen();
        if (endScreen != null)
        {
            endScreen.SetActive(true);
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.SetGameplayHUDVisible(false);
            if (GameStateManager.Instance != null) GameStateManager.Instance.SetGameMode(GameStateManager.GameMode.Cutscene);
            if (PlayerStateManager.Instance != null) PlayerStateManager.Instance.SetState(PlayerStateManager.PlayerState.Cutscene);
        }
        else if (openCreditsWhenFinished && GameFlowManager.Instance != null)
            GameFlowManager.Instance.OpenCredits();
        else
        {
            if (GameStateManager.Instance != null) GameStateManager.Instance.SetGameMode(GameStateManager.GameMode.Lobby);
            if (PlayerStateManager.Instance != null) PlayerStateManager.Instance.SetState(PlayerStateManager.PlayerState.AllMovement);
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }
    }
}
