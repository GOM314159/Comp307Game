using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Player")]

    [SerializeField]
    private PlayerInventory inventory;


    [SerializeField]
    private Transform weaponHoldPoint;


    // =========================================================
    // CURRENT WEAPON
    // =========================================================

    [Header("Current Weapon")]

    [SerializeField]
    private GameObject currentWeaponObject;


    [SerializeField]
    private WeaponItem currentWeapon;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugEquipment = true;


    // =========================================================
    // EQUIP SELECTED ITEM
    // =========================================================

    public void EquipSelectedItem()
    {
        if (inventory == null)
        {
            inventory =
                GetComponent<PlayerInventory>();
        }

        if (inventory == null)
        {
            Debug.LogWarning(
                "[Equipment] PlayerInventory not found."
            );

            return;
        }

        InventorySlot selectedSlot =
            inventory.GetSelectedSlot();

        if (selectedSlot == null ||
            selectedSlot.IsEmpty())
        {
            UnequipWeapon();

            Debug.Log(
                "[Equipment] No weapon equipped."
            );

            return;
        }

        if (selectedSlot.item is WeaponItem weapon)
        {
            currentWeapon = weapon;

            Debug.Log(
                "[Equipment] Equipped weapon: "
                + weapon.itemName
            );

            return;
        }

        UnequipWeapon();

        Debug.Log(
            "[Equipment] Selected item is not a weapon."
        );
    }


    // =========================================================
    // EQUIP AXE
    // =========================================================

    private void EquipAxe(AxeItem axe)
    {
        if (axe.equippedPrefab == null)
        {
            Debug.LogError(
                "[Equipment] Axe does not have "
                + "an equipped prefab assigned."
            );

            return;
        }

        if (weaponHoldPoint == null)
        {
            Debug.LogError(
                "[Equipment] Weapon Hold Point "
                + "has not been assigned."
            );

            return;
        }


        currentWeaponObject =
            Instantiate(
                axe.equippedPrefab,
                weaponHoldPoint
            );


        currentWeaponObject.transform.localPosition =
            Vector3.zero;


        currentWeaponObject.transform.localRotation =
            Quaternion.identity;


        currentWeaponObject.transform.localScale =
            Vector3.one;


        currentWeapon =
            axe;


        DebugEquipment(
            "Equipped Axe. Damage: "
            + axe.GetAttackDamage()
        );
    }


    // =========================================================
    // UNEQUIP
    // =========================================================

    public void UnequipWeapon()
    {
        if (currentWeaponObject != null)
        {
            Destroy(
                currentWeaponObject
            );
        }

        currentWeaponObject = null;

        currentWeapon = null;
    }


    // =========================================================
    // GET CURRENT WEAPON
    // =========================================================

    public WeaponItem GetCurrentWeapon()
    {
        return currentWeapon;
    }


    public GameObject GetCurrentWeaponObject()
    {
        return currentWeaponObject;
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugEquipment(
        string message)
    {
        if (!debugEquipment)
        {
            return;
        }

        Debug.Log(
            "[Equipment] "
            + message
        );
    }
}