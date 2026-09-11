using UnityEngine;

public class GunInteractable : WorldInteractable
{
    // =========================================================
    // GUN SETTINGS
    // =========================================================

    [Header("Gun")]

    [Tooltip("The Gun item given to the player.")]
    [SerializeField]
    private GunItem gunItem;


    [Tooltip("Player inventory that receives the gun.")]
    [SerializeField]
    private PlayerInventory playerInventory;


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Gun Damage")]

    [SerializeField]
    private float gunDamage = 10f;


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
        // CHECK GUN ITEM
        // -----------------------------------------------------

        if (gunItem == null)
        {
            Debug.LogError(
                "[Gun] Gun Item has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK INVENTORY
        // -----------------------------------------------------

        if (playerInventory == null)
        {
            Debug.LogError(
                "[Gun] Player Inventory has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // SET DAMAGE
        // -----------------------------------------------------

        gunItem.weaponDamage = gunDamage;


        // -----------------------------------------------------
        // ADD GUN TO INVENTORY
        // -----------------------------------------------------

        bool added =
            playerInventory.AddItem(
                gunItem,
                1
            );


        // -----------------------------------------------------
        // INVENTORY FULL
        // -----------------------------------------------------

        if (!added)
        {
            Debug.Log(
                "[Gun] Could not pick up gun. "
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
            "[Gun] Gun picked up."
            + " | Damage: "
            + gunDamage
        );


        gameObject.SetActive(false);
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Pick Up Gun";
    }
}