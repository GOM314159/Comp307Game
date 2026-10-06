using UnityEngine;

[CreateAssetMenu(
    fileName = "Katana",
    menuName = "Inventory/Items/Weapon/Melee/Gacha/Katana"
)]
public class KatanaItem : MeleeWeaponItem
{
    [Header("Katana")]
    [SerializeField] private float katanaDamage = 25f;
    [SerializeField] private float katanaRange = 6f;

    protected override void OnEnable()
    {
        base.OnEnable();
        weaponType = WeaponType.Melee;
        itemName = "Katana";
        stackable = false;
        maxStackSize = 1;
        weaponDamage = katanaDamage;
        attackRange = katanaRange;
    }
}
