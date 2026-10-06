using UnityEngine;

[CreateAssetMenu(
    fileName = "Pan",
    menuName = "Inventory/Items/Weapon/Melee/Gacha/Pan"
)]
public class PanItem : MeleeWeaponItem
{
    [Header("Pan")]
    [SerializeField] private float panDamage = 999999f;
    [SerializeField] private float panRange = 3.5f;

    protected override void OnEnable()
    {
        base.OnEnable();
        weaponType = WeaponType.Melee;
        itemName = "Pan";
        stackable = false;
        maxStackSize = 1;
        weaponDamage = panDamage;
        attackRange = panRange;
    }
}
