using UnityEngine;

public class WhiteTabletInteractable : WorldInteractable
{
    // =========================================================
    // STRENGTH BOOST
    // =========================================================

    [Header("Strength Boost")]

    [SerializeField]
    private float strengthIncrease = 5f;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [Tooltip(
        "Automatically found from the persistent Player if left empty."
    )]
    [SerializeField]
    private PlayerMovement playerMovement;


    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        ResolvePlayerReferences();
    }


    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void ResolvePlayerReferences()
    {
        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();
        }
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact()
    {
        if (!CanInteract())
        {
            return;
        }


        // -----------------------------------------------------
        // FIND PLAYER MOVEMENT
        // -----------------------------------------------------

        ResolvePlayerReferences();


        // -----------------------------------------------------
        // CHECK PLAYER
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            Debug.LogError(
                "[White Tablet] PlayerMovement "
                + "could not be found on the persistent Player."
            );

            return;
        }


        // -----------------------------------------------------
        // APPLY PERMANENT STRENGTH
        // -----------------------------------------------------

        playerMovement.ApplyPermanentStrengthBoost(
            strengthIncrease
        );


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        Debug.Log(
            "[White Tablet] Permanent strength boost activated."
            + " | Increase: +"
            + strengthIncrease
        );


        // -----------------------------------------------------
        // BASE INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // -----------------------------------------------------
        // REMOVE TABLET
        // -----------------------------------------------------

        gameObject.SetActive(false);
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Take White Tablet";
    }
}