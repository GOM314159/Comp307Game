using UnityEngine;

[CreateAssetMenu(
    fileName = "Stormbreaker",
    menuName = "Inventory/Items/Weapon/Melee/Gacha/Stormbreaker"
)]
public class StormbreakerItem : MeleeWeaponItem
{
    [Header("Stormbreaker")]
    [SerializeField] private float stormbreakerDamage = 40f;
    [SerializeField] private float stormbreakerRange = 7f;

    protected override void OnEnable()
    {
        base.OnEnable();
        weaponType = WeaponType.Melee;
        itemName = "Stormbreaker";
        stackable = false;
        maxStackSize = 1;
        weaponDamage = stormbreakerDamage;
        attackRange = stormbreakerRange;
    }
}
