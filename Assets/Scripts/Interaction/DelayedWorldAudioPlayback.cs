using System.Collections;
using UnityEngine;

/// <summary>
/// Plays one-shot world audio after a delay, even if the pickup object that
/// requested it is immediately disabled or destroyed.
/// </summary>
public sealed class DelayedWorldAudioPlayback : MonoBehaviour
{
    private static DelayedWorldAudioPlayback instance;

    public static void Play(AudioClip clip, Vector3 position, float delay, float volume = 1f)
    {
        if (clip == null) return;
        if (instance == null)
        {
            var runner = new GameObject("[DelayedWorldAudioPlayback]");
            DontDestroyOnLoad(runner);
            instance = runner.AddComponent<DelayedWorldAudioPlayback>();
        }
        instance.StartCoroutine(instance.PlayRoutine(clip, position, Mathf.Max(0f, delay), Mathf.Clamp01(volume)));
    }

    public static void PlayThroughAudioManager(AudioClip clip, float delay, float volume = 1f)
    {
        if (clip == null) return;
        if (instance == null)
        {
            var runner = new GameObject("[DelayedWorldAudioPlayback]");
            DontDestroyOnLoad(runner);
            instance = runner.AddComponent<DelayedWorldAudioPlayback>();
        }
        instance.StartCoroutine(instance.PlaySFXRoutine(clip, Mathf.Max(0f, delay), Mathf.Clamp01(volume)));
    }

    private IEnumerator PlaySFXRoutine(AudioClip clip, float delay, float volume)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (clip == null) yield break;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip, volume);
        else AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }

    private IEnumerator PlayRoutine(AudioClip clip, Vector3 position, float delay, float volume)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
