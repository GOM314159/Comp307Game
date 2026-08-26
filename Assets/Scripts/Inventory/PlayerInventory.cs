using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{

    [Header("Inventory Settings")]

    // Adjust inventory size here.
    [SerializeField]
    private int toolbarSize = 7;

    [SerializeField]
    private int mainInventorySize = 21;

    [Header("Inventory UI")]

    [Header("Inventory UI")]
    [SerializeField] private GameObject inventoryUI;

    [SerializeField] private GameObject inventoryCloseButton;

    [SerializeField]
    private InventoryUI inventoryUIController;

    private GameStateManager.GameMode previousGameMode;
    private PlayerStateManager.PlayerState previousPlayerState;

    private PlayerStateManager playerStateManager;

    private bool inventoryOpen;

    [Header("Inventory")]

    [SerializeField]
    private List<InventorySlot> slots =
        new List<InventorySlot>();

    // SELECTED SLOT

    [Header("Selected Item")]

    // The slot currently being used.
    [Header("Selected Toolbar Slot")]

    [SerializeField]
    private int selectedSlot = 0;
    // BASE ATTACK

    [Header("Attack Settings")]

    // Damage when attacking without a weapon.
    //
    // Adjust base attack damage here.
    [SerializeField] private float baseAttackDamage = 1f;

    // INPUT

    private PlayerControls controls;

    // INITIALIZATION

    private void Awake()
    {
        controls = new PlayerControls();

        // Left click can be moved to seperate thing later

        controls.Player.Attack.performed +=
            _ => Attack();

        controls.Player.Inventory.performed +=
            _ => ToggleInventory(); // Bind I / D-Pad Up to that action in the existing Input Actions asset.

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.SetActive(false);
        }

        // Right click handled in interaction

        // CREATE INVENTORY SLOTS

        slots.Clear();

        int totalSlots =
            toolbarSize +
            mainInventorySize;

        for (int i = 0; i < totalSlots; i++)
        {
            slots.Add(
                new InventorySlot(null, 0));
        }

        playerStateManager =
    GetComponent<PlayerStateManager>();

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        if (playerStateManager == null)
        {
            Debug.LogWarning(
                "[Inventory] PlayerStateManager not found on Player."
            );
        }


        Debug.Log(
            "[Inventory] Created with "
            + totalSlots
            + " slots.");
    }

    // ENABLE / DISABLE

    private void OnEnable()
    {
        if (controls != null)
        {
            controls.Enable();
        }
    }


    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Disable();
        }
    }


    // =========================================================
    // INVENTORY OPEN / CLOSE
    // =========================================================

    private void ToggleInventory()
    {
        if (inventoryOpen)
        {
            CloseInventory();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            OpenInventory();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OpenInventory()
    {
        if (inventoryOpen)
        {
            return;
        }



        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning(
                "[Inventory] GameStateManager instance not found."
            );

            return;
        }

        if (playerStateManager == null)
        {
            Debug.LogWarning(
                "[Inventory] PlayerStateManager not found."
            );

            return;
        }

        // Save the states before changing them.
        previousGameMode =
            GameStateManager.Instance.GetGameMode();

        previousPlayerState =
            playerStateManager.GetCurrentState();

        // Change GAME state.
        GameStateManager.Instance.SetGameMode(
            GameStateManager.GameMode.Paused
        );

        // Change PLAYER state.
        playerStateManager.SetState(
            PlayerStateManager.PlayerState.PauseMenu
        );

        // Show inventory.
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(true);
        }
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.SetActive(true);
        }
        if (inventoryUIController != null)
        {
            inventoryUIController.RefreshUI();
        }

        inventoryOpen = true;

        Debug.Log(
            "[Inventory] Opened."
            + " Game State: Paused"
            + " | Player State: PauseMenu"
        );
    }

    public void CloseInventory()
    {
        if (!inventoryOpen)
        {
            return;
        }

        // Hide inventory.
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        // Hide close button.
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.SetActive(false);
        }

        // Restore GAME state.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                previousGameMode
            );
        }

        // Restore PLAYER state.
        if (playerStateManager != null)
        {
            playerStateManager.SetState(
                previousPlayerState
            );
        }

        inventoryOpen = false;

        Debug.Log(
            "[Inventory] Closed."
            + " Game State restored to: "
            + previousGameMode
            + " | Player State restored to: "
            + previousPlayerState
        );
    }

    public bool IsInventoryOpen()
    {
        return inventoryOpen;
    }
    // =========================================================
    // INVENTORY SIZE
    // =========================================================

    public int GetToolbarSize()
    {
        return toolbarSize;
    }


    public int GetMainInventorySize()
    {
        return mainInventorySize;
    }


    public int GetTotalInventorySize()
    {
        return slots.Count;
    }
    // ATTACK

    private void Attack()
    {
        if (!playerStateManager.CanAttack())
        {
            return;
        }
        InventorySlot slot =
            GetSelectedSlot();

        // NO ITEM

        if (slot == null || slot.IsEmpty())
        {
            Debug.Log(
                "[Inventory] Attack: Empty hands.");

            DealAttackDamage(
                baseAttackDamage);

            return;
        }

        // WEAPON

        if (slot.item is WeaponItem weapon)
        {
            float damage =
                weapon.GetAttackDamage();


            Debug.Log(
                "[Inventory] Attack with: "
                + weapon.itemName
                + " | Type: "
                + weapon.weaponType
                + " | Damage: "
                + damage);

            // MELEE

            if (weapon is MeleeWeaponItem)
            {
                PerformMeleeAttack(
                    weapon as MeleeWeaponItem);

                return;
            }

            // RANGED

            if (weapon is RangedWeaponItem)
            {
                PerformRangedAttack(
                    weapon as RangedWeaponItem);

                return;
            }


            // Fallback
            DealAttackDamage(damage);

            return;
        }
        // NON-WEAPON

        Debug.Log(
            "[Inventory] "
            + slot.item.itemName
            + " is not a weapon.");

        // The player can still perform the base attack.
        DealAttackDamage(
            baseAttackDamage);
    }


    // MELEE ATTACK

    private void PerformMeleeAttack(
        MeleeWeaponItem weapon)
    {
        Debug.Log(
            "[Inventory] MELEE ATTACK"
            + " | Weapon: "
            + weapon.itemName
            + " | Damage: "
            + weapon.weaponDamage
            + " | Range: "
            + weapon.attackRange);


        // Actual melee hit detection will be added later. Please Sidd, detect the enemy, deal damage, handle cooldowns
    }


    // RANGED ATTACK

    private void PerformRangedAttack(
        RangedWeaponItem weapon)
    {
        Debug.Log(
            "[Inventory] RANGED ATTACK"
            + " | Weapon: "
            + weapon.itemName
            + " | Damage: "
            + weapon.weaponDamage
            + " | Ammo: "
            + weapon.GetCurrentAmmo());


        // Actual shooting will be added later. Please marmz firerate, damage, detect hit, projectile
    }


    // BASE ATTACK DAMAGE


    private void DealAttackDamage(
        float damage)
    {
        Debug.Log(
            "[Inventory] Attack dealt "
            + damage
            + " damage.");
    }


    // RIGHT CLICK / USE

    public void UseSelectedItem()
    {
        InventorySlot slot =
            GetSelectedSlot();

        // NO ITEM


        if (slot == null || slot.IsEmpty())
        {
            Debug.Log(
                "[Inventory] Nothing selected.");

            return;
        }


        ItemData item =
            slot.item;


        // WEAPON


        if (item is WeaponItem)
        {
            item.Use();

            return;
        }

        // CONSUMABLE


        if (item is ConsumableItem)
        {
            item.Use();


            // Remove one consumable after use.
            RemoveItem(
                selectedSlot,
                1);

            return;
        }

        // INTERACTABLE

        if (item is InteractableItem)
        {
            item.Use();

            return;
        }


        // FUTURE ITEM TYPES
        // If we add another ItemData type later,
        // its own Use() function will be called here.

        item.Use();
    }

    // GET SELECTED SLOT

    public InventorySlot GetSelectedSlot()
    {
        if (selectedSlot < 0 ||
            selectedSlot >= slots.Count)
        {
            return null;
        }


        return slots[selectedSlot];
    }

    public List<InventorySlot> GetSlots()
    {
        return slots;
    }

    // =========================================================
    // GET TOOLBAR SLOTS
    // =========================================================

    public InventorySlot GetToolbarSlot(
        int index)
    {
        if (index < 0 ||
            index >= toolbarSize)
        {
            return null;
        }


        return slots[index];
    }

    public InventorySlot GetMainInventorySlot(
        int index)
    {
        if (index < 0 ||
            index >= mainInventorySize)
        {
            return null;
        }


        int actualIndex =
            toolbarSize + index;

        return slots[actualIndex];
    }


    // =========================================================
    // SELECT TOOLBAR SLOT
    // =========================================================

    public void SelectSlot(
        int index)
    {
        if (index < 0 ||
            index >= toolbarSize)
        {
            return;
        }


        selectedSlot = index;


        Debug.Log(
            "[Inventory] Selected toolbar slot: "
            + selectedSlot
        );
    }


    public int GetSelectedSlotIndex()
    {
        return selectedSlot;
    }


    // =========================================================
    // SELECT SLOT
    // =========================================================

    

    // ADD ITEM

    public bool AddItem(
        ItemData item,
        int amount = 1)
    {
        if (item == null)
        {
            Debug.Log(
                "[Inventory] Cannot add null item.");

            return false;
        }


        if (amount <= 0)
        {
            return false;
        }

        // TRY TO STACK

        if (item.stackable)
        {
            for (int i = 0;
                i < slots.Count;
                i++)
            {
                InventorySlot slot =
                    slots[i];


                if (slot.item == item &&
                    slot.quantity < item.maxStackSize)
                {
                    int availableSpace =
                        item.maxStackSize -
                        slot.quantity;


                    int amountToAdd =
                        Mathf.Min(
                            amount,
                            availableSpace);


                    slot.quantity +=
                        amountToAdd;


                    amount -=
                        amountToAdd;


                    if (amount <= 0)
                    {
                        Debug.Log(
                            "[Inventory] Added "
                            + item.itemName);

                        return true;
                    }
                }
            }
        }

        // FIND EMPTY SLOT

        for (int i = 0;
            i < slots.Count;
            i++)
        {
            if (!slots[i].IsEmpty())
            {
                continue;
            }


            int amountToAdd;


            if (item.stackable)
            {
                amountToAdd =
                    Mathf.Min(
                        amount,
                        item.maxStackSize
                    );
            }
            else
            {
                amountToAdd = 1;
            }


            slots[i] =
                new InventorySlot(
                    item,
                    amountToAdd
                );


            amount -=
                amountToAdd;


            Debug.Log(
                "[Inventory] Added "
                + item.itemName
                + " to slot "
                + i
            );


            if (amount <= 0)
            {
                RefreshInventoryUI();

                return true;
            }
        }


        // =====================================================
        // INVENTORY FULL
        // =====================================================

        Debug.Log(
            "[Inventory] Inventory is full."
        );


        return false;
    }


    // REMOVE ITEM

    public bool RemoveItem(
        int slotIndex,
        int amount = 1)
    {
        if (slotIndex < 0 ||
            slotIndex >= slots.Count)
        {
            return false;
        }


        InventorySlot slot =
            slots[slotIndex];


        if (slot.IsEmpty())
        {
            return false;
        }


        slot.quantity -=
            amount;

        // SLOT IS NOW EMPTY

        if (slot.quantity <= 0)
        {
            slots[slotIndex] =
                new InventorySlot(
                    null,
                    0);
        }
        RefreshInventoryUI();

        return true;
    }
    // CHECK IF PLAYER HAS ITEM

    public bool HasItem(
        ItemData item)
    {
        if (item == null)
        {
            return false;
        }


        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item &&
                slot.quantity > 0)
            {
                return true;
            }
        }


        return false;
    }

    // GET TOTAL ITEM COUNT

    public int GetItemCount(
        ItemData item)
    {
        int total = 0;


        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item)
            {
                total +=
                    slot.quantity;
            }
        }


        return total;
    }

    
    private void RefreshInventoryUI()
    {
        if (inventoryUIController != null)
        {
            inventoryUIController.RefreshUI();
        }
    }

    // DEBUG
    public void PrintInventory()
    {
        Debug.Log(
            "========== INVENTORY =========="
        );


        for (int i = 0;
            i < slots.Count;
            i++)
        {
            InventorySlot slot =
                slots[i];


            string section;

            if (i < toolbarSize)
            {
                section = "TOOLBAR";
            }
            else
            {
                section = "MAIN";
            }


            if (slot.IsEmpty())
            {
                Debug.Log(
                    section
                    + " Slot "
                    + i
                    + ": EMPTY"
                );
            }
            else
            {
                Debug.Log(
                    section
                    + " Slot "
                    + i
                    + ": "
                    + slot.item.itemName
                    + " x"
                    + slot.quantity
                );
            }
        }


        Debug.Log(
            "================================"
        );
    }
}