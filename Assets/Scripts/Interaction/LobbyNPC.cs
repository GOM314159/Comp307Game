using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Day-aware lobby NPC. Each day can have random voice lines for before/after
/// the theater play and for a death/respawn return. The final day can hide NPC.
/// </summary>
public class LobbyNPC : WorldInteractable
{
    [Serializable]
    public class DayDialogue
    {
        [Min(1)] public int day = 1;
        public List<AudioClip> beforePlayLines = new List<AudioClip>();
        public List<AudioClip> afterPlayLines = new List<AudioClip>();
        public List<AudioClip> respawnLines = new List<AudioClip>();
    }

    [Header("NPC")]
    [SerializeField] private AudioSource audioSource;

    [Header("Dialogue by Day")]
    [SerializeField] private List<DayDialogue> dayDialogue = new List<DayDialogue>();

    [Header("Final Day")]
    [SerializeField] private bool disappearOnFinalDay = true;
    [SerializeField, Min(1)] private int finalDay = 4;

    private bool playWatchedThisDay;
    private bool respawnPending;
    private int trackedDay = -1;

    protected override void Awake()
    {
        base.Awake();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        RefreshDayState();
    }

    private void OnEnable()
    {
        if (TheaterManager.Instance != null)
            TheaterManager.Instance.OnPlayCompleted += HandlePlayCompleted;
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnDayChanged += HandleDayChanged;
            GameStateManager.Instance.OnLoss += HandleLoss;
        }
        RefreshDayState();
    }

    private void Start()
    {
        Subscribe();
        RefreshDayState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (TheaterManager.Instance != null)
            TheaterManager.Instance.OnPlayCompleted -= HandlePlayCompleted;
        if (TheaterManager.Instance != null)
            TheaterManager.Instance.OnPlayCompleted += HandlePlayCompleted;
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnDayChanged -= HandleDayChanged;
            GameStateManager.Instance.OnDayChanged += HandleDayChanged;
            GameStateManager.Instance.OnLoss -= HandleLoss;
            GameStateManager.Instance.OnLoss += HandleLoss;
        }
    }

    private void Unsubscribe()
    {
        if (TheaterManager.Instance != null)
            TheaterManager.Instance.OnPlayCompleted -= HandlePlayCompleted;
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnDayChanged -= HandleDayChanged;
            GameStateManager.Instance.OnLoss -= HandleLoss;
        }
    }

    private void HandleDayChanged(int day)
    {
        trackedDay = day;
        playWatchedThisDay = false;
        respawnPending = false;
        RefreshDayState();
    }

    private void HandleLoss(GameStateManager.LossReason reason)
    {
        if (reason == GameStateManager.LossReason.PlayerDeath)
            respawnPending = true;
    }

    private void HandlePlayCompleted()
    {
        int day = GetCurrentDay();
        if (day != trackedDay)
        {
            trackedDay = day;
            playWatchedThisDay = false;
        }
        playWatchedThisDay = true;
    }

    private int GetCurrentDay()
    {
        return GameStateManager.Instance != null
            ? GameStateManager.Instance.GetCurrentDay()
            : 1;
    }

    private void RefreshDayState()
    {
        int day = GetCurrentDay();
        trackedDay = day;

        if (disappearOnFinalDay && day >= finalDay)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
    }

    public override void Interact()
    {
        if (!CanInteract()) return;
        base.Interact();

        DayDialogue data = FindDialogue(GetCurrentDay());
        if (data == null) return;

        List<AudioClip> lines = respawnPending
            ? data.respawnLines
            : playWatchedThisDay
                ? data.afterPlayLines
                : data.beforePlayLines;

        AudioClip clip = PickRandom(lines);
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);

        // A respawn line is a one-shot state; subsequent interactions use the
        // normal before/after-play pool.
        respawnPending = false;
    }

    private DayDialogue FindDialogue(int day)
    {
        foreach (DayDialogue data in dayDialogue)
        {
            if (data != null && data.day == day) return data;
        }
        return null;
    }

    private AudioClip PickRandom(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return null;
        List<AudioClip> valid = new List<AudioClip>();
        foreach (AudioClip clip in clips)
            if (clip != null) valid.Add(clip);
        return valid.Count == 0 ? null : valid[UnityEngine.Random.Range(0, valid.Count)];
    }
}
