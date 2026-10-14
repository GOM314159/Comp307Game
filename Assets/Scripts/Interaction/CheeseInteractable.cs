using UnityEngine;

/// <summary>World pickup that adds a cheese consumable to the player's inventory.</summary>
public class CheeseInteractable : WorldInteractable
{
    [Header("Cheese Pickup")]
    [SerializeField] private CheeseItem cheeseItem;
    [SerializeField, Min(1)] private int amount = 1;
    [Tooltip("Automatically finds the persistent player's inventory if empty.")]
    [SerializeField] private PlayerInventory playerInventory;

    protected override void Awake()
    {
        base.Awake();
        ResolveInventory();
    }

    private void ResolveInventory()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    public override void Interact()
    {
        if (!CanInteract()) return;
        if (cheeseItem == null)
        {
            Debug.LogError("[CheeseInteractable] Assign a CheeseItem asset in the Inspector.", this);
            return;
        }

        ResolveInventory();
        if (playerInventory == null)
        {
            Debug.LogError("[CheeseInteractable] PlayerInventory could not be found.", this);
            return;
        }

        if (!playerInventory.AddItem(cheeseItem, amount))
        {
            Debug.LogWarning("[CheeseInteractable] Inventory is full; cheese pickup remains in the world.", this);
            return;
        }

        base.Interact();
        gameObject.SetActive(false);
    }

    public override string GetInteractionPrompt() => "Take Cheese";
}
