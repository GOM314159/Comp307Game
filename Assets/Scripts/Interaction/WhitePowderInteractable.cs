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
    // FOV BOOST
    // =========================================================

    [Header("Permanent FOV Boost")]

    [Tooltip(
        "The permanent FOV the player receives after activating "
        + "the White Powder."
    )]
    [SerializeField]
    private float activatedFOV = 60f;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerLook playerLook;


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
        // PLAYER MOVEMENT
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            Debug.LogError(
                "[White Powder] PlayerMovement "
                + "has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // PLAYER LOOK
        // -----------------------------------------------------

        if (playerLook == null)
        {
            Debug.LogError(
                "[White Powder] PlayerLook "
                + "has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // APPLY PERMANENT SPEED BOOST
        // -----------------------------------------------------

        playerMovement.ApplyPermanentSpeedBoost(
            speedMultiplier
        );


        // -----------------------------------------------------
        // APPLY PERMANENT FOV
        // -----------------------------------------------------

        playerLook.ApplyPermanentFOV(
            activatedFOV
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
            "[White Powder] Permanent buffs activated."
            + " | Speed Multiplier: x"
            + speedMultiplier
            + " | FOV: "
            + activatedFOV
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