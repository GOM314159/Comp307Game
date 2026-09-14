using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    private InventoryUI inventoryUI;


    [Header("Slot Background")]
    [SerializeField]
    private Image slotBackground;


    [Header("Slot Colours")]
    [SerializeField]
    private Color normalColour = Color.white;

    [SerializeField]
    private Color activeColour = Color.yellow;


    private void Awake()
    {
        inventoryUI =
            GetComponentInParent<InventoryUI>();


        if (slotBackground == null)
        {
            slotBackground =
                GetComponent<Image>();
        }


        SetActive(false);
    }


    public void SetActive(bool active)
    {
        if (slotBackground == null)
        {
            return;
        }


        slotBackground.color =
            active
                ? activeColour
                : normalColour;
    }


    public void OnDrop(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[InventorySlotUI] OnDrop called on: {gameObject.name}"
        );


        if (eventData.pointerDrag == null)
        {
            return;
        }


        InventoryItem draggedItem =
            eventData.pointerDrag
                .GetComponent<InventoryItem>();


        if (draggedItem == null)
        {
            return;
        }


        if (inventoryUI == null)
        {
            return;
        }


        inventoryUI.HandleItemDrop(
            draggedItem,
            this
        );
    }
}