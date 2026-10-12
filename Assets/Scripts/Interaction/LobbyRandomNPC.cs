using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays one random audio-only lobby NPC line after the day screen finishes.
/// This is separate from the existing lobby NPC. No dialogue text is displayed.
/// </summary>
[DisallowMultipleComponent]
public class LobbyRandomNPC : MonoBehaviour
{
    [Header("Audio-only random dialogue")]
    [Tooltip("Add the NPC voice clips here. One clip is selected randomly after the day screen finishes.")]
    [SerializeField] private List<AudioClip> voiceClips = new List<AudioClip>();

    [Tooltip("AudioSource used to play the selected voice clip. If empty, this object's AudioSource is used.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Optional delay after the day screen finishes before the NPC speaks.")]
    [SerializeField, Min(0f)] private float delayAfterDayScreen = 0.25f;

    [Tooltip("If enabled, the NPC plays at most one line per in-game day.")]
    [SerializeField] private bool onlyOncePerDay = true;

    [Tooltip("Prevent the same clip from playing twice in a row when multiple clips are available.")]
    [SerializeField] private bool avoidImmediateRepeat = true;

    private readonly HashSet<int> daysAlreadyPlayed = new HashSet<int>();
    private Coroutine playRoutine;
    private int lastClipIndex = -1;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private TheaterManager subscribedTheaterManager;

    private void OnEnable()
    {
        // Random NPC lines are tied to completing the theater play, not to
        // the day-screen video. This prevents Day 1 dialogue before the play.
        SubscribeToTheaterManager();
    }

    private void Start()
    {
        // TheaterManager may be initialized after this object is enabled.
        SubscribeToTheaterManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromTheaterManager();

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    private void SubscribeToTheaterManager()
    {
        TheaterManager manager = TheaterManager.Instance;
        if (manager == null || subscribedTheaterManager == manager)
            return;

        UnsubscribeFromTheaterManager();
        subscribedTheaterManager = manager;
        subscribedTheaterManager.OnPlayCompleted += HandlePlayCompleted;
    }

    private void UnsubscribeFromTheaterManager()
    {
        if (subscribedTheaterManager != null)
            subscribedTheaterManager.OnPlayCompleted -= HandlePlayCompleted;

        subscribedTheaterManager = null;
    }

    private void HandlePlayCompleted()
    {
        int day = GameStateManager.Instance != null
            ? GameStateManager.Instance.GetCurrentDay()
            : -1;

        // The random lobby NPC is intended for Days 1, 2 and 3 only.
        // Day 1 will only trigger after the theater play has completed.
        if (day < 1 || day > 3)
            return;

        if (onlyOncePerDay && daysAlreadyPlayed.Contains(day))
            return;

        if (audioSource == null || voiceClips == null || voiceClips.Count == 0)
            return;

        if (!voiceClips.Exists(clip => clip != null))
            return;

        daysAlreadyPlayed.Add(day);

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRandomVoiceAfterDelay());
    }

    private IEnumerator PlayRandomVoiceAfterDelay()
    {
        if (delayAfterDayScreen > 0f)
            yield return new WaitForSecondsRealtime(delayAfterDayScreen);

        List<int> validIndices = new List<int>();
        for (int i = 0; i < voiceClips.Count; i++)
        {
            if (voiceClips[i] != null)
                validIndices.Add(i);
        }

        if (validIndices.Count > 0)
        {
            int selectedIndex;
            if (avoidImmediateRepeat && validIndices.Count > 1 && validIndices.Contains(lastClipIndex))
            {
                validIndices.Remove(lastClipIndex);
                selectedIndex = validIndices[Random.Range(0, validIndices.Count)];
            }
            else
            {
                selectedIndex = validIndices[Random.Range(0, validIndices.Count)];
            }

            lastClipIndex = selectedIndex;
            audioSource.PlayOneShot(voiceClips[selectedIndex]);
        }

        playRoutine = null;
    }
}
