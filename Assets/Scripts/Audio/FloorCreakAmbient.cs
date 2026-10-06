using UnityEngine;

/// <summary>
/// Plays sporadic floor-creak one-shots. Place in Level 1 (or any case
/// scene) and assign wood/creak clips in the Inspector.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FloorCreakAmbient : MonoBehaviour
{
    [Header("Audio")]

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip[] creakClips;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.45f;

    [Header("Timing (seconds)")]

    [Tooltip("Minimum wait between creaks.")]
    [SerializeField, Min(5f)]
    private float minInterval = 45f;

    [Tooltip("Maximum wait between creaks.")]
    [SerializeField, Min(5f)]
    private float maxInterval = 120f;

    [Tooltip("Chance (0-1) that a scheduled tick actually plays a creak.")]
    [SerializeField, Range(0f, 1f)]
    private float playChance = 0.55f;

    private float nextCreakTime;


    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        ScheduleNextCreak(immediate: true);
    }


    private void Update()
    {
        if (creakClips == null || creakClips.Length == 0)
        {
            return;
        }

        if (Time.time < nextCreakTime)
        {
            return;
        }

        if (Random.value <= playChance)
        {
            PlayRandomCreak();
        }

        ScheduleNextCreak(immediate: false);
    }


    private void PlayRandomCreak()
    {
        AudioClip clip = PickRandomClip();

        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
    }


    private AudioClip PickRandomClip()
    {
        int validCount = 0;

        for (int i = 0; i < creakClips.Length; i++)
        {
            if (creakClips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int pick = Random.Range(0, validCount);

        for (int i = 0; i < creakClips.Length; i++)
        {
            if (creakClips[i] == null)
            {
                continue;
            }

            if (pick == 0)
            {
                return creakClips[i];
            }

            pick--;
        }

        return null;
    }


    private void ScheduleNextCreak(bool immediate)
    {
        float min = Mathf.Min(minInterval, maxInterval);
        float max = Mathf.Max(minInterval, maxInterval);

        float delay = immediate
            ? Random.Range(min * 0.35f, min)
            : Random.Range(min, max);

        nextCreakTime = Time.time + delay;
    }
}
