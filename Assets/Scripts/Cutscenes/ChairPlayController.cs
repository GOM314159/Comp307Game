using UnityEngine;
using System.Collections.Generic;


public class ChairPlayController : MonoBehaviour
{
    [Header("Play Videos")]

    [Tooltip(
        "Element 0 = Day 1, " +
        "Element 1 = Day 2, etc."
    )]
    [SerializeField]
    private List<TheaterPlayVideo> dailyPlayVideos =
        new List<TheaterPlayVideo>();


    [Header("Debug")]

    [SerializeField]
    private bool debugController = true;


    private bool isPlaying = false;


    // =========================================================
    // PUBLIC PLAY
    // =========================================================

    public void PlayCurrentDay()
    {
        if (isPlaying)
        {
            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[ChairPlayController] GameStateManager missing."
            );

            return;
        }


        int day =
            GameStateManager.Instance.GetCurrentDay();

        int index =
            day - 1;


        // -----------------------------------------------------
        // VALIDATE INDEX
        // -----------------------------------------------------

        if (index < 0 ||
            index >= dailyPlayVideos.Count)
        {
            Debug.LogError(
                "[ChairPlayController] No play assigned " +
                "for Day " + day
            );

            return;
        }


        TheaterPlayVideo play =
            dailyPlayVideos[index];


        if (play == null)
        {
            Debug.LogError(
                "[ChairPlayController] Play video is null " +
                "for Day " + day
            );

            return;
        }


        StartPlay(day, play);
    }


    // =========================================================
    // START PLAY
    // =========================================================

    private void StartPlay(
        int day,
        TheaterPlayVideo play)
    {
        isPlaying = true;


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.BeginExternalPlayForDay(day);
        }
        else
        {
            GameStateManager.Instance.SetCurrentPlay(day);
        }

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.WatchingPlay
        );


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }


        // -----------------------------------------------------
        // HIDE HUD
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }


        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        // -----------------------------------------------------
        // PLAY VIDEO
        // -----------------------------------------------------

        DebugPlay(
            "Playing Day " + day + " theater video."
        );

        play.Play(
            () => OnPlayFinished(day)
        );
    }


    // =========================================================
    // PLAY FINISHED
    // =========================================================

    private void OnPlayFinished(int day)
    {
        isPlaying = false;

        DebugPlay(
            "Day " + day + " theater video finished."
        );

        if (TheaterManager.Instance != null &&
            TheaterManager.Instance.IsPlayActive())
        {
            TheaterManager.Instance.CompleteExternalPlay();
        }
        else if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance.RevealPortalForDay(day);

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetGameMode(
                    GameStateManager.GameMode.Lobby
                );
            }
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }
    }


    // =========================================================
    // STOP
    // =========================================================

    public void StopCurrentPlay()
    {
        if (!isPlaying)
        {
            return;
        }


        int day =
            GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentDay()
                : 1;


        int index = day - 1;


        if (index >= 0 &&
            index < dailyPlayVideos.Count)
        {
            TheaterPlayVideo play =
                dailyPlayVideos[index];

            if (play != null)
            {
                play.Stop();
            }
        }


        isPlaying = false;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugPlay(string message)
    {
        if (!debugController)
        {
            return;
        }

        Debug.Log(
            "[ChairPlayController] " + message
        );
    }

    // =========================================================
    // DEBUG - SKIP CURRENT PLAY
    // =========================================================

    public void SkipCurrentPlay()
    {
        bool theaterPlayActive =
            TheaterManager.Instance != null &&
            TheaterManager.Instance.IsPlayActive();

        if (!isPlaying && !theaterPlayActive)
        {
            Debug.Log(
                "[ChairPlayController] No theater play is currently playing."
            );
            return;
        }

        int day =
            GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentDay()
                : 1;

        int index = day - 1;

        if (
            index < 0 ||
            index >= dailyPlayVideos.Count
        )
        {
            Debug.LogError(
                "[ChairPlayController] Cannot skip play. " +
                "Invalid day index: " + index
            );
            return;
        }

        TheaterPlayVideo play =
            dailyPlayVideos[index];

        if (play == null)
        {
            Debug.LogError(
                "[ChairPlayController] Cannot skip play. " +
                "TheaterPlayVideo is missing for Day " + day
            );
            return;
        }

        Debug.Log(
            "[ChairPlayController] F9 pressed. " +
            "Skipping Day " + day +
            " theater play while allowing progression."
        );

        play.SkipAndComplete();
    }

}