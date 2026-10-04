using UnityEngine;

public class Level3PianoInteractable : WorldInteractable
{
    [Header("Piano Models")]

    [Tooltip("The CLOSED piano model.")]
    [SerializeField]
    private GameObject closedPiano;

    [Tooltip("The OPEN piano model.")]
    [SerializeField]
    private GameObject openPiano;

    [Header("Interaction Volume")]

    [Tooltip(
        "Invisible BoxCollider used for looking/interacting. " +
        "Place and size this cube around the piano in the Scene view. " +
        "Create a child named 'Interaction Collider' if unassigned."
    )]
    [SerializeField]
    private BoxCollider interactionCollider;

    [SerializeField]
    private bool automaticallyFindInteractionCollider = true;

    [SerializeField]
    private bool showColliderDebug = true;

    [Header("Ambient Music")]

    [Tooltip("Music that plays before the piano is opened.")]
    [SerializeField]
    private AudioClip ambientMusic;

    [Tooltip(
        "One-shot sting played immediately when the ambient music stops, " +
        "for an abrupt cut-off ending."
    )]
    [SerializeField]
    private AudioClip abruptEndingClip;

    [Tooltip(
        "AudioSource used for the ambient piano music. Leave empty to " +
        "find/create one automatically."
    )]
    [SerializeField]
    private AudioSource ambientAudioSource;

    [SerializeField]
    [Range(0f, 1f)]
    private float ambientVolume = 0.6f;

    [SerializeField]
    [Range(0f, 1f)]
    private float abruptEndingVolume = 1f;

    [SerializeField]
    private bool playAmbientOnStart = true;

    [Header("Painting Changes")]

    [SerializeField]
    private Level3Painting[] paintings;

    [SerializeField]
    private bool findPaintingsAutomatically = true;

    [Header("Interaction")]

    [SerializeField]
    private string pianoPrompt = "Open Piano";

    private bool pianoOpened;

    protected override void Awake()
    {
        base.Awake();

        ResolveInteractionCollider();
        EnsureAmbientAudioSource();

        pianoOpened = false;

        SetPianoModelsClosed();
        EnableInteractionCollider();
    }

    private void Start()
    {
        if (playAmbientOnStart && !pianoOpened)
        {
            StartAmbientMusic();
        }
    }

    private void ResolveInteractionCollider()
    {
        if (interactionCollider == null &&
            automaticallyFindInteractionCollider)
        {
            Transform child =
                transform.Find("Interaction Collider");

            if (child != null)
            {
                interactionCollider =
                    child.GetComponent<BoxCollider>();
            }
        }

        if (interactionCollider == null)
        {
            interactionCollider =
                GetComponent<BoxCollider>();
        }

        if (interactionCollider == null)
        {
            Debug.LogError(
                "[Level3PianoInteractable] No interaction BoxCollider " +
                "found on "
                + gameObject.name
                + ". Add a child named 'Interaction Collider' with a " +
                "BoxCollider, or assign one in the Inspector."
            );
            return;
        }

        PrepareInteractionCollider();
    }

    private void PrepareInteractionCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }

        interactionCollider.isTrigger = true;
        interactionCollider.enabled = true;

        int interactionLayer =
            LayerMask.NameToLayer("Interaction");

        if (interactionLayer >= 0)
        {
            interactionCollider.gameObject.layer =
                interactionLayer;
        }
    }

    private void EnsureAmbientAudioSource()
    {
        if (ambientAudioSource == null)
        {
            ambientAudioSource = GetComponent<AudioSource>();
        }

        if (ambientAudioSource == null)
        {
            ambientAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        ambientAudioSource.playOnAwake = false;
        ambientAudioSource.loop = true;
        ambientAudioSource.spatialBlend = 0f;
        ambientAudioSource.volume = ambientVolume;
    }

    private void StartAmbientMusic()
    {
        EnsureAmbientAudioSource();

        if (ambientMusic == null)
        {
            Debug.LogWarning(
                "[Level3PianoInteractable] Ambient Music clip is not " +
                "assigned on " + gameObject.name + ".",
                this
            );
            return;
        }

        if (ambientAudioSource.isPlaying &&
            ambientAudioSource.clip == ambientMusic)
        {
            return;
        }

        ambientAudioSource.clip = ambientMusic;
        ambientAudioSource.volume = ambientVolume;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
    }

    private void StopAmbientMusic()
    {
        if (ambientAudioSource == null)
        {
            return;
        }

        ambientAudioSource.Stop();
        ambientAudioSource.clip = null;
        ambientAudioSource.loop = false;

        PlayAbruptEnding();
    }

    private void PlayAbruptEnding()
    {
        if (abruptEndingClip == null)
        {
            return;
        }

        EnsureAmbientAudioSource();

        ambientAudioSource.loop = false;
        ambientAudioSource.volume = abruptEndingVolume;

        // One-shot so the ambient loop source can still be reused on reset.
        ambientAudioSource.PlayOneShot(
            abruptEndingClip,
            abruptEndingVolume
        );
    }

    public override void Interact()
    {
        if (pianoOpened || !CanInteract())
        {
            return;
        }

        base.Interact();
        OpenPiano();
    }

    public override string GetInteractionPrompt()
    {
        return pianoPrompt;
    }

    private void OpenPiano()
    {
        if (pianoOpened)
        {
            return;
        }

        pianoOpened = true;

        StopAmbientMusic();
        SetPianoModelsOpen();
        DisableInteractionCollider();
        TriggerPaintings();
    }

    private void SetPianoModelsClosed()
    {
        if (closedPiano != null)
        {
            closedPiano.SetActive(true);
        }

        if (openPiano != null)
        {
            openPiano.SetActive(false);
        }
    }

    private void SetPianoModelsOpen()
    {
        if (closedPiano != null)
        {
            closedPiano.SetActive(false);
        }

        if (openPiano != null)
        {
            openPiano.SetActive(true);
        }
    }

    private void EnableInteractionCollider()
    {
        if (interactionCollider != null)
        {
            interactionCollider.enabled = true;
        }
    }

    private void DisableInteractionCollider()
    {
        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }
    }

    private void TriggerPaintings()
    {
        Level3Painting[] paintingsToChange = paintings;

        if ((paintingsToChange == null ||
             paintingsToChange.Length == 0) &&
            findPaintingsAutomatically)
        {
            paintingsToChange =
                FindObjectsByType<Level3Painting>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
        }

        if (paintingsToChange == null ||
            paintingsToChange.Length == 0)
        {
            Debug.LogWarning(
                "[Level3PianoInteractable] " +
                "No Level3Painting components were found."
            );
            return;
        }

        for (int i = 0; i < paintingsToChange.Length; i++)
        {
            if (paintingsToChange[i] != null)
            {
                paintingsToChange[i].ChangeToSecondImage();
            }
        }
    }

    public override void ResetInteraction()
    {
        base.ResetInteraction();

        pianoOpened = false;

        SetPianoModelsClosed();
        EnableInteractionCollider();
        ResetPaintings();

        if (playAmbientOnStart)
        {
            StartAmbientMusic();
        }
    }

    private void ResetPaintings()
    {
        Level3Painting[] paintingsToReset = paintings;

        if ((paintingsToReset == null ||
             paintingsToReset.Length == 0) &&
            findPaintingsAutomatically)
        {
            paintingsToReset =
                FindObjectsByType<Level3Painting>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
        }

        if (paintingsToReset == null)
        {
            return;
        }

        for (int i = 0; i < paintingsToReset.Length; i++)
        {
            if (paintingsToReset[i] != null)
            {
                paintingsToReset[i].ResetPainting();
            }
        }
    }

    public bool IsPianoOpened()
    {
        return pianoOpened;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showColliderDebug || interactionCollider == null)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.85f);
        Gizmos.matrix =
            interactionCollider.transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            interactionCollider.center,
            interactionCollider.size
        );

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
