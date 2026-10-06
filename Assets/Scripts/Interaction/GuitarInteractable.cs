using UnityEngine;

public class GuitarInteractable : WorldInteractable
{
    // =========================================================
    // GUITAR SETTINGS
    // =========================================================

    [Header("Calvin's Gibson Les Paul")]

    [Tooltip("The guitar item added to the player's inventory.")]
    [SerializeField]
    private GuitarItem guitarItem;

    [Tooltip("Player inventory that receives the guitar. " +
             "Leave empty to find it automatically.")]
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

        if (guitarItem == null)
        {
            Debug.LogError(
                "[Guitar] Guitar Item has not been assigned."
            );
            return;
        }

        if (playerInventory == null)
        {
            playerInventory =
                FindFirstObjectByType<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            Debug.LogError(
                "[Guitar] Player Inventory could not be found."
            );
            return;
        }

        // Add first. If the inventory is full, the world guitar remains
        // available and the interaction is not consumed.
        bool added =
            playerInventory.AddItem(
                guitarItem,
                1
            );

        if (!added)
        {
            Debug.Log(
                "[Guitar] Could not pick up guitar. Inventory is full."
            );
            return;
        }

        base.Interact();

        // Remove the world object after the inventory successfully
        // received the guitar. SetActive(false) matches the existing
        // pickup system and allows the object's state to be reset by
        // the game's normal world-reset systems.
        gameObject.SetActive(false);

        Debug.Log(
            "[Guitar] Calvin's Gibson Les Paul picked up."
        );
    }

    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Pick Up Guitar";
    }
}
