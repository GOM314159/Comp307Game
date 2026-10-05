using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a window jumpscare (audio + optional visual) when the player
/// enters a trigger volume. Attach to a GameObject with a trigger Collider.
/// Not wired into any scene by default — add it manually where needed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WindowJumpscare : MonoBehaviour
{
    // =========================================================
    // TRIGGER SETTINGS
    // =========================================================

    [Header("Trigger Settings")]

    [Tooltip("Only objects with this tag can fire the jumpscare.")]
    [SerializeField]
    private string playerTag = "Player";

    [Tooltip("If enabled, this jumpscare can only fire once.")]
    [SerializeField]
    private bool triggerOnce = true;


    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Jumpscare Audio")]

    [Tooltip("AudioSource used to play the jumpscare sound.")]
    [SerializeField]
    private AudioSource jumpscareAudioSource;

    [Tooltip("Clip played when the jumpscare triggers.")]
    [SerializeField]
    private AudioClip jumpscareClip;

    [Tooltip("If true, creates a temporary AudioSource when none is assigned.")]
    [SerializeField]
    private bool createAudioSourceIfMissing = true;


    // =========================================================
    // VISUAL (OPTIONAL)
    // =========================================================

    [Header("Jumpscare Visual (Optional)")]

    [Tooltip("Optional object enabled for the duration of the scare (e.g. face at window).")]
    [SerializeField]
    private GameObject jumpscareVisual;

    [Tooltip("How long the visual stays active after triggering.")]
    [SerializeField, Min(0.05f)]
    private float scareDuration = 1.5f;

    [Tooltip("Disable the visual again after scareDuration.")]
    [SerializeField]
    private bool hideVisualAfterDuration = true;


    // =========================================================
    // RUNTIME
    // =========================================================

    private bool hasTriggered;
    private Coroutine scareRoutine;


    // =========================================================
    // UNITY
    // =========================================================

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

        if (!other.CompareTag(playerTag))
        {
            // Also accept child colliders under a tagged player root.
            Transform root = other.transform.root;

            if (root == null || !root.CompareTag(playerTag))
            {
                return;
            }
        }

        TriggerJumpscare();
    }


    // =========================================================
    // JUMPSCARE
    // =========================================================

    public void TriggerJumpscare()
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        hasTriggered = true;

        PlayJumpscareAudio();

        if (scareRoutine != null)
        {
            StopCoroutine(scareRoutine);
        }

        scareRoutine = StartCoroutine(ScareSequence());
    }


    private void PlayJumpscareAudio()
    {
        if (jumpscareClip == null)
        {
            Debug.LogWarning(
                "[WindowJumpscare] No jumpscareClip assigned on '"
                + gameObject.name
                + "'."
            );

            return;
        }

        EnsureAudioSource();

        if (jumpscareAudioSource == null)
        {
            return;
        }

        jumpscareAudioSource.PlayOneShot(jumpscareClip);
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


    // =========================================================
    // PUBLIC HELPERS
    // =========================================================

    /// <summary>
    /// Allows the jumpscare to fire again when triggerOnce is enabled.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
