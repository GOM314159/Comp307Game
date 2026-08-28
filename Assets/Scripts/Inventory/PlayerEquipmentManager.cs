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
            Debug.LogError(
                "[Equipment] PlayerInventory not assigned."
            );

            return;
        }

        InventorySlot slot =
            inventory.GetSelectedSlot();


        // -----------------------------------------------------
        // EMPTY SLOT
        // -----------------------------------------------------

        if (slot == null || slot.IsEmpty())
        {
            UnequipWeapon();
            return;
        }


        // -----------------------------------------------------
        // CHECK WEAPON
        // -----------------------------------------------------

        if (!(slot.item is WeaponItem weapon))
        {
            UnequipWeapon();

            DebugEquipment(
                "Selected item is not a weapon."
            );

            return;
        }


        // -----------------------------------------------------
        // SAME WEAPON
        // -----------------------------------------------------

        if (currentWeapon == weapon &&
            currentWeaponObject != null)
        {
            return;
        }


        // -----------------------------------------------------
        // REMOVE CURRENT WEAPON
        // -----------------------------------------------------

        UnequipWeapon();


        // -----------------------------------------------------
        // EQUIP NEW WEAPON
        // -----------------------------------------------------

        if (weapon is AxeItem axe)
        {
            EquipAxe(axe);
            return;
        }


        DebugEquipment(
            "Weapon does not have an equipped prefab."
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