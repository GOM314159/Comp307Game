using UnityEngine;

[CreateAssetMenu(
    fileName = "CableCutter",
    menuName = "Inventory/Items/Cable Cutter"
)]
public class CableCutterItem : InteractableItem
{
    [Header("Cable Cutter")]
    [Tooltip("The physical cable cutter prefab used when equipped.")]
    public GameObject equippedPrefab;

    protected override void OnEnable()
    {
        base.OnEnable();

        stackable = false;
        maxStackSize = 1;
    }

    public override void Use()
    {
        Debug.Log("[Cable Cutter] Cable Cutter used.");
    }
}