using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
/// <summary>
/// Chair interactable used to watch the theater play.
///
/// The chair is reusable for every day:
///
/// Day 1 -> Play 1
/// Day 2 -> Play 2
/// Day 3 -> Play 3
/// Day 4 -> Play 4
///
/// A play can only be watched once during the current run/day.
///
/// IMPORTANT:
/// If the player dies during the case, the watched-play tracking
/// is reset, allowing the player to watch that day's play again
/// after respawning.
///
/// The theater plays are VideoPlayers rather than PlayableDirectors.
///
/// Each play can fade in and fade out using the assigned
/// CanvasGroup.
/// </summary>
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
     "How long the screen takes to fade to black before "
     + "the video starts."
     )]
    [SerializeField]
    private float fadeOutDuration = 0.75f;
    [Tooltip(
     "How long the screen takes to fade from black after "
     + "the video begins."
     )]
    [SerializeField]
    private float fadeInDuration = 0.75f;
    [Tooltip(
     "If true, the video begins while the screen is black "
     + "and then fades into the video."
    
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
    //
    // Runtime-only.
    // Resets when the game is completely restarted.
    private bool[] playsWatched;
    // ---------------------------------------------------------
    // CURRENT PLAY
    // ---------------------------------------------------------
    private bool isWatchingPlay = false;
    private int currentPlayDay = -1;
    private VideoPlayer currentVideoPlayer;

    private int lastCheckedDay = -1;
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
        }
    }
    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -=
            HandleGameLoss;
        }
        StopCurrentPlayIfNecessary();
    }
    private void Update()
    {
        // Keep the player locked while watching.
        if (isWatchingPlay)
        {
            LockPlayerTransform();
            return;
        }

        if (GameStateManager.Instance == null)
        {
            return;
        }

        int currentDay = GameStateManager.Instance.GetCurrentDay();
        int dayIndex = currentDay - 1;

        if (dayIndex < 0)
        {
            return;
        }

        EnsureDayTrackingExists(dayIndex);

        // Detect when the game moves to a new day.
        if (currentDay != lastCheckedDay)
        {
            lastCheckedDay = currentDay;

            // The chair is available again if this day's play
            // has not been watched yet.
            SetInteractionEnabled(!playsWatched[dayIndex]);

            if (!playsWatched[dayIndex])
            {
                ShowInteractionPrompt();
            }
            else
            {
                HideInteractionPrompt();
            }
        }
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
    private VideoPlayer GetPlayForDay(int dayIndex)
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
            elapsed += Time.unscaledDeltaTime;
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
        // RESET ALL PLAY AVAILABILITY
        //
        // This means:
        //
        // Player watches Day 1 play
        // Player dies during the case
        // Player respawns/restarts the day
        // Day 1 play can be watched again.
        // -----------------------------------------------------
        ResetPlayTracking();
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
        // Make sure the fade doesn't leave the player
        // permanently behind a black screen.
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
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
