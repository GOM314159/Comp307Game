using UnityEngine;

public class CableCutterInteractable : WorldInteractable
{
    // =========================================================
    // CABLE CUTTER SETTINGS
    // =========================================================

    [Header("Cable Cutter")]

    [Tooltip("The Cable Cutter item given to the player.")]
    [SerializeField]
    private CableCutterItem cableCutterItem;


    [Tooltip("Player inventory that receives the Cable Cutter.")]
    [SerializeField]
    private PlayerInventory playerInventory;


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
        // CHECK CABLE CUTTER ITEM
        // -----------------------------------------------------

        if (cableCutterItem == null)
        {
            Debug.LogError(
                "[Cable Cutter] Cable Cutter Item has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK INVENTORY
        // -----------------------------------------------------

        if (playerInventory == null)
        {
            Debug.LogError(
                "[Cable Cutter] Player Inventory has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // ADD CABLE CUTTER TO INVENTORY
        // -----------------------------------------------------

        bool added =
            playerInventory.AddItem(
                cableCutterItem,
                1
            );


        // -----------------------------------------------------
        // INVENTORY FULL
        // -----------------------------------------------------

        if (!added)
        {
            Debug.Log(
                "[Cable Cutter] Could not pick up Cable Cutter. "
                + "Inventory is full."
            );

            return;
        }


        // -----------------------------------------------------
        // COMPLETE BASE INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // -----------------------------------------------------
        // REMOVE FROM WORLD
        // -----------------------------------------------------

        Debug.Log(
            "[Cable Cutter] Cable Cutter picked up."
        );


        gameObject.SetActive(false);
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Pick Up Cable Cutter";
    }
}