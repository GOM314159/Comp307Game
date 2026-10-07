using UnityEngine;
using System;
using System.Collections.Generic;

public class TheaterManager : MonoBehaviour
{
    public static TheaterManager Instance { get; private set; }

    [Header("Day Configuration")]

    [SerializeField]
    [Min(1)]
    public int maximumDay = 4;

    // =========================================================
    // PLAY DATA
    // =========================================================

    [Serializable]
    public class PlayData
    {
        [Header("Play Information")]

        public int playID;

        public string playName;

        [TextArea(2, 5)]
        public string description;


        [Header("Case")]

        public bool hasCase = true;

        public int caseID = -1;


        [Header("Final Play")]

        public bool isFinalPlay = false;


        [Header("Audio")]

        public AudioManager.MusicType musicType =
            AudioManager.MusicType.Theater;

        public AudioManager.AmbienceType ambienceType =
            AudioManager.AmbienceType.Theater;
    }


    // =========================================================
    // DAY THEATER DATA
    // =========================================================

    [Serializable]
    public class DayTheaterData
    {
        [Header("Day")]
        [Min(1)]
        public int day = 1;


        [Header("Theater Description")]

        public string dayName;

        [TextArea(2, 5)]
        public string description;


        [Header("Plays")]

        public List<PlayData> plays =
            new List<PlayData>();
    }


    // =========================================================
    // THEATER DATABASE
    // =========================================================

    [Header("Theater Configuration")]

    [SerializeField]
    private List<DayTheaterData> days =
        new List<DayTheaterData>();


    // =========================================================
    // CURRENT PLAY
    // =========================================================

    [Header("Current Theater State")]

    [SerializeField]
    private DayTheaterData currentDay;

    [SerializeField]
    private PlayData currentPlay;
    // External chair/video playback can be the final day even when its PlayData
    // was not explicitly marked isFinalPlay in the Inspector.
    private bool externalPlayFinalOverride;

    [SerializeField]
    private bool playActive = false;

    [SerializeField]
    private bool playComplete = false;


    // =========================================================
    // DAY VISUAL STATE
    // =========================================================

    [Header("Day Changes")]

    [SerializeField]
    private int currentTheaterDay = 1;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnPlayStarted;

    public event Action OnPlayCompleted;

    public event Action OnFinalPlayStarted;

    public event Action OnFinalPlayCompleted;

    public event Action OnTheaterDayChanged;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugTheaterManager = true;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // DAY
    // =========================================================

    public void SetTheaterDay(int day)
    {
        if (day < 1 || day > maximumDay)
        {
            Debug.LogWarning(
                "[Theater] Invalid theater day: "
                + day
            );

            return;
        }

        currentTheaterDay = day;

        currentDay = GetDay(day);

        DebugTheater(
            "Theater changed to Day "
            + day
        );

        OnTheaterDayChanged?.Invoke();
    }


    public int GetTheaterDay()
    {
        return currentTheaterDay;
    }


    public DayTheaterData GetCurrentDay()
    {
        return currentDay;
    }

    // =========================================================
    // RESET FOR ENTIRE GAME
    // =========================================================

    public void ResetForGame()
    {
        Debug.Log(
            "[Theater] Resetting theater progression."
        );

        // Reset runtime state only. Serialized PlayData configuration is not modified.
        currentPlay = null;
        playActive = false;
        playComplete = false;
        externalPlayFinalOverride = false;
    }

    private DayTheaterData GetDay(int day)
    {
        foreach (DayTheaterData dayData in days)
        {
            if (dayData == null)
            {
                continue;
            }

            if (dayData.day == day)
            {
                return dayData;
            }
        }

        return null;
    }


    // =========================================================
    // PLAY
    // =========================================================

    public PlayData GetPlay(
        int day,
        int playID)
    {
        DayTheaterData dayData =
            GetDay(day);

        if (dayData == null)
        {
            return null;
        }

        foreach (PlayData play in dayData.plays)
        {
            if (play == null)
            {
                continue;
            }

            if (play.playID == playID)
            {
                return play;
            }
        }

        return null;
    }


    public PlayData GetCurrentPlay()
    {
        return currentPlay;
    }


    public int GetCurrentPlayID()
    {
        if (currentPlay == null)
        {
            return -1;
        }

        return currentPlay.playID;
    }


    // =========================================================
    // START PLAY
    // =========================================================

    public bool StartPlay(int playID)
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Theater] GameStateManager not found."
            );

            return false;
        }

        int day =
            GameStateManager.Instance.GetCurrentDay();

        SetTheaterDay(day);

        PlayData selectedPlay =
            GetPlay(day, playID);

        if (selectedPlay == null)
        {
            Debug.LogError(
                "[Theater] Play "
                + playID
                + " does not exist on Day "
                + day
            );

            return false;
        }

        currentPlay = selectedPlay;

        playActive = true;

        playComplete = false;

        GameStateManager.Instance
            .SetCurrentPlay(playID);

        GameStateManager.Instance
            .SetGameMode(
                GameStateManager.GameMode.WatchingPlay
            );

        DebugTheater(
            "Started Play "
            + selectedPlay.playID
            + ": "
            + selectedPlay.playName
        );


        // -----------------------------------------------------
        // AUDIO
        // -----------------------------------------------------

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                selectedPlay.musicType
            );

            AudioManager.Instance.PlayAmbience(
                selectedPlay.ambienceType
            );
        }


        // -----------------------------------------------------
        // FINAL PLAY
        // -----------------------------------------------------

        if (selectedPlay.isFinalPlay)
        {
            DebugTheater(
                "This is the FINAL PLAY."
            );

            OnFinalPlayStarted?.Invoke();
        }

        OnPlayStarted?.Invoke();

        return true;
    }


    // =========================================================
    // PLAY COMPLETE
    // =========================================================

    /// <summary>
    /// Binds a video that is played by another component (for example Chair.cs)
    /// to the authoritative theater state. This is especially important on
    /// Day 4 because the configured PlayData carries the final-play flag.
    /// </summary>
    public bool BeginExternalPlayForDay(int day)
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Theater] Cannot begin external play without GameStateManager."
            );
            return false;
        }

        SetTheaterDay(day);

        DayTheaterData dayData = GetDay(day);
        PlayData selectedPlay = null;

        if (dayData != null)
        {
            if (day == maximumDay)
            {
                foreach (PlayData candidate in dayData.plays)
                {
                    if (candidate != null && candidate.isFinalPlay)
                    {
                        selectedPlay = candidate;
                        break;
                    }
                }
            }

            if (selectedPlay == null)
            {
                foreach (PlayData candidate in dayData.plays)
                {
                    if (candidate != null)
                    {
                        selectedPlay = candidate;
                        break;
                    }
                }
            }
        }

        if (selectedPlay == null)
        {
            Debug.LogWarning(
                "[Theater] No PlayData is configured for external Day "
                + day
                + "."
            );

            currentPlay = null;
            externalPlayFinalOverride = day == maximumDay;
            playActive = true;
            playComplete = false;

            GameStateManager.Instance.SetCurrentPlay(day);
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.WatchingPlay
            );

            OnPlayStarted?.Invoke();
            return true;
        }

        currentPlay = selectedPlay;
        externalPlayFinalOverride = day == maximumDay || selectedPlay.isFinalPlay;
        playActive = true;
        playComplete = false;

        GameStateManager.Instance.SetCurrentPlay(
            selectedPlay.playID
        );

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.WatchingPlay
        );

        if (externalPlayFinalOverride)
        {
            OnFinalPlayStarted?.Invoke();
        }

        OnPlayStarted?.Invoke();

        DebugTheater(
            "External play bound: Day "
            + day
            + " / Play "
            + selectedPlay.playID
            + " / Final="
            + externalPlayFinalOverride
        );

        return true;
    }


    /// <summary>
    /// Completes an externally managed theater video. Normal external videos
    /// return to the lobby; a configured final play enters BossFight through
    /// the existing final-play pipeline.
    /// </summary>
    public void CancelExternalPlay()
    {
        if (!playActive)
        {
            return;
        }

        playActive = false;
        playComplete = false;
        externalPlayFinalOverride = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
        }
    }


    public void CompleteExternalPlay()
    {
        if (!playActive)
        {
            Debug.LogWarning(
                "[Theater] No external play is currently active."
            );
            return;
        }

        playActive = false;
        playComplete = true;

        bool isFinal =
            externalPlayFinalOverride ||
            (currentPlay != null && currentPlay.isFinalPlay);

        int completedDay =
            GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentDay()
                : currentTheaterDay;

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance.RevealPortalForDay(
                completedDay
            );
        }

        OnPlayCompleted?.Invoke();

        if (isFinal)
        {
            externalPlayFinalOverride = false;
            CompleteFinalPlay();
            return;
        }

        externalPlayFinalOverride = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Lobby
            );
        }
    }


    public void CompletePlay()
    {
        if (!playActive)
        {
            Debug.LogWarning(
                "[Theater] No play is currently active."
            );

            return;
        }

        playActive = false;

        playComplete = true;

        bool isFinal =
            currentPlay != null &&
            currentPlay.isFinalPlay;

        DebugTheater(
            "Play completed."
        );

        if (PortalProgressionManager.Instance != null &&
            GameStateManager.Instance != null)
        {
            int completedPlayDay =
                GameStateManager.Instance.GetCurrentDay();

            Debug.Log(
                "[Theater] Revealing portal for Day "
                + completedPlayDay
                + " after play completion."
            );

            PortalProgressionManager.Instance
                .RevealPortalForDay(completedPlayDay);
        }
        else
        {
            Debug.LogWarning(
                "[Theater] Could not reveal portal because " +
                "PortalProgressionManager or GameStateManager is missing."
            );
        }

        OnPlayCompleted?.Invoke();


        // -----------------------------------------------------
        // FINAL PLAY
        // -----------------------------------------------------

        if (isFinal)
        {
            CompleteFinalPlay();

            return;
        }


        // -----------------------------------------------------
        // NORMAL PLAY
        // -----------------------------------------------------

        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Lobby
        );

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.PlayFinished();
        }
    }


    // =========================================================
    // FINAL PLAY
    // =========================================================

    private void CompleteFinalPlay()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .CompleteFinalPlay();
        }

        DebugTheater(
            "Final play completed."
        );

        OnFinalPlayCompleted?.Invoke();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance
                .FinalPlayFinished();
        }
    }


    // =========================================================
    // CASE AFTER PLAY
    // =========================================================

    public bool HasCaseAfterCurrentPlay()
    {
        if (currentPlay == null)
        {
            return false;
        }

        return currentPlay.hasCase &&
               currentPlay.caseID >= 0;
    }


    public int GetCaseAfterCurrentPlay()
    {
        if (!HasCaseAfterCurrentPlay())
        {
            return -1;
        }

        return currentPlay.caseID;
    }


    public void StartCaseAfterPlay()
    {
        if (!HasCaseAfterCurrentPlay())
        {
            Debug.LogWarning(
                "[Theater] No case is assigned "
                + "to the current play."
            );

            return;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCase(
                currentPlay.caseID
            );
        }
    }


    // =========================================================
    // PLAY PROGRESSION
    // =========================================================

    public int GetNumberOfPlaysForDay(int day)
    {
        DayTheaterData dayData =
            GetDay(day);

        if (dayData == null)
        {
            return 0;
        }

        return dayData.plays.Count;
    }


    public bool HasPlay(
        int day,
        int playID)
    {
        return GetPlay(day, playID) != null;
    }


    public bool IsFinalPlay()
    {
        return externalPlayFinalOverride ||
               (currentPlay != null && currentPlay.isFinalPlay);
    }


    public bool IsPlayActive()
    {
        return playActive;
    }


    public bool IsPlayComplete()
    {
        return playComplete;
    }


    // =========================================================
    // NEXT PLAY
    // =========================================================

    public int GetNextPlayID()
    {
        if (currentDay == null ||
            currentPlay == null)
        {
            return -1;
        }

        int currentIndex =
            currentDay.plays.IndexOf(currentPlay);

        int nextIndex =
            currentIndex + 1;

        if (nextIndex >= currentDay.plays.Count)
        {
            return -1;
        }

        return currentDay
            .plays[nextIndex]
            .playID;
    }


    public bool HasNextPlay()
    {
        return GetNextPlayID() >= 0;
    }


    public void StartNextPlay()
    {
        int nextPlay =
            GetNextPlayID();

        if (nextPlay < 0)
        {
            DebugTheater(
                "No more plays this day."
            );

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance
                    .CompleteDay();
            }

            return;
        }

        StartPlay(nextPlay);
    }


    // =========================================================
    // DAY TRANSITION
    // =========================================================

    public void AdvanceTheaterDay()
    {
        int nextDay =
            currentTheaterDay + 1;

        if (nextDay > maximumDay)
        {
            Debug.LogWarning(
                "[Theater] Cannot advance "
                + "past Day 4."
            );

            return;
        }

        SetTheaterDay(nextDay);

        currentPlay = null;

        playActive = false;

        playComplete = false;

        DebugTheater(
            "Advanced theater to Day "
            + nextDay
        );
    }


    // =========================================================
    // DAY 4 / FINAL
    // =========================================================

    public bool IsFinalDay()
    {
        return currentTheaterDay == maximumDay;
    }


    public PlayData GetFinalPlay()
    {
        DayTheaterData dayFour =
            GetDay(4);

        if (dayFour == null)
        {
            return null;
        }

        foreach (PlayData play in dayFour.plays)
        {
            if (play != null &&
                play.isFinalPlay)
            {
                return play;
            }
        }

        return null;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetTheater()
    {
        currentTheaterDay = 1;

        currentDay = GetDay(1);

        currentPlay = null;

        playActive = false;

        playComplete = false;

        DebugTheater(
            "Theater reset to Day 1."
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugTheater(string message)
    {
        if (!debugTheaterManager)
        {
            return;
        }

        Debug.Log(
            "[Theater Manager] "
            + message
        );
    }
}