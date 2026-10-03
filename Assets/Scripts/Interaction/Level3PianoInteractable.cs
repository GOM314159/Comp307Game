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
    // INTERACTION COLLIDER
    // =========================================================

    [Header("Interaction Collider")]

    [Tooltip(
        "A manually positioned BoxCollider used specifically " +
        "for interacting with the piano. Create a child object " +
        "called 'Interaction Collider' and assign its BoxCollider here."
    )]
    [SerializeField]
    private BoxCollider interactionCollider;


    [Tooltip(
        "Automatically finds a child named 'Interaction Collider' " +
        "if the collider has not been assigned."
    )]
    [SerializeField]
    private bool automaticallyFindInteractionCollider = true;


    [Tooltip(
        "Shows the interaction collider in the Scene view while " +
        "the piano is selected. The actual BoxCollider gizmo is " +
        "also visible whenever the collider object is selected."
    )]
    [SerializeField]
    private bool showColliderDebug = true;


    // =========================================================
    // PAINTINGS
    // =========================================================

    [Header("Painting Changes")]

    [Tooltip(
        "Paintings that should change when the piano is opened."
    )]
    [SerializeField]
    private Level3Painting[] paintings;


    [Tooltip(
        "If enabled, automatically finds all Level3Painting " +
        "components in the current scene when the list above is empty."
    )]
    [SerializeField]
    private bool findPaintingsAutomatically = true;


    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]

    [SerializeField]
    private string pianoPrompt = "Open Piano";


    // =========================================================
    // STATE
    // =========================================================

    private bool pianoOpened;


    // =========================================================
    // AWAKE
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        ResolveInteractionCollider();

        pianoOpened = false;

        SetPianoModelsClosed();

        EnableInteractionCollider();
    }


    // =========================================================
    // FIND INTERACTION COLLIDER
    // =========================================================

    private void ResolveInteractionCollider()
    {
        if (interactionCollider != null)
        {
            PrepareInteractionCollider();
            return;
        }


        if (!automaticallyFindInteractionCollider)
        {
            Debug.LogError(
                "[Level3PianoInteractable] No Interaction Collider " +
                "has been assigned on "
                + gameObject.name
                + "."
            );

            return;
        }


        Transform child =
            transform.Find("Interaction Collider");


        if (child != null)
        {
            interactionCollider =
                child.GetComponent<BoxCollider>();
        }


        if (interactionCollider == null)
        {
            Debug.LogError(
                "[Level3PianoInteractable] Could not find a " +
                "BoxCollider on the child named 'Interaction Collider'. " +
                "Create one and assign it in the Inspector."
            );

            return;
        }


        PrepareInteractionCollider();
    }


    // =========================================================
    // PREPARE COLLIDER
    // =========================================================

    private void PrepareInteractionCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }


        // This collider is for interaction only.
        // It should never physically block the player.
        interactionCollider.isTrigger = true;

        interactionCollider.enabled = true;


        Debug.Log(
            "[Level3PianoInteractable] Interaction Collider assigned: "
            + interactionCollider.gameObject.name
        );
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact()
    {
        if (pianoOpened)
        {
            return;
        }


        if (!CanInteract())
        {
            return;
        }


        Debug.Log(
            "[Level3PianoInteractable] Interaction received."
        );


        base.Interact();

        OpenPiano();
    }


    public override string GetInteractionPrompt()
    {
        return pianoPrompt;
    }


    // =========================================================
    // OPEN PIANO
    // =========================================================

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


        Debug.Log(
            "[Level3PianoInteractable] Piano opened."
        );
    }


    // =========================================================
    // CLOSED STATE
    // =========================================================

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


    // =========================================================
    // OPEN STATE
    // =========================================================

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


    // =========================================================
    // INTERACTION COLLIDER ENABLE / DISABLE
    // =========================================================

    private void EnableInteractionCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }


        interactionCollider.enabled = true;
    }


    private void DisableInteractionCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }


        interactionCollider.enabled = false;
    }


    // =========================================================
    // PAINTINGS
    // =========================================================

    private void TriggerPaintings()
    {
        Level3Painting[] paintingsToChange =
            paintings;


        if (
            (paintingsToChange == null ||
             paintingsToChange.Length == 0) &&
            findPaintingsAutomatically
        )
        {
            paintingsToChange =
                FindObjectsByType<Level3Painting>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
        }


        if (
            paintingsToChange == null ||
            paintingsToChange.Length == 0
        )
        {
            Debug.LogWarning(
                "[Level3PianoInteractable] " +
                "No Level3Painting components were found."
            );

            return;
        }


        for (
            int i = 0;
            i < paintingsToChange.Length;
            i++
        )
        {
            if (paintingsToChange[i] != null)
            {
                paintingsToChange[i]
                    .ChangeToSecondImage();
            }
        }
    }


    // =========================================================
    // RESET
    // =========================================================
    //
    // GameResetManager already calls:
    //
    //     interactable.ResetInteraction();
    //
    // Because this class overrides ResetInteraction(), the piano
    // will now correctly reset when the day/game is reset.
    //
    // =========================================================

    public override void ResetInteraction()
    {
        // Let WorldInteractable restore its normal state first.
        base.ResetInteraction();


        pianoOpened = false;


        SetPianoModelsClosed();


        EnableInteractionCollider();


        ResetPaintings();


        Debug.Log(
            "[Level3PianoInteractable] Piano reset."
        );
    }


    private void ResetPaintings()
    {
        Level3Painting[] paintingsToReset =
            paintings;


        if (
            (paintingsToReset == null ||
             paintingsToReset.Length == 0) &&
            findPaintingsAutomatically
        )
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


        for (
            int i = 0;
            i < paintingsToReset.Length;
            i++
        )
        {
            if (paintingsToReset[i] != null)
            {
                paintingsToReset[i].ResetPainting();
            }
        }
    }


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsPianoOpened()
    {
        return pianoOpened;
    }


    // =========================================================
    // EDITOR DEBUG GIZMO
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (!showColliderDebug)
        {
            return;
        }


        if (interactionCollider == null)
        {
            return;
        }


        Gizmos.matrix =
            interactionCollider.transform.localToWorldMatrix;


        Gizmos.DrawWireCube(
            interactionCollider.center,
            interactionCollider.size
        );


        Gizmos.matrix =
            Matrix4x4.identity;
    }

#endif
}