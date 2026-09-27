using UnityEngine;

public class BlueTabletInteractable : WorldInteractable
{
    // =========================================================
    // STAMINA BOOST
    // =========================================================

    [Header("Stamina Boost")]

    [SerializeField]
    private float staminaIncrease = 25f;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [Tooltip(
        "Automatically found from the persistent Player if left empty."
    )]
    [SerializeField]
    private PlayerStamina playerStamina;


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
        if (playerStamina == null)
        {
            playerStamina =
                FindFirstObjectByType<PlayerStamina>();
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
        // FIND PLAYER STAMINA
        // -----------------------------------------------------

        ResolvePlayerReferences();


        // -----------------------------------------------------
        // CHECK PLAYER STAMINA
        // -----------------------------------------------------

        if (playerStamina == null)
        {
            Debug.LogError(
                "[Blue Tablet] PlayerStamina "
                + "could not be found on the persistent Player."
            );

            return;
        }


        // -----------------------------------------------------
        // APPLY PERMANENT STAMINA BOOST
        // -----------------------------------------------------

        playerStamina.ApplyPermanentStaminaBoost(
            staminaIncrease
        );


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        Debug.Log(
            "[Blue Tablet] Permanent stamina boost activated."
            + " | Increase: +"
            + staminaIncrease
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
        return "Take Blue Tablet";
    }
}