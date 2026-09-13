using UnityEngine;

public class WhitePowderInteractable : WorldInteractable
{
    // =========================================================
    // SPEED BOOST
    // =========================================================

    [Header("Speed Boost")]

    [SerializeField]
    private float speedMultiplier = 1.25f;


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

        if (playerMovement == null)
        {
            Debug.LogError(
                "[White Powder] PlayerMovement "
                + "has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // APPLY SPEED BOOST
        // -----------------------------------------------------

        playerMovement.ApplyPermanentSpeedBoost(
            speedMultiplier
        );


        // -----------------------------------------------------
        // REGISTER SIDE QUEST ITEM
        // -----------------------------------------------------

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[White Powder] CaseManager is missing."
            );
        }
        else
        {
            bool registered =
                CaseManager.Instance.TryRegisterSideQuestItem(
                    this
                );

            if (!registered)
            {
                Debug.LogWarning(
                    "[White Powder] Picked up, but it was "
                    + "not the side quest item for the "
                    + "current case."
                );
            }
        }


        // -----------------------------------------------------
        // BASE INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // -----------------------------------------------------
        // REMOVE FROM WORLD
        // -----------------------------------------------------

        gameObject.SetActive(false);


        Debug.Log(
            "[White Powder] Speed boost activated."
            + " | Multiplier: x"
            + speedMultiplier
        );
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Take White Powder";
    }
}