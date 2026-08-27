using UnityEngine;

[CreateAssetMenu(
    fileName = "Gun",
    menuName = "Inventory/Items/Weapon/Ranged/Gun"
)]
public class GunItem : RangedWeaponItem
{
    // =========================================================
    // GUN SETTINGS
    // =========================================================

    [Header("Gun Settings")]

    [Tooltip("The physical gun prefab that appears in the player's hand.")]
    public GameObject equippedPrefab;


    [Tooltip("Damage dealt by the gun.")]
    [SerializeField]
    private float gunDamage = 10f;


    // =========================================================
    // INITIALIZATION
    // =========================================================

    protected override void OnEnable()
    {
        base.OnEnable();

        weaponType = WeaponType.Ranged;

        stackable = false;

        maxStackSize = 1;

        weaponDamage = gunDamage;
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public override float GetAttackDamage()
    {
        return weaponDamage;
    }


    // =========================================================
    // USE
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Inventory] Gun selected."
        );
    }
}