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

    [Tooltip(
        "Automatically found from the persistent Player if left empty."
    )]
    [SerializeField]
    private PlayerMovement playerMovement;

    [Tooltip(
        "Automatically found from the persistent Player if left empty."
    )]
    [SerializeField]
    private PlayerLook playerLook;


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
        // -----------------------------------------------------
        // PLAYER MOVEMENT
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();
        }


        // -----------------------------------------------------
        // PLAYER LOOK
        // -----------------------------------------------------

        if (playerLook == null)
        {
            playerLook =
                FindFirstObjectByType<PlayerLook>();
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
        // MAKE SURE PLAYER REFERENCES EXIST
        // -----------------------------------------------------

        ResolvePlayerReferences();


        // -----------------------------------------------------
        // PLAYER MOVEMENT
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            Debug.LogError(
                "[White Powder] PlayerMovement "
                + "could not be found on the persistent Player."
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
                + "could not be found on the persistent Player."
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