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

    [Header("Permanent Items")]

    [Tooltip(
    "The letter permanently given from Great Uncle Zihaad "
    + "at the beginning of a new game."
)]
    [SerializeField]
    private Letter startingLetter;

    [SerializeField]
    private ExodiaPieceItem startingExodiaRightLeg;

    [Header("Equipment")]

    [SerializeField]
    private PlayerEquipmentManager equipmentManager;

    [Header("Inventory UI")]
    [SerializeField] private GameObject inventoryUI;

    [SerializeField] private GameObject inventoryCloseButton;

    [SerializeField]
    private InventoryUI inventoryUIController;

    private GameStateManager.GameMode previousGameMode;
    private PlayerStateManager.PlayerState previousPlayerState;

    private PlayerStateManager playerStateManager;

    private bool inventoryOpen;

    [System.Serializable]
    public class InventorySnapshotSlot
    {
        public ItemData item;
        public int quantity;

        public InventorySnapshotSlot(
            ItemData item,
            int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }
    }

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

    [Header("Toolbar Cycling")]
    [SerializeField] private float gamepadInitialDelay = 0.35f;
    [SerializeField] private float gamepadRepeatRate = 0.08f;

    private float leftShoulderTimer = 0f;
    private float rightShoulderTimer = 0f;

    // INPUT

    private PlayerControls controls;

    // INITIALIZATION

    private void Awake()
    {
        controls = new PlayerControls();

        if (equipmentManager == null)
        {
            equipmentManager =
                GetComponent<PlayerEquipmentManager>();
        }

        //ontrols.Player.Inventory.performed +=
          //  _ => ToggleInventory(); // Bind I / D-Pad Up to that action in the existing Input Actions asset.

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

        //GiveStartingLetter();
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
        Debug.Log("[Inventory] I KEY RECEIVED");


        // =========================================================
        // DON'T OPEN INVENTORY WHILE PAUSED
        // =========================================================

        if (playerStateManager != null &&
            playerStateManager.GetCurrentState() ==
            PlayerStateManager.PlayerState.PauseMenu)
        {
            Debug.Log(
                "[Inventory] Ignored I input because "
                + "Pause Menu is open."
            );

            return;
        }

        if (!playerStateManager.CanOpenInventory())
        {
            Debug.Log(
                "[Inventory] Cannot open inventory "
                + "in current PlayerState: "
                + playerStateManager.GetCurrentState()
            );

            return;
        }



        // =========================================================
        // INVENTORY
        // =========================================================

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

    // =========================================================
    // REFRESH INVENTORY UI
    // =========================================================

    private void RefreshInventoryUI()
    {
        if (inventoryUIController != null)
        {
            inventoryUIController.RefreshUI();
        }

    }

    public void OpenInventory()
    {
        if (inventoryOpen)
            return;

        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[Inventory] GameStateManager instance not found.");
            return;
        }

        if (playerStateManager == null)
        {
            Debug.LogWarning("[Inventory] PlayerStateManager not found.");
            return;
        }

        // =========================================================
        // SAVE CURRENT STATE
        // =========================================================

        previousPlayerState =
        playerStateManager.GetCurrentState();

        // =========================================================
        // CHANGE PLAYER STATE
        // =========================================================

        playerStateManager.SetState(
            PlayerStateManager.PlayerState.Inventory
        );


        // =========================================================
        // SHOW INVENTORY UI
        // =========================================================

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(true);

            Debug.Log(
                "[Inventory] UI activated: "
                + inventoryUI.name
                + " | ActiveSelf: "
                + inventoryUI.activeSelf
                + " | ActiveInHierarchy: "
                + inventoryUI.activeInHierarchy
            );
        }
        else
        {
            Debug.LogError(
                "[Inventory] inventoryUI IS NULL! Assign the Inventory UI object in the Player Inspector."
            );
        }

        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.SetActive(true);
        }

        // Refresh the UI after enabling it.
        if (inventoryUIController != null)
        {
            inventoryUIController.RefreshUI();
        }

        RefreshInventoryUI();

        inventoryOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameUICursorController.Instance != null &&
            GameInputDevice.LastUsedDeviceIsGamepad())
        {
            GameUICursorController.Instance
                .ResetControllerCursorForUI();
        }

        Debug.Log(
            "[Inventory] Opened."
            + " Game State: "
            + GameStateManager.Instance.GetGameMode()
            + " | Player State: "
            + playerStateManager.GetCurrentState()
        );
    }

    public bool IsInventoryOpen()
    {
        return inventoryOpen;
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


        // Restore PLAYER state.
        if (playerStateManager != null)
        {
            playerStateManager.SetState(
                previousPlayerState
            );
        }

        inventoryOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log(
            "[Inventory] Closed."
            + " Game State restored to: "
            + previousGameMode
            + " | Player State restored to: "
            + previousPlayerState
        );
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

        RefreshInventoryUI();
        RefreshSelectedEquipment();

        if (equipmentManager != null)
        {
            equipmentManager.EquipSelectedItem();
        }
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

            Debug.Log(
    $"[INVENTORY CHECK] " +
    $"slots[{i}] = {(slots[i] == null ? "NULL" : "EXISTS")} | " +
    $"item = {(slots[i]?.item == null ? "NULL" : slots[i].item.itemName)}"
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
                RefreshSelectedEquipment();

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

        if (slot.item is Letter)
        {
            Debug.Log(
                "[Inventory] Letter cannot be removed."
            );

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
        RefreshSelectedEquipment();

        return true;
    }
    // REMOVE ALL COPIES OF A SPECIFIC ITEM

    public bool RemoveItemData(ItemData item)
    {
        if (item == null)
            return false;

        bool removed = false;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot == null || slot.IsEmpty() || slot.item != item)
                continue;

            slots[i] = new InventorySlot(null, 0);
            removed = true;
        }

        if (removed)
        {
            selectedSlot = 0;
            RefreshInventoryUI();
            RefreshSelectedEquipment();
        }

        return removed;
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

    private void Update()
    {
        if (Keyboard.current != null &&
        Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }

        if (Gamepad.current != null)
        {
            // buttonNorth = Y on Xbox
            // buttonNorth = Triangle on PlayStation
            if (Gamepad.current.buttonNorth.wasPressedThisFrame)
            {
                GameInputDevice.SetLastUsedDevice(
                    Gamepad.current
                );

                ToggleInventory();
            }
        }

        UpdateToolbarSelection();
    }

    // =========================================================
    // TOOLBAR SLOT CYCLING
    // =========================================================

    private void UpdateToolbarSelection()
    {
        // -----------------------------------------------------
        // MOUSE SCROLL
        // -----------------------------------------------------

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (scroll > 0f)
            {
                SelectPreviousToolbarSlot();
            }
            else if (scroll < 0f)
            {
                SelectNextToolbarSlot();
            }
        }


        // -----------------------------------------------------
        // GAMEPAD
        // -----------------------------------------------------

        if (Gamepad.current != null)
        {
            HandleShoulderInput();
        }
    }

    private void HandleShoulderInput()
    {
        // -----------------------------------------------------
        // LEFT SHOULDER = PREVIOUS SLOT
        // -----------------------------------------------------

        if (Gamepad.current.leftShoulder.isPressed)
        {
            if (leftShoulderTimer <= 0f)
            {
                SelectPreviousToolbarSlot();

                // First press has a longer delay.
                if (leftShoulderTimer == 0f)
                {
                    leftShoulderTimer = gamepadInitialDelay;
                }
                else
                {
                    leftShoulderTimer = gamepadRepeatRate;
                }
            }
            else
            {
                leftShoulderTimer -= Time.deltaTime;
            }
        }
        else
        {
            leftShoulderTimer = 0f;
        }


        // -----------------------------------------------------
        // RIGHT SHOULDER = NEXT SLOT
        // -----------------------------------------------------

        if (Gamepad.current.rightShoulder.isPressed)
        {
            if (rightShoulderTimer <= 0f)
            {
                SelectNextToolbarSlot();

                // First press has a longer delay.
                if (rightShoulderTimer == 0f)
                {
                    rightShoulderTimer = gamepadInitialDelay;
                }
                else
                {
                    rightShoulderTimer = gamepadRepeatRate;
                }
            }
            else
            {
                rightShoulderTimer -= Time.deltaTime;
            }
        }
        else
        {
            rightShoulderTimer = 0f;
        }
    }



    // =========================================================
    // PREVIOUS TOOLBAR SLOT
    // =========================================================

    private void SelectPreviousToolbarSlot()
    {
        selectedSlot--;

        if (selectedSlot < 0)
        {
            selectedSlot = toolbarSize - 1;
        }

        Debug.Log(
            "[Inventory] Active toolbar slot changed LEFT -> "
            + selectedSlot
            + " / "
            + (toolbarSize - 1)
        );

        RefreshInventoryUI();
        RefreshSelectedEquipment();

        if (equipmentManager != null)
        {
            equipmentManager.EquipSelectedItem();
        }
    }


    // =========================================================
    // NEXT TOOLBAR SLOT
    // =========================================================

    private void SelectNextToolbarSlot()
    {
        selectedSlot++;

        if (selectedSlot >= toolbarSize)
        {
            selectedSlot = 0;
        }

        Debug.Log(
            "[Inventory] Active toolbar slot changed RIGHT -> "
            + selectedSlot
            + " / "
            + (toolbarSize - 1)
        );

        RefreshInventoryUI();
        RefreshSelectedEquipment();

        if (equipmentManager != null)
        {
            equipmentManager.EquipSelectedItem();
        }
    }

    public bool SwapSlots(
    int firstIndex,
    int secondIndex)
    {
        if (firstIndex < 0 ||
            firstIndex >= slots.Count)
        {
            Debug.Log(
       $"[Inventory] Swapped slot " +
       $"{firstIndex} with slot " +
       $"{secondIndex}"
   );
            return false;

        }

        if (secondIndex < 0 ||
            secondIndex >= slots.Count)
        {
            Debug.Log(
       $"[Inventory] Swapped slot " +
       $"{firstIndex} with slot " +
       $"{secondIndex}"
   );
            return false;
        }

        if (firstIndex == secondIndex)
        {
            return false;
        }

        InventorySlot firstSlot =
            slots[firstIndex];

        InventorySlot secondSlot =
            slots[secondIndex];

        slots[firstIndex] =
            secondSlot;

        slots[secondIndex] =
            firstSlot;

        Debug.Log(
            $"[Inventory] Swapped slot " +
            $"{firstIndex} with slot " +
            $"{secondIndex}"
        );

        RefreshInventoryUI();
        RefreshSelectedEquipment();

        return true;
    }
    // =========================================================
    // EQUIPMENT SYNC
    // =========================================================

    private void RefreshSelectedEquipment()
    {
        if (equipmentManager == null)
        {
            return;
        }

        equipmentManager.EquipSelectedItem();
    }

    // =========================================================
    // RESET SUPPORT
    // =========================================================

    public void ClearInventory()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] =
                new InventorySlot(
                    null,
                    0
                );
        }

        selectedSlot = 0;

        RefreshInventoryUI();

        RefreshSelectedEquipment();

        Debug.Log(
            "[Inventory] Inventory cleared."
        );
    }


    // =========================================================
    // CREATE SNAPSHOT
    // =========================================================

    public List<InventorySnapshotSlot>
        CreateSnapshot()
    {
        List<InventorySnapshotSlot> snapshot =
            new List<InventorySnapshotSlot>();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot =
                slots[i];

            if (slot == null)
            {
                snapshot.Add(
                    new InventorySnapshotSlot(
                        null,
                        0
                    )
                );

                continue;
            }

            snapshot.Add(
                new InventorySnapshotSlot(
                    slot.item,
                    slot.quantity
                )
            );
        }

        return snapshot;
    }


    // =========================================================
    // RESTORE SNAPSHOT
    // =========================================================

    public void RestoreSnapshot(
        List<InventorySnapshotSlot> snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning(
                "[Inventory] Cannot restore null snapshot."
            );

            return;
        }

        slots.Clear();

        for (int i = 0; i < snapshot.Count; i++)
        {
            InventorySnapshotSlot snapshotSlot =
                snapshot[i];

            if (snapshotSlot == null)
            {
                slots.Add(
                    new InventorySlot(
                        null,
                        0
                    )
                );

                continue;
            }

            slots.Add(
                new InventorySlot(
                    snapshotSlot.item,
                    snapshotSlot.quantity
                )
            );
        }

        selectedSlot = 0;

        RefreshInventoryUI();

        RefreshSelectedEquipment();

        Debug.Log(
            "[Inventory] Inventory snapshot restored."
        );
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

    // =========================================================
    // STARTING LETTER
    // =========================================================

    public void GiveStartingLetter()
    {
        if (startingLetter == null)
        {
            Debug.LogError(
                "[Inventory] Starting Letter has not "
                + "been assigned in the Inspector."
            );

            return;
        }


        if (HasItem(startingLetter))
        {
            Debug.Log(
                "[Inventory] Player already has the letter."
            );

            return;
        }


        bool added =
            AddItem(
                startingLetter,
                1
            );


        if (added)
        {
            Debug.Log(
                "[Inventory] Starting letter added."
            );
        }
        else
        {
            Debug.LogError(
                "[Inventory] Could not add starting letter. "
                + "Inventory may be full."
            );
        }
    }

    // =========================================================
    // STARTING EXODIA RIGHT LEG
    // =========================================================

    public void GiveStartingExodiaRightLeg()
    {
        if (startingExodiaRightLeg == null)
        {
            Debug.LogError(
                "[Inventory] Starting Exodia Right Leg "
                + "has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // ADD TO INVENTORY
        // -----------------------------------------------------

        if (!HasItem(startingExodiaRightLeg))
        {
            bool added =
                AddItem(
                    startingExodiaRightLeg,
                    1
                );


            if (!added)
            {
                Debug.LogError(
                    "[Inventory] Could not add the "
                    + "starting Exodia Right Leg."
                );

                return;
            }
        }


        // -----------------------------------------------------
        // REGISTER WITH GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Inventory] GameStateManager is missing."
            );

            return;
        }


        GameStateManager.Instance.RegisterExodiaPiece(
            GameStateManager.ExodiaPiece.RightLeg
        );


        Debug.Log(
            "[Inventory] Starting Exodia Right Leg given."
        );
    }
}