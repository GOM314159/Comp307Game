using UnityEngine;

[CreateAssetMenu(
    fileName = "New Interactable",
    menuName = "Inventory/Items/Interactable")]
public class InteractableItem : ItemData
{

    [Header("Interactable Settings")]
    // something happens or menu opens
    public string menuName;


    protected virtual void OnEnable()
    {
        itemType = ItemType.Interactable;
    }


    // =========================================================
    // RIGHT CLICK
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Inventory] Opening interactable menu: "
            + menuName);
    }
}