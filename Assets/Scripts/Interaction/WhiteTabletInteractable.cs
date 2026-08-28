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

    [SerializeField]
    private PlayerMovement playerMovement;


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
        // CHECK PLAYER
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            Debug.LogError(
                "[White Tablet] PlayerMovement has not been assigned."
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
        // REMOVE TABLET
        // -----------------------------------------------------

        Destroy(gameObject);
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Take White Tablet";
    }
}