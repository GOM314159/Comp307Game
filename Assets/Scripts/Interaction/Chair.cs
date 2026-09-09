using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

/// <summary>
/// Chair interactable used to watch the theater play.
///
/// Each day can have its own PlayableDirector / Timeline.
/// The player is locked into a specific position and rotation
/// while the play is running.
///
/// A play can only be watched once per day.
/// Dying and respawning resets the play availability for that day.
/// Completely restarting the game also resets all play availability.
/// </summary>
public class Chair : WorldInteractable
{
    // =========================================================
    // CHAIR SETTINGS
    // =========================================================

    [Header("Chair")]

    [Tooltip("The position where the player will be placed when sitting.")]
    [SerializeField]
    private Transform playerSeatPosition;

    [Tooltip("Optional. If assigned, this transform determines the player's facing direction.")]
    [SerializeField]
    private Transform playerFacingDirection;


    // =========================================================
    // DAILY CUTSCENES
    // =========================================================

    [Header("Daily Theater Plays")]

    [Tooltip(
        "One Timeline / PlayableDirector per day. " +
        "Element 0 = Day 1, Element 1 = Day 2, etc."
    )]
    [SerializeField]
    private List<PlayableDirector> dailyPlays =
        new List<PlayableDirector>();


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Playback Settings")]

    [Tooltip("GameStateManager mode while the theater play is running.")]
    [SerializeField]
    private GameStateManager.GameMode watchingGameMode =
        GameStateManager.GameMode.WatchingPlay;

    [Tooltip("Player state used while watching the play.")]
    [SerializeField]
    private PlayerStateManager.PlayerState watchingPlayerState =
        PlayerStateManager.PlayerState.Cutscene;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugChair = true;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    // Tracks whether each day's play has already been watched.
    //
    // Example:
    // Index 0 = Day 1
    // Index 1 = Day 2
    // Index 2 = Day 3
    // Index 3 = Day 4
    //
    // This is intentionally runtime-only.
    // It therefore resets when the game completely restarts.
    private bool[] playsWatched;


    private bool isWatchingPlay = false;

    private int currentPlayDay = -1;

    private GameStateManager.GameMode previousGameMode;

    private PlayerStateManager.PlayerState previousPlayerState;

    private Vector3 lockedPlayerPosition;

    private Quaternion lockedPlayerRotation;

    private GameObject player;


    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        InitializePlayTracking();
    }


    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss += HandleGameLoss;
        }
    }


    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -= HandleGameLoss;
        }

        StopCurrentPlayIfNecessary();
    }


    private void Update()
    {
        if (!isWatchingPlay)
        {
            return;
        }

        LockPlayerTransform();
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializePlayTracking()
    {
        int numberOfDays = 0;

        if (GameStateManager.Instance != null)
        {
            numberOfDays =
                Mathf.Max(
                    1,
                    GameStateManager.Instance.CurrentDay
                );
        }

        // The list itself determines how many possible plays exist.
        //
        // This is resized automatically so the system can support
        // additional days without changing this script.
        int numberOfPlays =
            Mathf.Max(
                numberOfDays,
                dailyPlays.Count
            );

        playsWatched =
            new bool[numberOfPlays];
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact()
    {
        // -----------------------------------------------------
        // BASIC INTERACTION CHECK
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
        // GAME STATE CHECK
        // -----------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Chair] GameStateManager is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // PLAYER STATE CHECK
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


        // -----------------------------------------------------
        // MAKE SURE TRACKING ARRAY IS LARGE ENOUGH
        // -----------------------------------------------------

        EnsureDayTrackingExists(dayIndex);


        // -----------------------------------------------------
        // HAS THIS PLAY ALREADY BEEN WATCHED?
        // -----------------------------------------------------

        if (playsWatched[dayIndex])
        {
            DebugChair(
                "Day " + currentDay +
                " play has already been watched."
            );

            return;
        }


        // -----------------------------------------------------
        // GET THAT DAY'S PLAY
        // -----------------------------------------------------

        PlayableDirector play =
            GetPlayForDay(dayIndex);

        if (play == null)
        {
            Debug.LogWarning(
                "[Chair] No theater play has been assigned for Day "
                + currentDay
                + "."
            );

            return;
        }


        // -----------------------------------------------------
        // START PLAY
        // -----------------------------------------------------

        StartPlay(
            currentDay,
            play
        );
    }


    // =========================================================
    // GET PLAY FOR DAY
    // =========================================================

    private PlayableDirector GetPlayForDay(int dayIndex)
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
        PlayableDirector play
    )
    {
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
        // SAVE PREVIOUS STATES
        // -----------------------------------------------------

        previousGameMode =
            GameStateManager.Instance.GetGameMode();

        previousPlayerState =
            playerState.GetCurrentState();


        // -----------------------------------------------------
        // POSITION PLAYER
        // -----------------------------------------------------

        MovePlayerIntoSeat();


        // -----------------------------------------------------
        // LOCK PLAYER
        // -----------------------------------------------------

        LockPlayerTransform();


        // -----------------------------------------------------
        // MARK PLAY AS WATCHED
        // -----------------------------------------------------

        playsWatched[day - 1] = true;

        currentPlayDay = day;


        // -----------------------------------------------------
        // SET GAME STATE
        // -----------------------------------------------------

        GameStateManager.Instance.SetGameMode(
            watchingGameMode
        );


        // -----------------------------------------------------
        // SET PLAYER STATE
        //
        // PlayerStateManager.Cutscene already prevents:
        //
        // - Movement
        // - Looking
        // - Interaction
        // - Inventory
        // - Attacking
        // - Pause
        // -----------------------------------------------------

        playerState.SetState(
            watchingPlayerState
        );


        // -----------------------------------------------------
        // PLAY TIMELINE
        // -----------------------------------------------------

        isWatchingPlay = true;

        play.stopped += OnPlayFinished;

        play.Play();


        DebugChair(
            "Started Day "
            + day
            + " theater play."
        );
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

        // Store the final position and rotation.
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
    // PLAY FINISHED
    // =========================================================

    private void OnPlayFinished(
        PlayableDirector finishedDirector
    )
    {
        finishedDirector.stopped -= OnPlayFinished;

        if (!isWatchingPlay)
        {
            return;
        }

        isWatchingPlay = false;


        DebugChair(
            "Day "
            + currentPlayDay
            + " theater play finished."
        );


        // -----------------------------------------------------
        // RESTORE PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                previousPlayerState
            );
        }


        // -----------------------------------------------------
        // RESTORE GAME MODE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                previousGameMode
            );
        }


        currentPlayDay = -1;

        player = null;
    }


    // =========================================================
    // DEATH / LOSS
    // =========================================================

    private void HandleGameLoss(
        GameStateManager.LossReason reason
    )
    {
        if (reason != GameStateManager.LossReason.PlayerDeath)
        {
            return;
        }


        // -----------------------------------------------------
        // STOP PLAY
        // -----------------------------------------------------

        StopCurrentPlayIfNecessary();


        // -----------------------------------------------------
        // RESET ALL DAILY PLAY AVAILABILITY
        //
        // This is intentional.
        //
        // Normal mode:
        // PlayerDeathManager.ResetCurrentDay()
        // resets the current day.
        //
        // Hardcore:
        // PlayerDeathManager.ResetEntireGame()
        // starts the game over.
        //
        // In both cases, the theater play should be available
        // again when the new run begins.
        // -----------------------------------------------------

        ResetPlayTracking();


        DebugChair(
            "Player died. Theater plays have been reset."
        );
    }


    // =========================================================
    // STOP PLAY
    // =========================================================

    private void StopCurrentPlayIfNecessary()
    {
        if (!isWatchingPlay)
        {
            return;
        }

        PlayableDirector play =
            GetPlayForDay(currentPlayDay - 1);

        if (play != null)
        {
            play.stopped -= OnPlayFinished;

            if (play.state == PlayState.Playing)
            {
                play.Stop();
            }
        }

        isWatchingPlay = false;

        currentPlayDay = -1;
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

        for (int i = 0; i < playsWatched.Length; i++)
        {
            playsWatched[i] = false;
        }

        currentPlayDay = -1;
        isWatchingPlay = false;
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

    private void DebugChair(string message)
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
