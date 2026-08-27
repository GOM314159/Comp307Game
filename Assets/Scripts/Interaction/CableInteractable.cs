using UnityEngine;

public class CableInteractable : WorldInteractable
{
    [Header("Cable")]

    [SerializeField]
    private bool canBeCut = true;

    [SerializeField]
    private GameObject objectToDisable;

    public override void Interact()
    {
        if (!canBeCut)
        {
            return;
        }

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError("[Cable] PlayerInventory not found.");
            return;
        }

        InventorySlot selectedSlot = inventory.GetSelectedSlot();

        if (selectedSlot == null || selectedSlot.IsEmpty())
        {
            Debug.Log("[Cable] No item equipped.");
            return;
        }

        if (!(selectedSlot.item is CableCutterItem))
        {
            Debug.Log("[Cable] You need a Cable Cutter to cut this cable.");
            return;
        }

        CutCable();
    }

    private void CutCable()
    {
        canBeCut = false;

        Debug.Log("[Cable] Cable has been cut.");

        if (objectToDisable != null)
        {
            objectToDisable.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public override string GetInteractionPrompt()
    {
        return "Cut Cable";
    }
}