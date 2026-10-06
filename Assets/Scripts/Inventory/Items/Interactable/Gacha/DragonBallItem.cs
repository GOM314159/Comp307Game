using UnityEngine;

[CreateAssetMenu(
    fileName = "DragonBall",
    menuName = "Inventory/Items/Gacha/Dragon Ball"
)]
public class DragonBallItem : ItemData
{
    [Header("Dragon Ball")]
    [Range(1, 7)]
    public int dragonBallNumber = 1;

    private void OnEnable()
    {
        itemType = ItemType.Interactable;
        stackable = false;
        maxStackSize = 1;
        itemName = "Dragon Ball " + dragonBallNumber;
    }
}
