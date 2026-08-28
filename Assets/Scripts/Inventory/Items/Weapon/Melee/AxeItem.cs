using UnityEditor.UI;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Axe",
    menuName = "Inventory/Items/Weapon/Melee/Axe"
)]
public class AxeItem : MeleeWeaponItem
{
    // =========================================================
    // AXE SETTINGS
    // =========================================================

    [Header("Axe Settings")]

    [Tooltip("The physical axe prefab that appears in the player's hand done by Marmz.")]
    public GameObject equippedPrefab;


    [Tooltip("Damage dealt by the axe is 10.")]
    [SerializeField]
    private float axeDamage = 10f;


    // =========================================================
    // INITIALIZATION
    // =========================================================

    protected override void OnEnable()
    {
        base.OnEnable();

        // Make sure the item is always a melee weapon.
        weaponType = WeaponType.Melee;

        // Set the item name to "Axe".
        SetItemName("Axe");

        // Axe is never stackable.
        stackable = false;

        maxStackSize = 1;

        // Set the weapon damage.
        weaponDamage = axeDamage;

        

    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public override float GetAttackDamage()
    {
        return 10f;
    }


    // =========================================================
    // USE
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Inventory] Axe selected."
        );
    }
}