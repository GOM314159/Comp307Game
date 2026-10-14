using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Swaps the lobby between its per-day dress states.
///
/// Each day owns one "layer": a root of objects to ADD, and a list of
/// day-one objects to SWITCH OFF (so a prop can be replaced rather than
/// only added to).
///
/// Layers stack. Day 3 shows Day 2's layer and Day 3's layer together, so
/// the theatre keeps decaying instead of resetting each morning.
///
/// Put this on the Lobby root.
/// </summary>
[DisallowMultipleComponent]
public class LobbyDayVisuals : MonoBehaviour
{
    // =========================================================
    // DAY LAYER
    // =========================================================

    [Serializable]
    public class DayLayer
    {
        [Header("Applies From")]

        [Min(2)]
        public int fromDay = 2;


        [Header("Add")]

        [Tooltip("Root of everything this day ADDS. Everything under " +
                 "it is enabled from this day onward.")]
        public GameObject addRoot;


        [Header("Replace")]

        [Tooltip("Day-one objects switched OFF from this day onward. " +
                 "Use this when the day root contains a worse version " +
                 "of something that already exists.")]
        public List<GameObject> switchOff =
            new List<GameObject>();
    }


    // =========================================================
    // CONFIGURATION
    // =========================================================

    [Header("Day Layers")]

    [SerializeField]
    private List<DayLayer> layers =
        new List<DayLayer>();


    [Header("Behaviour")]

    [Tooltip("Apply the current day as soon as the lobby starts. " +
             "Needed because SetDay() does not raise OnDayChanged " +
             "when the day has not actually changed.")]
    [SerializeField]
    private bool applyOnStart = true;


    [Header("Runtime State")]

    [SerializeField]
    private int appliedDay = -1;


    [Header("Debug")]

    [SerializeField]
    private bool debugLobbyDayVisuals = false;


    // =========================================================
    // INTERNAL
    // =========================================================

    // What every switched-off object looked like before we touched
    // it, so day 1 can restore exactly rather than assuming "active".
    private readonly Dictionary<GameObject, bool> baseline =
        new Dictionary<GameObject, bool>();

    private bool baselineCaptured = false;

    private bool subscribed = false;

    private bool started = false;

    private GameStateManager subscribedGameState;

    private TheaterManager subscribedTheater;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CaptureBaseline();
    }


    private void Start()
    {
        started = true;

        Subscribe();

        if (applyOnStart)
        {
            Apply(ResolveCurrentDay(), true);
        }
    }


    private void OnEnable()
    {
        if (started)
        {
            Subscribe();
        }
    }


    private void OnDisable()
    {
        Unsubscribe();
    }


    private void OnDestroy()
    {
        Unsubscribe();
    }


    // =========================================================
    // BASELINE
    // =========================================================

    private void CaptureBaseline()
    {
        if (baselineCaptured)
        {
            return;
        }

        baselineCaptured = true;

        foreach (DayLayer layer in layers)
        {
            if (layer == null)
            {
                continue;
            }

            foreach (GameObject target in layer.switchOff)
            {
                if (target == null)
                {
                    continue;
                }

                if (!baseline.ContainsKey(target))
                {
                    baseline.Add(
                        target,
                        target.activeSelf
                    );
                }
            }
        }
    }


    // =========================================================
    // EVENTS
    // =========================================================

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        subscribedGameState = GameStateManager.Instance;

        if (subscribedGameState != null)
        {
            subscribedGameState.OnDayChanged +=
                HandleDayChanged;
        }

        subscribedTheater = TheaterManager.Instance;

        if (subscribedTheater != null)
        {
            subscribedTheater.OnTheaterDayChanged +=
                HandleTheaterDayChanged;
        }

        subscribed =
            subscribedGameState != null ||
            subscribedTheater != null;

        if (!subscribed)
        {
            Debug.LogWarning(
                "[LobbyDayVisuals] No GameStateManager or "
                + "TheaterManager found. The lobby will show "
                + "whatever day it was last told to."
            );
        }
    }


    private void Unsubscribe()
    {
        if (subscribedGameState != null)
        {
            subscribedGameState.OnDayChanged -=
                HandleDayChanged;
        }

        if (subscribedTheater != null)
        {
            subscribedTheater.OnTheaterDayChanged -=
                HandleTheaterDayChanged;
        }

        subscribedGameState = null;

        subscribedTheater = null;

        subscribed = false;
    }


    private void HandleDayChanged(int day)
    {
        Apply(day, false);
    }


    private void HandleTheaterDayChanged()
    {
        Apply(ResolveCurrentDay(), false);
    }


    // =========================================================
    // DAY
    // =========================================================

    private int ResolveCurrentDay()
    {
        if (GameStateManager.Instance != null)
        {
            return GameStateManager.Instance
                .GetCurrentDay();
        }

        if (TheaterManager.Instance != null)
        {
            return TheaterManager.Instance
                .GetTheaterDay();
        }

        return 1;
    }


    public void ApplyCurrentDay()
    {
        Apply(ResolveCurrentDay(), true);
    }


    /// <summary>
    /// Shows every layer whose fromDay is at or below the given day,
    /// and restores everything else to how it was on day one.
    /// </summary>
    public void Apply(int day, bool force)
    {
        if (!force && appliedDay == day)
        {
            return;
        }

        CaptureBaseline();


        // -----------------------------------------------------
        // RESET TO DAY ONE
        // -----------------------------------------------------

        foreach (KeyValuePair<GameObject, bool> entry in baseline)
        {
            if (entry.Key == null)
            {
                continue;
            }

            entry.Key.SetActive(entry.Value);
        }

        foreach (DayLayer layer in layers)
        {
            if (layer == null || layer.addRoot == null)
            {
                continue;
            }

            layer.addRoot.SetActive(false);
        }


        // -----------------------------------------------------
        // STACK EVERY LAYER UP TO TODAY
        // -----------------------------------------------------

        int appliedLayers = 0;

        foreach (DayLayer layer in layers)
        {
            if (layer == null || day < layer.fromDay)
            {
                continue;
            }

            appliedLayers++;

            if (layer.addRoot != null)
            {
                layer.addRoot.SetActive(true);
            }

            foreach (GameObject target in layer.switchOff)
            {
                if (target == null)
                {
                    continue;
                }

                target.SetActive(false);
            }
        }

        appliedDay = day;

        if (debugLobbyDayVisuals)
        {
            Debug.Log(
                "[LobbyDayVisuals] Day "
                + day
                + " applied ("
                + appliedLayers
                + " layer(s) active)."
            );
        }
    }


    public int GetAppliedDay()
    {
        return appliedDay;
    }


    // =========================================================
    // EDITOR PREVIEW
    // =========================================================

    [ContextMenu("Preview/Day 1")]
    private void PreviewDay1() { Apply(1, true); }

    [ContextMenu("Preview/Day 2")]
    private void PreviewDay2() { Apply(2, true); }

    [ContextMenu("Preview/Day 3")]
    private void PreviewDay3() { Apply(3, true); }

    [ContextMenu("Preview/Day 4")]
    private void PreviewDay4() { Apply(4, true); }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            DayLayer layer = layers[i];

            if (layer == null || layer.addRoot == null)
            {
                continue;
            }

            for (int j = 0; j < layer.switchOff.Count; j++)
            {
                GameObject target = layer.switchOff[j];

                if (target == null)
                {
                    continue;
                }

                // Switching off an ancestor of the day root would
                // hide the day root with it, and the reset pass
                // would then fight itself.
                if (target.transform.IsChildOf(
                        layer.addRoot.transform) ||
                    layer.addRoot.transform.IsChildOf(
                        target.transform))
                {
                    Debug.LogWarning(
                        "[LobbyDayVisuals] Day "
                        + layer.fromDay
                        + ": '"
                        + target.name
                        + "' is in the same branch as the day "
                        + "root. Switch-off targets must live "
                        + "outside the day root.",
                        this
                    );
                }
            }
        }
    }
}
