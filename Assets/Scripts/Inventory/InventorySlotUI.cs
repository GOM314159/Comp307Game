using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    private InventoryUI inventoryUI;

    [Header("Slot Background")]
    [SerializeField]
    private Image slotBackground;

    [Header("Outline Colours")]
    [SerializeField]
    private Color normalOutlineColour = Color.black;

    [SerializeField]
    private Color activeOutlineColour = new Color(0.58f, 0.18f, 0.95f, 1f);

    [SerializeField, Min(0f)]
    private float outlineDistance = 7f;

    private Outline slotOutline;

    private void Awake()
    {
        inventoryUI = GetComponentInParent<InventoryUI>();

        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        ResolveOutline();

        if (slotOutline != null)
            slotOutline.effectDistance = new Vector2(outlineDistance, outlineDistance);

        SetActive(false);
    }

    private void ResolveOutline()
    {
        if (slotOutline != null)
            return;

        // Prefer the Outline on the slot background itself. This avoids
        // accidentally tinting or outlining the item icon or another parent.
        if (slotBackground != null)
            slotOutline = slotBackground.GetComponent<Outline>();

        if (slotOutline == null)
            slotOutline = GetComponent<Outline>();

        if (slotOutline == null && slotBackground != null)
            slotOutline = slotBackground.GetComponentInChildren<Outline>(true);

        if (slotOutline == null)
            slotOutline = GetComponentInChildren<Outline>(true);
    }

    public void SetActive(bool active)
    {
        ResolveOutline();

        // Intentionally only change the outline. Do not change the slot
        // background Image colour or any InventoryItem/icon colour.
        if (slotOutline != null)
        {
            slotOutline.enabled = true;
            slotOutline.effectColor = active ? activeOutlineColour : normalOutlineColour;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[InventorySlotUI] OnDrop called on: {gameObject.name}");

        if (eventData.pointerDrag == null)
            return;

        InventoryItem draggedItem =
            eventData.pointerDrag.GetComponent<InventoryItem>();

        if (draggedItem == null || inventoryUI == null)
            return;

        inventoryUI.HandleItemDrop(draggedItem, this);
    }
}
