using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Connects the chair to the theater play videos.
///
/// The chair remains reusable for every day.
///
/// Index:
///
/// 0 = Day 1
/// 1 = Day 2
/// 2 = Day 3
/// 3 = Day 4
/// </summary>
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

        GameStateManager.Instance.SetCurrentPlay(day);

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


        // -----------------------------------------------------
        // RETURN TO LOBBY
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
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
}