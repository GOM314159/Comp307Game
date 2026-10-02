using UnityEngine;

public class Level3PianoInteractable : WorldInteractable
{
    [Header("Piano Models")]
    [Tooltip(
        "The CLOSED piano GameObject. This is the piano visible " +
        "before the player interacts with it."
    )]
    [SerializeField]
    private GameObject closedPiano;

    [Tooltip(
        "The OPEN piano GameObject. This is automatically hidden " +
        "at startup and shown after interaction."
    )]
    [SerializeField]
    private GameObject openPiano;

    [Header("Painting Changes")]
    [Tooltip(
        "Paintings that should change when this piano is opened. " +
        "If empty and automatic discovery is enabled, every " +
        "Level3Painting in the scene will be changed."
    )]
    [SerializeField]
    private Level3Painting[] paintings;

    [Tooltip(
        "If enabled, the script automatically finds every " +
        "Level3Painting in the current scene when no paintings " +
        "are assigned above."
    )]
    [SerializeField]
    private bool findPaintingsAutomatically = true;

    [Header("Interaction")]
    [Tooltip("Text displayed by the interaction prompt.")]
    [SerializeField]
    private string pianoPrompt = "Open Piano";

    private bool pianoOpened = false;


 

    protected override void Awake()
    {
        base.Awake();

        
        if (closedPiano == null)
        {
            closedPiano = gameObject;
        }

        pianoOpened = false;

        SetPianoModelsClosed();

      
    }


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

       
        TriggerPaintings();

        Debug.Log(
            "[Level3PianoInteractable] Piano opened. " +
            "Level 3 paintings have been triggered."
        );
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


//paont
    private void TriggerPaintings()
    {
        Level3Painting[] paintingsToChange = paintings;

        // If the Inspector list is empty, optionally find all
        // Level3Painting components in this scene.
        if (
            (paintingsToChange == null ||
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
                "[Level3PianoInteractable] No Level3Painting " +
                "components were found or assigned."
            );

            return;
        }

        for (
            int i = 0;
            i < paintingsToChange.Length;
            i++)
        {
            Level3Painting painting =
                paintingsToChange[i];

            if (painting == null)
            {
                continue;
            }

            painting.ChangeToSecondImage();
        }
    }


 //reset

    public bool IsPianoOpened()
    {
        return pianoOpened;
    }


    public void ResetPiano()
    {
        pianoOpened = false;

        SetPianoModelsClosed();

        Level3Painting[] paintingsToReset = paintings;

        if (
            (paintingsToReset == null ||
             paintingsToReset.Length == 0) &&
            findPaintingsAutomatically)
        {
            paintingsToReset =
                FindObjectsByType<Level3Painting>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
        }

        if (paintingsToReset != null)
        {
            for (
                int i = 0;
                i < paintingsToReset.Length;
                i++)
            {
                if (paintingsToReset[i] != null)
                {
                    paintingsToReset[i].ResetPainting();
                }
            }
        }

        SetInteractionEnabled(true);
    }
}
