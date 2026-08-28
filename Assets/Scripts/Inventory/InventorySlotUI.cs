using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    private InventoryUI inventoryUI;

    private void Awake()
    {
        inventoryUI =
            GetComponentInParent<InventoryUI>();
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[InventorySlotUI] OnDrop called on: {gameObject.name}"
        );

        if (eventData.pointerDrag == null)
        {
            Debug.LogWarning(
                "[InventorySlotUI] No pointerDrag found."
            );

            return;
        }

        InventoryItem draggedItem =
            eventData.pointerDrag
                .GetComponent<InventoryItem>();

        if (draggedItem == null)
        {
            Debug.LogWarning(
                "[InventorySlotUI] Dragged object has no InventoryItem."
            );

            return;
        }

        if (inventoryUI == null)
        {
            Debug.LogError(
                "[InventorySlotUI] Could not find InventoryUI."
            );

            return;
        }

        inventoryUI.HandleItemDrop(
            draggedItem,
            this
        );
    }
}