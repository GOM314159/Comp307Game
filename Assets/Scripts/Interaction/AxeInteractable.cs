using UnityEngine;

public class AxeInteractable : WorldInteractable
{
    // =========================================================
    // AXE SETTINGS
    // =========================================================

    [Header("Axe")]

    [Tooltip("The StormBreaker given to the player.")]
    [SerializeField]
    private AxeItem axeItem;


    [Tooltip("Player inventory that receives the axe.")]
    [SerializeField]
    private PlayerInventory playerInventory;



    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Axe Damage")]

    [SerializeField]
    private float axeDamage = 10f;


    // =========================================================
    // INTERACTION
    // =========================================================

    public override void Interact()
    {
        // Make sure this object can currently be interacted with.
        if (!CanInteract())
        {
            return;
        }


        // -----------------------------------------------------
        // CHECK AXE ITEM
        // -----------------------------------------------------

        if (axeItem == null)
        {
            Debug.LogError(
                "[Axe] Axe Item has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK INVENTORY
        // -----------------------------------------------------

        if (playerInventory == null)
        {
            Debug.LogError(
                "[Axe] Player Inventory has not been assigned."
            );

            return;
        }

        axeItem.weaponDamage = axeDamage;


        // -----------------------------------------------------
        // ADD AXE TO INVENTORY
        // -----------------------------------------------------

        bool added =
            playerInventory.AddItem(
                axeItem,
                1
            );



        // -----------------------------------------------------
        // INVENTORY FULL
        // -----------------------------------------------------

        if (!added)
        {
            Debug.Log(
                "[Axe] Could not pick up axe. "
                + "Inventory is full."
            );

            return;
        }


        // -----------------------------------------------------
        // OBJECTIVE - SIDD PLEASE SEE ME
        // -----------------------------------------------------

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[Axe] CaseManager is missing. " +
                "Cannot update case objective."
            );

            return;
        }

        CaseManager.Instance.FindRequiredObject();


        // -----------------------------------------------------
        // COMPLETE BASE INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // --------------------------------------------------------
        // REMOVE AXE FROM WORLD - WISH I COULD ALT + F4 MYSELF FR  
        // --------------------------------------------------------

        Debug.Log(
            "[Axe] Axe picked up."
            + " | Damage: "
            + axeDamage
            + " | Objective updated."
        );


        Destroy(gameObject);
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Pick Up Axe";
    }
}