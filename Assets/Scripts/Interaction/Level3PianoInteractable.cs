using UnityEngine;

public class Level3PianoInteractable : WorldInteractable
{
    // =========================================================
    // PIANO MODELS
    // =========================================================

    [Header("Piano Models")]

    [Tooltip("The CLOSED piano model.")]
    [SerializeField]
    private GameObject closedPiano;

    [Tooltip("The OPEN piano model.")]
    [SerializeField]
    private GameObject openPiano;


    // =========================================================
    // INTERACTION VOLUME
    // =========================================================

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


    // =========================================================
    // PAINTINGS
    // =========================================================

    [Header("Painting Changes")]

    [SerializeField]
    private Level3Painting[] paintings;

    [SerializeField]
    private bool findPaintingsAutomatically = true;


    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]

    [SerializeField]
    private string pianoPrompt = "Open Piano";


    private bool pianoOpened;


    protected override void Awake()
    {
        base.Awake();

        ResolveInteractionCollider();

        pianoOpened = false;

        SetPianoModelsClosed();

        EnableInteractionCollider();
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
            // Prefer a BoxCollider on this object so you can just drop an
            // invisible cube collider on the piano root if you want.
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

        // Interaction-only volume: never physically blocks the player.
        interactionCollider.isTrigger = true;
        interactionCollider.enabled = true;

        // Keep the collider on the Interaction layer so PlayerInteraction
        // can see it through the interaction mask.
        int interactionLayer =
            LayerMask.NameToLayer("Interaction");

        if (interactionLayer >= 0)
        {
            interactionCollider.gameObject.layer =
                interactionLayer;
        }
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
