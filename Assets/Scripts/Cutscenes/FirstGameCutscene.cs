using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Controls the first-game cinematic sequence.
///
/// Flow:
///
/// Intro
///     ↓
/// First-game backstory
///     ↓
/// Lobby
///
/// The backstory video is selected automatically
/// based on the player's selected gender.
/// </summary>
public class FirstGameCutscene : MonoBehaviour
{
    // =========================================================
    // INTRO
    // =========================================================

    [Header("Intro")]

    [SerializeField]
    private IntroCutscene introCutscene;


    // =========================================================
    // BACKSTORY VIDEOS
    // =========================================================

    [Header("First Job / Backstory")]

    [Tooltip("Backstory video used when the player selected Male.")]
    [SerializeField]
    private VideoPlayer maleBackstoryVideo;


    [Tooltip("Backstory video used when the player selected Female.")]
    [SerializeField]
    private VideoPlayer femaleBackstoryVideo;


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Settings")]

    [SerializeField]
    private bool playOnStart = false;


    [SerializeField]
    private bool debugCutscene = true;


    // =========================================================
    // STATE
    // =========================================================

    private bool sequenceRunning = false;

    private bool backstoryPlaying = false;

    private VideoPlayer currentBackstoryVideo;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        SetupVideo(maleBackstoryVideo);
        SetupVideo(femaleBackstoryVideo);
    }


    private void Start()
    {
        if (playOnStart)
        {
            PlayFirstGameSequence();
        }
    }


    // =========================================================
    // PUBLIC START
    // =========================================================

    /// <summary>
    /// Starts the first-game cinematic sequence.
    /// </summary>
    public void PlayFirstGameSequence()
    {
        if (sequenceRunning)
        {
            return;
        }

        sequenceRunning = true;

        DebugCutscene(
            "Starting first-game cinematic sequence."
        );

        SetCutsceneState();

        // -----------------------------------------------------
        // START INTRO
        // -----------------------------------------------------

        if (introCutscene == null)
        {
            Debug.LogError(
                "[FirstGameCutscene] IntroCutscene reference missing."
            );

            FinishIntroAndStartBackstory();

            return;
        }

        introCutscene.PlayIntroWithoutFlowCallback(
            OnIntroFinished
        );
    }


    // =========================================================
    // INTRO FINISHED
    // =========================================================

    private void OnIntroFinished()
    {
        DebugCutscene(
            "Intro finished. Starting first-job backstory."
        );

        PlayBackstory();
    }


    // =========================================================
    // BACKSTORY
    // =========================================================

    private void PlayBackstory()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[FirstGameCutscene] GameStateManager missing."
            );

            FinishSequence();

            return;
        }

        GameStateManager.PlayerGender gender =
            GameStateManager.Instance.GetPlayerGender();


        VideoPlayer selectedVideo = null;


        // -----------------------------------------------------
        // SELECT VIDEO BY GENDER
        // -----------------------------------------------------

        if (gender == GameStateManager.PlayerGender.Male)
        {
            selectedVideo = maleBackstoryVideo;

            DebugCutscene(
                "Player gender is Male. Playing male backstory."
            );
        }
        else if (
            gender == GameStateManager.PlayerGender.Female)
        {
            selectedVideo = femaleBackstoryVideo;

            DebugCutscene(
                "Player gender is Female. Playing female backstory."
            );
        }


        // -----------------------------------------------------
        // VALIDATE
        // -----------------------------------------------------

        if (selectedVideo == null)
        {
            Debug.LogError(
                "[FirstGameCutscene] No backstory video assigned " +
                "for selected gender."
            );

            FinishSequence();

            return;
        }


        // -----------------------------------------------------
        // PLAY
        // -----------------------------------------------------

        backstoryPlaying = true;

        selectedVideo.loopPointReached +=
            OnBackstoryFinished;

        selectedVideo.errorReceived +=
            OnBackstoryError;

        selectedVideo.prepareCompleted +=
            OnBackstoryPrepared;

        selectedVideo.frameReady +=
            OnBackstoryFirstFrameReady;

        currentBackstoryVideo = selectedVideo;

        selectedVideo.Stop();
        selectedVideo.Prepare();
    }


    // =========================================================
    // BACKSTORY PREPARED
    // =========================================================

    private void OnBackstoryPrepared(VideoPlayer player)
    {
        player.prepareCompleted -=
            OnBackstoryPrepared;

        DebugCutscene(
            "Backstory prepared. Playing."
        );

        player.Play();
    }


    // =========================================================
    // BACKSTORY FIRST FRAME
    // =========================================================

    private void OnBackstoryFirstFrameReady(
        VideoPlayer player,
        long frameIndex)
    {
        player.frameReady -=
            OnBackstoryFirstFrameReady;

        if (currentBackstoryVideo != player)
        {
            return;
        }

        DebugCutscene(
            "Backstory first frame is ready. Switching away from intro."
        );

        // The new video has a real frame ready. Only now remove the
        // finished intro video so the 3D world can never appear between them.
        if (introCutscene != null)
        {
            introCutscene.StopForTransition();
        }
    }


    // =========================================================
    // BACKSTORY FINISHED
    // =========================================================

    private void OnBackstoryFinished(VideoPlayer player)
    {
        player.loopPointReached -=
            OnBackstoryFinished;

        player.errorReceived -=
            OnBackstoryError;
        player.frameReady -=
            OnBackstoryFirstFrameReady;

        if (currentBackstoryVideo == player)
        {
            currentBackstoryVideo = null;
        }

        backstoryPlaying = false;

        DebugCutscene(
            "First-job backstory finished."
        );

        FinishSequence();
    }


    // =========================================================
    // BACKSTORY ERROR
    // =========================================================

    private void OnBackstoryError(
        VideoPlayer player,
        string message)
    {
        player.loopPointReached -=
            OnBackstoryFinished;

        player.errorReceived -=
            OnBackstoryError;
        player.frameReady -=
            OnBackstoryFirstFrameReady;

        if (currentBackstoryVideo == player)
        {
            currentBackstoryVideo = null;
        }

        Debug.LogError(
            "[FirstGameCutscene] Backstory video error: "
            + message
        );

        backstoryPlaying = false;

        FinishSequence();
    }


    // =========================================================
    // FALLBACK
    // =========================================================

    private void FinishIntroAndStartBackstory()
    {
        PlayBackstory();
    }


    // =========================================================
    // FINISH ENTIRE SEQUENCE
    // =========================================================

    private void FinishSequence()
    {
        if (!sequenceRunning)
        {
            return;
        }

        sequenceRunning = false;

        DebugCutscene(
            "First-game cinematic sequence finished."
        );


        // -----------------------------------------------------
        // STOP BACKSTORY VIDEOS
        // -----------------------------------------------------

        StopVideo(maleBackstoryVideo);
        StopVideo(femaleBackstoryVideo);


        // -----------------------------------------------------
        // RETURN TO LOBBY
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.IntroFinished();
        }
        else
        {
            Debug.LogError(
                "[FirstGameCutscene] GameFlowManager missing."
            );
        }
    }


    // =========================================================
    // VIDEO SETUP
    // =========================================================

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
        video.sendFrameReadyEvents = true;

        video.Stop();

        if (Camera.main != null)
        {
            video.renderMode =
                VideoRenderMode.CameraNearPlane;

            video.targetCamera =
                Camera.main;

            video.targetCameraAlpha = 1f;
        }
    }


    // =========================================================
    // STOP VIDEO
    // =========================================================

    private void StopVideo(VideoPlayer video)
    {
        if (video == null)
        {
            return;
        }

        video.Stop();

        video.loopPointReached -=
            OnBackstoryFinished;

        video.errorReceived -=
            OnBackstoryError;

        video.prepareCompleted -=
            OnBackstoryPrepared;
        video.frameReady -=
            OnBackstoryFirstFrameReady;
    }


    // =========================================================
    // STOP SEQUENCE
    // =========================================================

    public void StopSequence()
    {
        sequenceRunning = false;
        backstoryPlaying = false;
        currentBackstoryVideo = null;

        StopVideo(maleBackstoryVideo);
        StopVideo(femaleBackstoryVideo);

        if (introCutscene != null)
        {
            introCutscene.StopCutscene();
        }
    }


    // =========================================================
    // CUTSCENE STATE
    // =========================================================

    private void SetCutsceneState()
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
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugCutscene(string message)
    {
        if (!debugCutscene)
        {
            return;
        }

        Debug.Log(
            "[FirstGameCutscene] " + message
        );
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        StopVideo(maleBackstoryVideo);
        StopVideo(femaleBackstoryVideo);
    }
}