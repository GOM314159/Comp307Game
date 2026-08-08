using UnityEngine;

[CreateAssetMenu(
    fileName = "New Consumable",
    menuName = "Inventory/Items/Consumable")]
public class ConsumableItem : ItemData
{
    [Header("Consumable Settings")]

    public float effectAmount = 1f; //for if many things need to happen


    protected virtual void OnEnable()
    {
        itemType = ItemType.Consumable;
    }


    // =========================================================
    // RIGHT CLICK
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Inventory] Consumed: "
            + itemName);
    }
}