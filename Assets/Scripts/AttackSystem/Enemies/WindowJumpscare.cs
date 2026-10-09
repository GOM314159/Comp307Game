
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Collider))]
public class WindowJumpscare : MonoBehaviour
{
    // TRIGGER SETTINGS

    [Header("Trigger Settings")]
    [Tooltip("Only objects with this tag can trigger the jumpscare.")]
    [SerializeField]
    private string playerTag = "Player";

    [Tooltip("If enabled, the jumpscare can only trigger once.")]
    [SerializeField]
    private bool triggerOnce = true;


    // AUDIO

    [Header("Jumpscare Audio")]
    [Tooltip("AudioSource used to play the jumpscare sound.")]
    [SerializeField]
    private AudioSource jumpscareAudioSource;

    [Tooltip("Audio clip played when the jumpscare triggers.")]
    [SerializeField]
    private AudioClip jumpscareClip;

    [Tooltip("Create an AudioSource automatically if none is assigned.")]
    [SerializeField]
    private bool createAudioSourceIfMissing = true;


    // OPTIONAL VISUAL

    [Header("Jumpscare Visual (Optional)")]
    [Tooltip("Optional object enabled while the jumpscare is active.")]
    [SerializeField]
    private GameObject jumpscareVisual;

    [Tooltip("How long the visual stays active.")]
    [SerializeField, Min(0.05f)]
    private float scareDuration = 1.5f;

    [Tooltip("Disable the visual after the scare duration.")]
    [SerializeField]
    private bool hideVisualAfterDuration = true;


    // WINDOW VIDEO

    [Header("Window Video")]
    [Tooltip("VideoPlayer on the window jumpscare screen.")]
    [SerializeField]
    private VideoPlayer windowVideoPlayer;

    [Tooltip("Optional video clip. If empty, uses the VideoPlayer's assigned clip.")]
    [SerializeField]
    private VideoClip windowVideoClip;

    [Tooltip("Stop the video after the scare duration. Disable to play the whole clip.")]
    [SerializeField]
    private bool stopVideoAfterScareDuration = true;


    // RUNTIME

    private bool hasTriggered;
    private Coroutine scareRoutine;
    private Coroutine stopVideoRoutine;


    // UNITY

    private void Reset()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (jumpscareVisual != null)
        {
            jumpscareVisual.SetActive(false);
        }

        EnsureAudioSource();

        // Try to find a VideoPlayer on this object or its children
        // if one has not been assigned in the Inspector.
        if (windowVideoPlayer == null)
        {
            windowVideoPlayer = GetComponentInChildren<VideoPlayer>(true);
        }

        if (windowVideoPlayer != null)
        {
            windowVideoPlayer.playOnAwake = false;
            windowVideoPlayer.isLooping = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        bool isPlayer = other.CompareTag(playerTag);

        if (!isPlayer)
        {
            Transform root = other.transform.root;

            if (root == null || !root.CompareTag(playerTag))
            {
                return;
            }
        }

        TriggerJumpscare();
    }


    // JUMPSCARE

    public void TriggerJumpscare()
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        hasTriggered = true;

        PlayJumpscareAudio();
        PlayWindowVideo();

        if (scareRoutine != null)
        {
            StopCoroutine(scareRoutine);
        }

        scareRoutine = StartCoroutine(ScareSequence());
    }

    private void PlayWindowVideo()
    {
        if (windowVideoPlayer == null)
        {
            Debug.LogWarning(
                "[WindowJumpscare] No VideoPlayer assigned or found.",
                this
            );

            return;
        }

        if (windowVideoClip != null)
        {
            windowVideoPlayer.clip = windowVideoClip;
        }

        if (windowVideoPlayer.clip == null)
        {
            Debug.LogWarning(
                "[WindowJumpscare] No video clip assigned.",
                this
            );

            return;
        }

        if (stopVideoRoutine != null)
        {
            StopCoroutine(stopVideoRoutine);
            stopVideoRoutine = null;
        }

        windowVideoPlayer.playOnAwake = false;
        windowVideoPlayer.isLooping = false;

        windowVideoPlayer.Stop();
        windowVideoPlayer.Play();

        if (stopVideoAfterScareDuration)
        {
            stopVideoRoutine = StartCoroutine(
                StopWindowVideoAfterDelay()
            );
        }
    }

    private IEnumerator StopWindowVideoAfterDelay()
    {
        yield return new WaitForSecondsRealtime(scareDuration);

        if (windowVideoPlayer != null)
        {
            windowVideoPlayer.Stop();
        }

        stopVideoRoutine = null;
    }

    private void PlayJumpscareAudio()
    {
        if (jumpscareClip == null)
        {
            Debug.LogWarning(
                "[WindowJumpscare] No jumpscare audio clip assigned.",
                this
            );

            return;
        }

        EnsureAudioSource();

        if (jumpscareAudioSource != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareClip);
        }
    }

    private IEnumerator ScareSequence()
    {
        if (jumpscareVisual != null)
        {
            jumpscareVisual.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(scareDuration);

        if (hideVisualAfterDuration && jumpscareVisual != null)
        {
            jumpscareVisual.SetActive(false);
        }

        scareRoutine = null;
    }

    private void EnsureAudioSource()
    {
        if (jumpscareAudioSource != null)
        {
            return;
        }

        jumpscareAudioSource = GetComponent<AudioSource>();

        if (jumpscareAudioSource != null)
        {
            return;
        }

        if (!createAudioSourceIfMissing)
        {
            return;
        }

        jumpscareAudioSource = gameObject.AddComponent<AudioSource>();
        jumpscareAudioSource.playOnAwake = false;
        jumpscareAudioSource.spatialBlend = 0f;
    }


    // PUBLIC HELPERS

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}