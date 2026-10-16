using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

public class PlayerEquipmentManager : MonoBehaviour
{
    [Serializable]
    public class WeaponVisualMapping
    {
        [Tooltip("Exact ItemData asset used by the inventory.")]
        public WeaponItem item;
        [Tooltip("Existing scene model to show while this item is selected. Can be assigned across scenes.")]
        public GameObject heldModel;
    }

    [Header("Weapon Model Mapping")]
    [Tooltip("Assign one mapping per weapon. The matching held model is enabled and all other mapped models are hidden.")]
    [SerializeField] private List<WeaponVisualMapping> weaponVisuals = new List<WeaponVisualMapping>();
    [Tooltip("If enabled, a weapon's equippedPrefab is instantiated under Weapon Hold Point when no mapped scene model exists.")]
    [SerializeField] private bool instantiateEquippedPrefabWhenUnmapped = true;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Audio & Animation Integration")]
    [SerializeField] private AxeSwingInput axeSwingInput;

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

    private GameObject currentMappedWeaponObject;


    [SerializeField]
    private WeaponItem currentWeapon;




    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugEquipment = true;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        // Automatically find AxeSwingInput on Nephew if not manually assigned
        if (axeSwingInput == null)
        {
            axeSwingInput = GetComponent<AxeSwingInput>();
        }
    }


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

        if (selectedSlot == null || selectedSlot.IsEmpty())
        {
            UnequipWeapon();
            SetMappedVisual(null);
            Debug.Log("[Equipment] No weapon equipped.");
            return;
        }

        if (selectedSlot.item is WeaponItem weapon)
        {
            currentWeapon = weapon;
            if (currentWeaponObject != null)
            {
                Destroy(currentWeaponObject);
                currentWeaponObject = null;
            }
            bool mapped = SetMappedVisual(weapon);

            if (!mapped && instantiateEquippedPrefabWhenUnmapped)
                TryInstantiateWeaponPrefab(weapon);

            SyncWeaponAudio(weapon);

            Debug.Log("[Equipment] Equipped weapon: " + weapon.itemName);
            return;
        }

        UnequipWeapon();
        SetMappedVisual(null);

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

    private void SyncWeaponAudio(WeaponItem weapon)
    {
        if (axeSwingInput == null || weapon == null) return;

        string itemName = weapon.itemName.ToLower();

        if (itemName.Contains("katana"))
        {
            axeSwingInput.SetActiveWeapon(AxeSwingInput.ActiveWeapon.Katana);
        }
        else if (itemName.Contains("guitar"))
        {
            axeSwingInput.SetActiveWeapon(AxeSwingInput.ActiveWeapon.Guitar);
        }
        else if (itemName.Contains("pan"))
        {
            axeSwingInput.SetActiveWeapon(AxeSwingInput.ActiveWeapon.Pan);
        }
        else if (itemName.Contains("stormbreaker") || itemName.Contains("storm"))
        {
            axeSwingInput.SetActiveWeapon(AxeSwingInput.ActiveWeapon.StormBreaker);
        }
        else if (itemName.Contains("gun"))
        {
            axeSwingInput.SetActiveWeapon(AxeSwingInput.ActiveWeapon.Gun);
        }
        else
        {
            // Default fallback to Axe
            axeSwingInput.SetActiveWeapon(AxeSwingInput.ActiveWeapon.Axe);
        }
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
        SetMappedVisual(null);
    }


    private bool SetMappedVisual(WeaponItem selected)
    {
        bool found = false;
        currentMappedWeaponObject = null;

        foreach (WeaponVisualMapping mapping in weaponVisuals)
        {
            if (mapping == null || mapping.heldModel == null)
                continue;

            // Does this model represent the equipped item?
            bool matchingWeapon =
                selected != null && mapping.item == selected;

            // Is this model's character hierarchy active?
            Transform parent = mapping.heldModel.transform.parent;

            bool characterActive =
                parent != null && parent.gameObject.activeInHierarchy;

            bool shouldEnable = matchingWeapon && characterActive;

            if (mapping.heldModel.activeSelf != shouldEnable)
            {
                mapping.heldModel.SetActive(shouldEnable);
            }

            // Remember the weapon on the active character.
            if (shouldEnable)
            {
                currentMappedWeaponObject = mapping.heldModel;
            }

            // A matching mapping exists even if its character
            // is currently inactive.
            if (matchingWeapon)
            {
                found = true;
            }
        }

        return found;
    }

    public void RefreshWeaponVisuals()
    {
        SetMappedVisual(currentWeapon);
    }


    private void TryInstantiateWeaponPrefab(WeaponItem weapon)
    {
        if (weapon == null || weaponHoldPoint == null) return;

        GameObject prefab = null;
        // Supports weapon-specific equippedPrefab fields declared on subclasses
        // (AxeItem, GunItem, and future weapon assets) without requiring duplicate fields.
        Type type = weapon.GetType();
        while (type != null && prefab == null)
        {
            FieldInfo field = type.GetField("equippedPrefab",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null && typeof(GameObject).IsAssignableFrom(field.FieldType))
                prefab = field.GetValue(weapon) as GameObject;
            type = type.BaseType;
        }

        if (prefab == null) return;
        if (currentWeaponObject != null) Destroy(currentWeaponObject);
        currentWeaponObject = Instantiate(prefab, weaponHoldPoint, false);
        currentWeaponObject.transform.localPosition = Vector3.zero;
        currentWeaponObject.transform.localRotation = Quaternion.identity;
        currentWeaponObject.transform.localScale = Vector3.one;
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
        if (currentMappedWeaponObject != null)
            return currentMappedWeaponObject;

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