using UnityEngine;

[CreateAssetMenu(
    fileName = "New Melee Weapon",
    menuName = "Inventory/Items/Weapon/Melee")]
public class MeleeWeaponItem : WeaponItem
{
  

    [Header("Melee Settings")]

    // Maximum distance at which the melee attack
    // can hit something.
    //
    // Adjust melee attack range here.
    public float attackRange = 2f;


    // How quickly the player can perform another attack.
    //
    // Adjust melee attack cooldown here.
    public float attackCooldown = 0.5f;


    protected override void OnEnable()
    {
        base.OnEnable();

        weaponType = WeaponType.Melee;
    }


    // =========================================================
    // ATTACK
    // =========================================================

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
            "[Inventory] Melee weapon equipped/selected: "
            + itemName);
    }
}