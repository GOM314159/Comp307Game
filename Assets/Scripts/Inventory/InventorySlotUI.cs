using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    private InventoryUI inventoryUI;
    private InventoryItem inventoryItem;

    private void Awake()
    {
        inventoryUI =
            GetComponentInParent<InventoryUI>();

        inventoryItem =
            GetComponentInChildren<InventoryItem>();
    }

    public void OnDrop(
        PointerEventData eventData)
    {

        if (eventData.pointerDrag == null)
            return;

        InventoryItem draggedItem =
            eventData.pointerDrag
                .GetComponent<InventoryItem>();

        if (draggedItem == null)
            return;

        if (inventoryItem == null)
        {
            inventoryItem =
                GetComponentInChildren<InventoryItem>();
        }

        if (inventoryItem == null)
            return;

        if (draggedItem == inventoryItem)
            return;

        if (inventoryUI == null)
        {
            Debug.LogError(
                "[InventorySlotUI] Could not find InventoryUI."
            );

            return;
        }

        inventoryUI.HandleItemDrop(
            draggedItem,
            inventoryItem
        );
    }

}