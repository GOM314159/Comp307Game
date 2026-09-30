using UnityEngine;

[CreateAssetMenu(
    fileName = "TheOneRing",
    menuName = "Inventory/Items/Gacha/The One Ring"
)]
public class OneRingItem : ItemData
{
    private void OnEnable()
    {
        itemType = ItemType.Interactable;
        itemName = "The One Ring";
        stackable = false;
        maxStackSize = 1;
    }

    public override void Use()
    {
        // TBD - figure out what this thing actually does later
        Debug.Log("[Inventory] The One Ring used. Still TBD.");
    }
}
