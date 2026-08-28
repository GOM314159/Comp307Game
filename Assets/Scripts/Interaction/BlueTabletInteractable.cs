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

    [SerializeField]
    private PlayerStamina playerStamina;


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
        // CHECK PLAYER STAMINA
        // -----------------------------------------------------

        if (playerStamina == null)
        {
            Debug.LogError(
                "[Blue Tablet] PlayerStamina has not been assigned."
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
        // REMOVE TABLET
        // -----------------------------------------------------

        Destroy(gameObject);
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Take Blue Tablet";
    }
}