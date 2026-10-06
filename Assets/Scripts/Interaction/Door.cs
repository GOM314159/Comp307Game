using UnityEngine;

public class Door : WorldInteractable
{
    // =========================================================
    // DOOR SETTINGS
    // =========================================================

    [Header("Door")]

    [Tooltip("Whether the door starts in the open position.")]
    [SerializeField]
    private bool startsOpen = false;

    [Tooltip("Whether the player can open and close the door repeatedly.")]
    [SerializeField]
    private bool canClose = true;


    // =========================================================
    // DOOR STATE
    // =========================================================

    private bool isOpen = false;


    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        isOpen = startsOpen;

        // -----------------------------------------------------
        // ANIMATION SETUP
        // -----------------------------------------------------


        // -----------------------------------------------------
        // SOUND SETUP
        // -----------------------------------------------------
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact()
    {
        // Make sure the normal WorldInteractable checks
        // still happen first.
        if (!CanInteract())
        {
            return;
        }

        // -----------------------------------------------------
        // OPEN / CLOSE
        // -----------------------------------------------------

        if (!isOpen)
        {
            OpenDoor();
        }
        else if (canClose)
        {
            CloseDoor();
        }
    }


    // =========================================================
    // OPEN
    // =========================================================

    private void OpenDoor()
    {
        isOpen = true;

        Debug.Log("[Door] " + gameObject.name + " opened.");

        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------
        Animator animator = GetComponent<Animator>();
        animator.SetTrigger("Open");


        // -----------------------------------------------------
        // SOUND
        // -----------------------------------------------------
    }


    // =========================================================
    // CLOSE
    // =========================================================

    private void CloseDoor()
    {
        isOpen = false;

        Debug.Log("[Door] " + gameObject.name + " closed.");

        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------
        Animator animator = GetComponent<Animator>();
        animator.SetTrigger("Close");

        // -----------------------------------------------------
        // SOUND
        // -----------------------------------------------------
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        if (isOpen && canClose)
        {
            return "Close";
        }

        return "Open";
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsOpen()
    {
        return isOpen;
    }


    // =========================================================
    // RESET
    // =========================================================

    public override void ResetInteraction()
    {
        base.ResetInteraction();

        isOpen = startsOpen;

        // -----------------------------------------------------
        // RESET ANIMATION
        // -----------------------------------------------------
    }
}