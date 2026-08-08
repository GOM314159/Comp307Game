using UnityEngine;

public abstract class WeaponItem : ItemData
{
    public enum WeaponType
    {
        Melee,
        Ranged
    }
    [Header("Weapon Settings")]

    public WeaponType weaponType;


    // Weapon damage.
    // Adjust weapon damage here.
    public float weaponDamage = 1f;


    protected virtual void OnEnable()
    {
        itemType = ItemType.Weapon;
    }

    public override float GetAttackDamage()
    {
        return weaponDamage;
    }


    // =========================================================
    // RIGHT CLICK
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Inventory] Weapon selected: "
            + itemName);
    }
}