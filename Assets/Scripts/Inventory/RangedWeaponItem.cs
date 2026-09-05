using UnityEngine;

[CreateAssetMenu(
    fileName = "New Ranged Weapon",
    menuName = "Inventory/Items/Weapon/Ranged")]
public class RangedWeaponItem : WeaponItem
{

    [Header("Ranged Settings")]

    // Maximum distance the weapon can hit.
    public float range = 50f;


    // How many shots can be fired per second.
    public float fireRate = 5f;


    // Magazine capacity.
    // public int magazineSize = 12;


    // Current ammunition.
    [SerializeField]
    private int currentAmmo;


    protected override void OnEnable()
    {
        base.OnEnable();

        weaponType = WeaponType.Ranged;

        // currentAmmo = magazineSize;
    }


    // =========================================================
    // AMMO - Until we get to a point where we want to consider none infinite ammo
    // =========================================================

    //public int GetCurrentAmmo()
    //{
    //    return currentAmmo;
    //}


    //public int GetMagazineSize()
    //{
    //    return magazineSize;
    //}


    //public bool HasAmmo()
    //{
    //    return currentAmmo > 0;
    //}


    //public bool UseAmmo()
    //{
    //    if (currentAmmo <= 0)
    //    {
    //        return false;
    //    }

    //    currentAmmo--;

    //    return true;
    //}


    //public void Reload()
    //{
    //    currentAmmo = magazineSize;
    //}


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
            "[Inventory] Ranged weapon equipped/selected: "
            + itemName);
    }
}