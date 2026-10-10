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

    [Header("Models by Day")]
    [Tooltip("Assign 1 model to use it for all days, or assign up to 3 models and map days below.")]
    [SerializeField] private List<GameObject> dayModels = new List<GameObject>();

    [Tooltip("Optional explicit model index per day. Example 0,1,2,2 uses 3 models for 4 days.")]
    [SerializeField] private List<int> modelIndexByDay = new List<int> { 0, 1, 2, 2 };

    [Tooltip("Optional single-model override. When assigned, this model is used every day.")]
    [SerializeField] private GameObject singleModelOverride;


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

        // Keep the component/root active so subscriptions remain valid, but
        // the NPC itself must disappear completely on Day 4.
        if (day >= 4)
        {
            // Day 4 has no Lobby NPC at all. Disable the whole root so its
            // collider, prompt and audio cannot remain in the lobby.
            SetAllModelsActive(false);

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        ApplyModelForDay(day);
    }


    private void ApplyModelForDay(int day)
    {
        if (singleModelOverride != null)
        {
            SetAllModelsActive(false);
            singleModelOverride.SetActive(true);
            return;
        }

        if (dayModels == null || dayModels.Count == 0)
        {
            return;
        }

        int dayIndex = Mathf.Max(0, day - 1);

        // Four gameplay days may use only three models. For Day 4 (and any
        // later safety-state), use the last configured day mapping.
        if (modelIndexByDay.Count > 0)
        {
            dayIndex = Mathf.Min(dayIndex, modelIndexByDay.Count - 1);
        }

        int modelIndex = dayIndex;

        if (dayIndex < modelIndexByDay.Count)
        {
            modelIndex = modelIndexByDay[dayIndex];
        }

        // One model is automatically reused for all days. With multiple
        // models, clamp to the last supplied model for any later day.
        modelIndex = Mathf.Clamp(
            modelIndex,
            0,
            dayModels.Count - 1
        );

        for (int i = 0; i < dayModels.Count; i++)
        {
            if (dayModels[i] != null)
            {
                dayModels[i].SetActive(i == modelIndex);
            }
        }
    }


    private void SetAllModelsActive(bool active)
    {
        if (singleModelOverride != null)
        {
            singleModelOverride.SetActive(active);
        }

        if (dayModels == null)
        {
            return;
        }

        for (int i = 0; i < dayModels.Count; i++)
        {
            if (dayModels[i] != null)
            {
                dayModels[i].SetActive(active);
            }
        }
    }


    public override void ResetInteraction()
    {
        base.ResetInteraction();

        // GameResetManager resets inactive WorldInteractables too. Re-apply
        // the current day so a Day 4 NPC cannot reappear after a reset.
        RefreshDayState();
    }


    public override bool CanInteract()
    {
        // The lobby NPC does not exist as an interactive character on the
        // final day. Keep the root active for state/event handling, but block
        // all interaction.
        if (GetCurrentDay() >= 4)
        {
            return false;
        }

        if (!base.CanInteract())
        {
            return false;
        }

        // Block re-interaction while a voiceline is still playing.
        if (audioSource != null && audioSource.isPlaying)
        {
            return false;
        }

        return true;
    }

    public void SetDialogueAudioPaused(bool paused)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            return;

        if (paused)
        {
            if (audioSource.isPlaying)
                audioSource.Pause();
        }
        else
        {
            audioSource.UnPause();
        }
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
