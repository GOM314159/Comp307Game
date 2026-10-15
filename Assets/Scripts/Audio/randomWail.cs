using UnityEngine;

/// <summary>Plays a configured doll wail on a repeating, adjustable interval.</summary>
[RequireComponent(typeof(AudioSource))]
public class randomWail : MonoBehaviour
{
    [SerializeField] private AudioClip wail;
    [SerializeField, Min(0.1f)] private float intervalSeconds = 20f;
    [SerializeField, Min(0f)] private float volume = 4f;
    [SerializeField] private bool randomizeFirstPlay = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 60f;
    }

    private void OnEnable()
    {
        if (wail != null)
            InvokeRepeating(nameof(PlayWail), randomizeFirstPlay ? Random.Range(0f, Mathf.Max(0.1f, intervalSeconds)) : intervalSeconds,
                Mathf.Max(0.1f, intervalSeconds));
    }

    private void OnDisable() => CancelInvoke(nameof(PlayWail));

    private void PlayWail()
    {
        if (wail != null && audioSource != null)
            audioSource.PlayOneShot(wail, volume);
    }

    public void SetAudioPaused(bool paused)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            return;

        if (paused)
            audioSource.Pause();
        else
            audioSource.UnPause();
    }
}
