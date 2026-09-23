using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;


public class Chair : WorldInteractable
{
    // =========================================================
    // CHAIR SETTINGS
    // =========================================================

    [Header("Chair")]

    [Tooltip(
        "The position where the player will be placed when sitting."
    )]
    [SerializeField]
    private Transform playerSeatPosition;

    [Tooltip(
        "Optional. Determines which direction the player faces."
    )]
    [SerializeField]
    private Transform playerFacingDirection;


    // =========================================================
    // DAILY THEATER PLAYS
    // =========================================================

    [Header("Daily Theater Plays")]

    [Tooltip(
        "One VideoPlayer per day.\n" +
        "Element 0 = Day 1\n" +
        "Element 1 = Day 2\n" +
        "Element 2 = Day 3\n" +
        "Element 3 = Day 4"
    )]
    [SerializeField]
    private List<VideoPlayer> dailyPlays =
        new List<VideoPlayer>();


    // =========================================================
    // FADE
    // =========================================================

    [Header("Play Fade")]

    [Tooltip(
        "CanvasGroup used as a full-screen black fade overlay."
    )]
    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [Tooltip(
        "How long the screen takes to fade to black before " +
        "the video starts."
    )]
    [SerializeField]
    private float fadeOutDuration = 0.75f;

    [Tooltip(
        "How long the screen takes to fade from black after " +
        "the video begins."
    )]
    [SerializeField]
    private float fadeInDuration = 0.75f;

    [Tooltip(
        "If true, the video begins while the screen is black " +
        "and then fades into the video."
    )]
    [SerializeField]
    private bool fadeIntoVideo = true;


    // =========================================================
    // PLAYBACK SETTINGS
    // =========================================================

    [Header("Playback Settings")]

    [Tooltip(
        "Game mode used while the player watches the play."
    )]
    [SerializeField]
    private GameStateManager.GameMode watchingGameMode =
        GameStateManager.GameMode.WatchingPlay;

    [Tooltip(
        "Player state used while watching."
    )]
    [SerializeField]
    private PlayerStateManager.PlayerState watchingPlayerState =
        PlayerStateManager.PlayerState.Cutscene;


    // =========================================================
    // VIDEO SETTINGS
    // =========================================================

    [Header("Video Settings")]

    [Tooltip(
        "If enabled, videos automatically render to the Main Camera."
    )]
    [SerializeField]
    private bool renderVideosToMainCamera = true;

    [Tooltip(
        "If enabled, video audio is played through the VideoPlayer."
    )]
    [SerializeField]
    private bool useVideoAudio = true;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugChair = true;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    // ---------------------------------------------------------
    // PLAY TRACKING
    // ---------------------------------------------------------

    // Index 0 = Day 1
    // Index 1 = Day 2
    // Index 2 = Day 3
    // Index 3 = Day 4

    private bool[] playsWatched;


    // ---------------------------------------------------------
    // CURRENT PLAY
    // ---------------------------------------------------------

    private bool isWatchingPlay = false;

    private int currentPlayDay = -1;

    private VideoPlayer currentVideoPlayer;


    // ---------------------------------------------------------
    // DAY TRACKING
    // ---------------------------------------------------------

    private int lastCheckedDay = -1;


    // ---------------------------------------------------------
    // CHAIR INTERACTION SUPPRESSION
    // ---------------------------------------------------------

    private bool interactionSuppressed = false;


    // ---------------------------------------------------------
    // PLAYER
    // ---------------------------------------------------------

    private GameObject player;

    private Vector3 lockedPlayerPosition;

    private Quaternion lockedPlayerRotation;


    // ---------------------------------------------------------
    // PREVIOUS STATES
    // ---------------------------------------------------------

    private GameStateManager.GameMode previousGameMode;

    private PlayerStateManager.PlayerState previousPlayerState;


    // ---------------------------------------------------------
    // COROUTINE
    // ---------------------------------------------------------

    private Coroutine playSequenceCoroutine;


    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        InitializePlayTracking();

        SetupFade();

        SetupVideoPlayers();
    }


    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss +=
                HandleGameLoss;

            GameStateManager.Instance.OnGameModeChanged +=
                HandleGameModeChanged;

            // If the chair exists while already in the Main Menu,
            // make sure it starts fresh.
            if (GameStateManager.Instance.GetGameMode() ==
                GameStateManager.GameMode.MainMenu)
            {
                ResetPlayTracking();
                RestoreChairInteraction();
            }
        }
    }


    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -=
                HandleGameLoss;

            GameStateManager.Instance.OnGameModeChanged -=
                HandleGameModeChanged;
        }

        // Always hide the chair prompt when disabled.
        HideInteractionPrompt();

        StopCurrentPlayIfNecessary();
    }

    // =========================================================
    // GAME MODE RESET
    // =========================================================

    private void HandleGameModeChanged(
        GameStateManager.GameMode newGameMode
    )
    {
        // A completely new game returns through the Main Menu.
        //
        // This clears the chair's local watched-play tracking so
        // starting a new game does NOT inherit plays watched during
        // the previous game.
        if (newGameMode ==
            GameStateManager.GameMode.MainMenu)
        {
            DebugChair(
                "Main Menu reached. Resetting theater play tracking."
            );

            StopCurrentPlayIfNecessary();

            ResetPlayTracking();

            RestoreChairInteraction();
        }
    }


    private void Update()
    {
        // =====================================================
        // WATCHING PLAY
        // =====================================================

        if (isWatchingPlay)
        {
            LockPlayerTransform();

            // The chair must remain unavailable.
            HideInteractionPrompt();

            return;
        }


        // =====================================================
        // INTERACTION SUPPRESSED
        // =====================================================

        if (interactionSuppressed)
        {
            HideInteractionPrompt();

            return;
        }


        // =====================================================
        // GAME STATE
        // =====================================================

        if (GameStateManager.Instance == null)
        {
            HideInteractionPrompt();

            return;
        }


        int currentDay =
            GameStateManager.Instance.GetCurrentDay();

        int dayIndex =
            currentDay - 1;


        if (dayIndex < 0)
        {
            HideInteractionPrompt();

            return;
        }


        EnsureDayTrackingExists(dayIndex);


        // =====================================================
        // DETECT DAY CHANGE
        // =====================================================

        if (currentDay != lastCheckedDay)
        {
            lastCheckedDay = currentDay;

            bool available =
                !playsWatched[dayIndex];

            if (available)
            {
                SetInteractionEnabled(true);

                
            }
            else
            {
                SetInteractionEnabled(false);

                HideInteractionPrompt();
            }
        }


        // =====================================================
        // IMPORTANT
        // =====================================================
        //
        // Do NOT continuously call ShowInteractionPrompt()
        // here.
        //
        // PlayerInteraction handles the prompt every frame.
        //
        // This prevents the Chair from fighting with
        // PlayerInteraction over prompt visibility.
        //
        // =====================================================
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializePlayTracking()
    {
        int numberOfPlays =
            Mathf.Max(
                1,
                dailyPlays.Count
            );

        playsWatched =
            new bool[numberOfPlays];
    }


    private void SetupFade()
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = 0f;

        fadeCanvasGroup.blocksRaycasts = false;

        fadeCanvasGroup.interactable = false;
    }


    private void SetupVideoPlayers()
    {
        for (
            int i = 0;
            i < dailyPlays.Count;
            i++
        )
        {
            VideoPlayer video =
                dailyPlays[i];

            if (video == null)
            {
                continue;
            }


            video.playOnAwake = false;

            video.isLooping = false;


            if (useVideoAudio)
            {
                video.audioOutputMode =
                    VideoAudioOutputMode.Direct;

                video.EnableAudioTrack(
                    0,
                    true
                );
            }


            if (renderVideosToMainCamera)
            {
                SetupVideoCamera(video);
            }
        }
    }


    private void SetupVideoCamera(
        VideoPlayer video
    )
    {
        if (Camera.main == null)
        {
            DebugChair(
                "Main Camera not found while setting up video."
            );

            return;
        }


        video.renderMode =
            VideoRenderMode.CameraNearPlane;

        video.targetCamera =
            Camera.main;

        video.targetCameraAlpha = 1f;
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact()
    {
        // -----------------------------------------------------
        // BASIC CHECK
        // -----------------------------------------------------

        if (!CanInteract())
        {
            return;
        }


        if (interactionSuppressed)
        {
            return;
        }


        if (isWatchingPlay)
        {
            return;
        }


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Chair] GameStateManager is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance == null)
        {
            Debug.LogError(
                "[Chair] PlayerStateManager is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // CURRENT DAY
        // -----------------------------------------------------

        int currentDay =
            GameStateManager.Instance.GetCurrentDay();

        int dayIndex =
            currentDay - 1;


        if (dayIndex < 0)
        {
            Debug.LogError(
                "[Chair] Invalid day: "
                + currentDay
            );

            return;
        }


        // -----------------------------------------------------
        // ENSURE TRACKING
        // -----------------------------------------------------

        EnsureDayTrackingExists(dayIndex);


        // -----------------------------------------------------
        // ALREADY WATCHED?
        // -----------------------------------------------------

        if (playsWatched[dayIndex])
        {
            DebugChair(
                "Day "
                + currentDay
                + " play has already been watched."
            );

            SuppressChairInteraction();

            return;
        }


        // -----------------------------------------------------
        // GET VIDEO
        // -----------------------------------------------------

        VideoPlayer play =
            GetPlayForDay(dayIndex);


        if (play == null)
        {
            Debug.LogWarning(
                "[Chair] No theater video assigned for Day "
                + currentDay
                + "."
            );

            return;
        }


        // -----------------------------------------------------
        // START
        // -----------------------------------------------------

        StartPlay(
            currentDay,
            play
        );
    }


    // =========================================================
    // GET PLAY
    // =========================================================

    private VideoPlayer GetPlayForDay(
        int dayIndex
    )
    {
        if (dayIndex < 0)
        {
            return null;
        }


        if (dayIndex >= dailyPlays.Count)
        {
            return null;
        }


        return dailyPlays[dayIndex];
    }


    // =========================================================
    // START PLAY
    // =========================================================

    private void StartPlay(
        int day,
        VideoPlayer play
    )
    {
        // -----------------------------------------------------
        // FIND PLAYER
        // -----------------------------------------------------

        player =
            FindAnyObjectByType<PlayerStateManager>()?
            .gameObject;


        if (player == null)
        {
            Debug.LogError(
                "[Chair] Could not find player."
            );

            return;
        }


        PlayerStateManager playerState =
            PlayerStateManager.Instance;


        if (playerState == null)
        {
            Debug.LogError(
                "[Chair] PlayerStateManager.Instance is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // SAVE STATES
        // -----------------------------------------------------

        previousGameMode =
            GameStateManager.Instance.GetGameMode();

        previousPlayerState =
            playerState.GetCurrentState();


        // -----------------------------------------------------
        // SUPPRESS CHAIR IMMEDIATELY
        // -----------------------------------------------------
        //
        // This is the important fix.
        //
        // PlayerInteraction runs every frame, so merely hiding
        // the prompt is not enough.
        //
        // We disable interaction FIRST.
        //
        // -----------------------------------------------------

        SuppressChairInteraction();


        // -----------------------------------------------------
        // POSITION PLAYER
        // -----------------------------------------------------

        MovePlayerIntoSeat();

        LockPlayerTransform();


        // -----------------------------------------------------
        // INTERNAL STATE
        // -----------------------------------------------------

        isWatchingPlay = true;

        currentPlayDay = day;

        currentVideoPlayer = play;


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        GameStateManager.Instance.SetCurrentPlay(day);

        GameStateManager.Instance.SetGameMode(
            watchingGameMode
        );


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        playerState.SetState(
            watchingPlayerState
        );


        // -----------------------------------------------------
        // HIDE HUD
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                false
            );
        }


        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        // -----------------------------------------------------
        // START COROUTINE
        // -----------------------------------------------------

        if (playSequenceCoroutine != null)
        {
            StopCoroutine(
                playSequenceCoroutine
            );
        }


        playSequenceCoroutine =
            StartCoroutine(
                PlayVideoSequence(
                    day,
                    play
                )
            );


        DebugChair(
            "Starting Day "
            + day
            + " theater play."
        );
    }


    // =========================================================
    // SUPPRESS CHAIR INTERACTION
    // =========================================================

    private void SuppressChairInteraction()
    {
        interactionSuppressed = true;

        SetInteractionEnabled(false);

        HideInteractionPrompt();


        DebugChair(
            "Chair interaction suppressed."
        );
    }


    // =========================================================
    // RESTORE CHAIR INTERACTION
    // =========================================================

    private void RestoreChairInteraction()
    {
        interactionSuppressed = false;

        HideInteractionPrompt();

        // Force the next Update to reassess the current day.
        lastCheckedDay = -1;


        DebugChair(
            "Chair interaction restored."
        );
    }


    // =========================================================
    // VIDEO SEQUENCE
    // =========================================================

    private System.Collections.IEnumerator PlayVideoSequence(
        int day,
        VideoPlayer play
    )
    {
        // -----------------------------------------------------
        // PREPARE
        // -----------------------------------------------------

        if (play == null)
        {
            FinishPlayWithoutVideo();

            yield break;
        }


        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        if (renderVideosToMainCamera)
        {
            SetupVideoCamera(play);
        }


        // -----------------------------------------------------
        // STOP OLD PLAY
        // -----------------------------------------------------

        play.Stop();


        // -----------------------------------------------------
        // FADE TO BLACK
        // -----------------------------------------------------

        if (fadeCanvasGroup != null)
        {
            yield return FadeCanvas(
                0f,
                1f,
                fadeOutDuration
            );
        }


        // -----------------------------------------------------
        // PREPARE VIDEO
        // -----------------------------------------------------

        bool prepared = false;

        bool preparationFailed = false;


        void HandlePrepared(
            VideoPlayer source
        )
        {
            prepared = true;
        }


        void HandleError(
            VideoPlayer source,
            string message
        )
        {
            preparationFailed = true;

            Debug.LogError(
                "[Chair] Video preparation error: "
                + message
            );
        }


        play.prepareCompleted +=
            HandlePrepared;

        play.errorReceived +=
            HandleError;


        play.Prepare();


        // -----------------------------------------------------
        // WAIT FOR PREPARATION
        // -----------------------------------------------------

        while (
            !prepared &&
            !preparationFailed
        )
        {
            yield return null;
        }


        play.prepareCompleted -=
            HandlePrepared;

        play.errorReceived -=
            HandleError;


        // -----------------------------------------------------
        // PREPARATION FAILED
        // -----------------------------------------------------

        if (preparationFailed)
        {
            Debug.LogError(
                "[Chair] Could not prepare Day "
                + day
                + " theater video."
            );

            FinishPlayWithoutVideo();

            yield break;
        }


        // -----------------------------------------------------
        // PLAY VIDEO
        // -----------------------------------------------------

        bool videoFinished = false;

        bool videoErrored = false;


        void HandleFinished(
            VideoPlayer source
        )
        {
            videoFinished = true;
        }


        void HandleVideoError(
            VideoPlayer source,
            string message
        )
        {
            videoErrored = true;

            Debug.LogError(
                "[Chair] Theater video error: "
                + message
            );
        }


        play.loopPointReached +=
            HandleFinished;

        play.errorReceived +=
            HandleVideoError;


        play.Play();


        // -----------------------------------------------------
        // MARK AS WATCHED
        //
        // Only mark it watched once playback actually begins.
        // -----------------------------------------------------

        int index =
            day - 1;


        EnsureDayTrackingExists(index);


        playsWatched[index] = true;


        SetInteractionEnabled(false);

        HideInteractionPrompt();


        DebugChair(
            "Day "
            + day
            + " theater video started."
        );


        // -----------------------------------------------------
        // FADE INTO VIDEO
        // -----------------------------------------------------

        if (
            fadeCanvasGroup != null &&
            fadeIntoVideo
        )
        {
            yield return FadeCanvas(
                1f,
                0f,
                fadeInDuration
            );
        }


        // -----------------------------------------------------
        // WAIT FOR VIDEO
        // -----------------------------------------------------

        while (
            !videoFinished &&
            !videoErrored &&
            isWatchingPlay
        )
        {
            LockPlayerTransform();

            // Keep prompt hidden during the entire play.
            HideInteractionPrompt();

            yield return null;
        }


        // -----------------------------------------------------
        // CLEAN EVENTS
        // -----------------------------------------------------

        play.loopPointReached -=
            HandleFinished;

        play.errorReceived -=
            HandleVideoError;


        // -----------------------------------------------------
        // VIDEO ERROR
        // -----------------------------------------------------

        if (videoErrored)
        {
            FinishPlayWithoutVideo();

            yield break;
        }


        // -----------------------------------------------------
        // VIDEO FINISHED
        // -----------------------------------------------------

        if (isWatchingPlay)
        {
            yield return FinishPlaySequence();
        }
    }


    // =========================================================
    // FADE
    // =========================================================

    private System.Collections.IEnumerator FadeCanvas(
        float from,
        float to,
        float duration
    )
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }


        if (duration <= 0f)
        {
            fadeCanvasGroup.alpha = to;

            yield break;
        }


        fadeCanvasGroup.blocksRaycasts = true;


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );


            fadeCanvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    progress
                );


            yield return null;
        }


        fadeCanvasGroup.alpha = to;


        if (Mathf.Approximately(to, 0f))
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }


    // =========================================================
    // FINISH PLAY
    // =========================================================

    private System.Collections.IEnumerator FinishPlaySequence()
    {
        DebugChair(
            "Day "
            + currentPlayDay
            + " theater play finished."
        );


        // -----------------------------------------------------
        // FADE TO BLACK
        // -----------------------------------------------------

        if (fadeCanvasGroup != null)
        {
            yield return FadeCanvas(
                fadeCanvasGroup.alpha,
                1f,
                fadeOutDuration
            );
        }


        // -----------------------------------------------------
        // STOP VIDEO
        // -----------------------------------------------------

        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
        }


        // -----------------------------------------------------
        // RESTORE STATES
        // -----------------------------------------------------

        RestoreAfterPlay();


        // -----------------------------------------------------
        // FADE BACK TO GAME
        // -----------------------------------------------------

        if (fadeCanvasGroup != null)
        {
            yield return FadeCanvas(
                1f,
                0f,
                fadeInDuration
            );
        }


        playSequenceCoroutine = null;
    }


    // =========================================================
    // FINISH WITHOUT VIDEO
    // =========================================================

    private void FinishPlayWithoutVideo()
    {
        DebugChair(
            "Theater play ended without successful playback."
        );


        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
        }


        // -----------------------------------------------------
        // IMPORTANT:
        // Do NOT mark a failed video as watched.
        // -----------------------------------------------------

        if (currentPlayDay > 0)
        {
            int index =
                currentPlayDay - 1;


            EnsureDayTrackingExists(index);


            playsWatched[index] = false;
        }


        RestoreAfterPlay();


        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;

            fadeCanvasGroup.blocksRaycasts = false;
        }


        playSequenceCoroutine = null;
    }


    // =========================================================
    // RESTORE AFTER PLAY
    // =========================================================

    private void RestoreAfterPlay()
    {
        isWatchingPlay = false;


        // -----------------------------------------------------
        // RESTORE CHAIR STATE
        // -----------------------------------------------------

        RestoreChairInteraction();


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                previousPlayerState
            );
        }


        // -----------------------------------------------------
        // GAME MODE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                previousGameMode
            );
        }


        // -----------------------------------------------------
        // HUD
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                true
            );
        }


        // -----------------------------------------------------
        // RESET REFERENCES
        // -----------------------------------------------------

        currentVideoPlayer = null;

        currentPlayDay = -1;

        player = null;


        // -----------------------------------------------------
        // IMPORTANT
        // -----------------------------------------------------
        //
        // The play has now been watched.
        //
        // Therefore the chair should remain disabled.
        //
        // Do NOT simply call SetInteractionEnabled(true).
        //
        // Update() will see that playsWatched[currentDay]
        // is true and keep the chair disabled.
        //
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            int currentDay =
                GameStateManager.Instance.GetCurrentDay();

            int index =
                currentDay - 1;


            if (index >= 0)
            {
                EnsureDayTrackingExists(index);


                if (playsWatched[index])
                {
                    SetInteractionEnabled(false);

                    HideInteractionPrompt();
                }
            }
        }
    }


    // =========================================================
    // POSITION PLAYER
    // =========================================================

    private void MovePlayerIntoSeat()
    {
        if (player == null)
        {
            return;
        }


        if (playerSeatPosition != null)
        {
            player.transform.position =
                playerSeatPosition.position;
        }


        if (playerFacingDirection != null)
        {
            player.transform.rotation =
                playerFacingDirection.rotation;
        }


        lockedPlayerPosition =
            player.transform.position;

        lockedPlayerRotation =
            player.transform.rotation;
    }


    // =========================================================
    // LOCK PLAYER
    // =========================================================

    private void LockPlayerTransform()
    {
        if (player == null)
        {
            return;
        }


        player.transform.position =
            lockedPlayerPosition;

        player.transform.rotation =
            lockedPlayerRotation;
    }


    // =========================================================
    // DEATH / LOSS
    // =========================================================

    private void HandleGameLoss(
        GameStateManager.LossReason reason
    )
    {
        if (
            reason !=
            GameStateManager.LossReason.PlayerDeath
        )
        {
            return;
        }


        DebugChair(
            "Player died. Resetting theater play availability."
        );


        // -----------------------------------------------------
        // STOP CURRENT VIDEO
        // -----------------------------------------------------

        StopCurrentPlayIfNecessary();


        // -----------------------------------------------------
        // RESET PLAY AVAILABILITY
        // -----------------------------------------------------

        ResetPlayTracking();


        // -----------------------------------------------------
        // RESTORE CHAIR INTERACTION
        // -----------------------------------------------------

        RestoreChairInteraction();
    }


    // =========================================================
    // STOP CURRENT PLAY
    // =========================================================

    private void StopCurrentPlayIfNecessary()
    {
        if (playSequenceCoroutine != null)
        {
            StopCoroutine(
                playSequenceCoroutine
            );

            playSequenceCoroutine = null;
        }


        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
        }


        isWatchingPlay = false;

        currentVideoPlayer = null;

        currentPlayDay = -1;


        // -----------------------------------------------------
        // MAKE SURE THE FADE DOES NOT REMAIN BLACK
        // -----------------------------------------------------

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;

            fadeCanvasGroup.blocksRaycasts = false;
        }


        // -----------------------------------------------------
        // ALWAYS HIDE PROMPT
        // -----------------------------------------------------

        HideInteractionPrompt();
    }


    // =========================================================
    // RESET PLAY TRACKING
    // =========================================================

    private void ResetPlayTracking()
    {
        if (playsWatched == null)
        {
            InitializePlayTracking();

            return;
        }


        for (
            int i = 0;
            i < playsWatched.Length;
            i++
        )
        {
            playsWatched[i] = false;
        }


        currentPlayDay = -1;

        isWatchingPlay = false;

        currentVideoPlayer = null;


        // Allow interaction again after reset.
        interactionSuppressed = false;


        // Force Update() to reassess the day.
        lastCheckedDay = -1;
    }


    // =========================================================
    // ENSURE DAY EXISTS
    // =========================================================

    private void EnsureDayTrackingExists(
        int dayIndex
    )
    {
        if (dayIndex < 0)
        {
            return;
        }


        if (playsWatched == null)
        {
            playsWatched =
                new bool[dayIndex + 1];

            return;
        }


        if (dayIndex < playsWatched.Length)
        {
            return;
        }


        bool[] newTracking =
            new bool[dayIndex + 1];


        for (
            int i = 0;
            i < playsWatched.Length;
            i++
        )
        {
            newTracking[i] =
                playsWatched[i];
        }


        playsWatched =
            newTracking;
    }

    // =========================================================
    // WORLD RESET
    // =========================================================

    protected override void OnResetState()
    {
        DebugChair(
            "World reset detected. Resetting all theater play tracking."
        );

        // ---------------------------------------------------------
        // STOP ANY CURRENT PLAY
        // ---------------------------------------------------------

        StopCurrentPlayIfNecessary();


        // ---------------------------------------------------------
        // RESET ALL WATCHED PLAYS
        // ---------------------------------------------------------
        //
        // This is the important fix.
        //
        // GameResetManager calls ResetInteraction() on every
        // WorldInteractable when the player dies/restarts a day
        // or starts a completely new game.
        //
        // The Chair must therefore reset its PRIVATE
        // playsWatched array here.
        //
        // ---------------------------------------------------------

        ResetPlayTracking();


        // ---------------------------------------------------------
        // RESTORE CHAIR INTERACTION
        // ---------------------------------------------------------

        RestoreChairInteraction();


        // ---------------------------------------------------------
        // FORCE UPDATE TO RECHECK THE CURRENT DAY
        // ---------------------------------------------------------

        lastCheckedDay = -1;


        DebugChair(
            "Theater play tracking successfully reset."
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugChair(
        string message
    )
    {
        if (!debugChair)
        {
            return;
        }


        Debug.Log(
            "[Chair] "
            + message
        );
    }
}
