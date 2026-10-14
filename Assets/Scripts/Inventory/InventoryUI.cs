using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    // =========================================================
    // PLAYER INVENTORY
    // =========================================================

    [Header("Player Inventory")]

    [SerializeField]
    private PlayerInventory playerInventory;


    // =========================================================
    // MAIN INVENTORY UI
    // =========================================================

    [Header("Main Inventory UI")]

    [SerializeField]
    private List<InventoryItem> mainInventoryItems =
        new List<InventoryItem>();


    // =========================================================
    // TOOLBAR UI
    // =========================================================

    [Header("Toolbar UI")]

    [SerializeField]
    private List<InventoryItem> toolbarItems =
        new List<InventoryItem>();


    // =========================================================
    // START
    // =========================================================

    private void LateUpdate()
    {
        // Selection can change through keyboard/controller input without a
        // full inventory refresh. Keep only the currently selected slot purple.
        ResolvePlayerInventory();
        if (playerInventory != null) RefreshSelectedSlot();
    }

    private void Start()
    {
        ResolvePlayerInventory();

        if (playerInventory == null)
        {
            Debug.LogError(
                "[Inventory UI] PlayerInventory could not be found."
            );

            return;
        }

        RefreshUI();
    }

    private void ResolvePlayerInventory()
    {
        if (playerInventory != null)
        {
            return;
        }

        playerInventory =
            FindFirstObjectByType<PlayerInventory>();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void RefreshUI()
    {
        ResolvePlayerInventory();

        if (playerInventory == null)
        {
            return;
        }

        Debug.Log("[InventoryUI] RefreshUI() started.");

        RefreshToolbar();

        RefreshSelectedSlot();

        RefreshMainInventory();

        Debug.Log("[InventoryUI] RefreshUI() finished.");
    }


    // =========================================================
    // REFRESH TOOLBAR
    // =========================================================

    private void RefreshToolbar()
    {
        Debug.Log(
            $"[InventoryUI] RefreshToolbar() | " +
            $"toolbarItems.Count = {toolbarItems.Count} | " +
            $"inventory toolbar size = {playerInventory.GetToolbarSize()}"
        );

        int toolbarSize = playerInventory.GetToolbarSize();

        for (int i = 0; i < toolbarItems.Count; i++)
        {
            InventoryItem uiItem = toolbarItems[i];

            if (uiItem == null)
            {
                Debug.LogWarning(
                    $"[InventoryUI] toolbarItems[{i}] is NULL!"
                );
                continue;
            }

            InventorySlot slot =
                playerInventory.GetToolbarSlot(i);

            Debug.Log(
                $"[InventoryUI] Toolbar {i} | " +
                $"UI Object = {uiItem.gameObject.name} | " +
                $"Slot Empty = {(slot == null || slot.IsEmpty())}"
            );

            if (slot == null || slot.IsEmpty())
            {
                uiItem.ClearItem();
            }
            else
            {
                Debug.Log(
                    $"[InventoryUI] Putting {slot.item.itemName} " +
                    $"into toolbar UI slot {i}"
                );

                uiItem.SetItem(slot.item);

                Debug.Log(
              $"[InventoryUI] Toolbar {i} | " +
              $"UI Object = {uiItem.gameObject.name} | " +
              $"Slot Empty = {(slot == null || slot.IsEmpty())}"
          );
            }
        }
    }


    private void RefreshSelectedSlot()
    {
        int selectedSlot =
            playerInventory.GetSelectedSlotIndex();


        for (int i = 0;
            i < toolbarItems.Count;
            i++)
        {
            InventoryItem item =
                toolbarItems[i];


            if (item == null)
            {
                continue;
            }


            InventorySlotUI slot =
                item.GetComponentInParent<InventorySlotUI>();


            if (slot == null)
            {
                continue;
            }


            slot.SetActive(
                i == selectedSlot
            );
        }
    }



    // =========================================================
    // REFRESH MAIN INVENTORY
    // =========================================================

    private void RefreshMainInventory()
    {
        int mainSize =
            playerInventory.GetMainInventorySize();


        for (int i = 0;
            i < mainInventoryItems.Count;
            i++)
        {
            InventoryItem uiItem =
                mainInventoryItems[i];


            if (uiItem == null)
            {
                continue;
            }


            if (i >= mainSize)
            {
                uiItem.ClearItem();
                continue;
            }


            InventorySlot slot =
                playerInventory.GetMainInventorySlot(i);


            if (slot == null ||
                slot.IsEmpty())
            {
                uiItem.ClearItem();
            }
            else
            {
                Debug.Log(
        $"[InventoryUI DEBUG] Toolbar slot {i} " +
        $"contains: {slot.item.name}"
    );

                Debug.Log(
                    $"[InventoryUI DEBUG] Sending {slot.item.name} " +
                    $"to UI object: {uiItem.gameObject.name}"
                );

                uiItem.SetItem(slot.item);

                Debug.Log(
                    $"[InventoryUI DEBUG] SetItem() finished for " +
                    $"{uiItem.gameObject.name}"
                );
            }
        }
    }

    public void HandleItemDrop(
    InventoryItem draggedItem,
    InventorySlotUI targetSlot)
    {
        if (draggedItem == null ||
            targetSlot == null)
        {
            return;
        }

        int sourceIndex =
            GetInventoryIndex(draggedItem);

        int targetIndex =
            GetInventoryIndex(targetSlot);

        Debug.Log(
            $"[InventoryUI] Source: {sourceIndex} | " +
            $"Target: {targetIndex}"
        );

        if (sourceIndex < 0 ||
            targetIndex < 0)
        {
            Debug.LogError(
                "[InventoryUI] Could not find inventory indices."
            );

            return;
        }

        if (sourceIndex == targetIndex)
        {
            return;
        }

        playerInventory.SwapSlots(
            sourceIndex,
            targetIndex
        );

        RefreshUI();
    }

    private int GetInventoryIndex(
    InventoryItem uiItem)
    {
        int toolbarIndex =
            toolbarItems.IndexOf(uiItem);

        if (toolbarIndex >= 0)
        {
            return toolbarIndex;
        }

        int mainInventoryIndex =
            mainInventoryItems.IndexOf(uiItem);

        if (mainInventoryIndex >= 0)
        {
            return playerInventory.GetToolbarSize()
                + mainInventoryIndex;
        }

        return -1;
    }


    private int GetInventoryIndex(
    InventorySlotUI targetSlot)
    {
        for (int i = 0;
            i < toolbarItems.Count;
            i++)
        {
            if (toolbarItems[i] == null)
                continue;

            InventorySlotUI slot =
                toolbarItems[i]
                    .GetComponentInParent<InventorySlotUI>();

            if (slot == targetSlot)
            {
                return i;
            }
        }

        for (int i = 0;
            i < mainInventoryItems.Count;
            i++)
        {
            if (mainInventoryItems[i] == null)
                continue;

            InventorySlotUI slot =
                mainInventoryItems[i]
                    .GetComponentInParent<InventorySlotUI>();

            if (slot == targetSlot)
            {
                return playerInventory.GetToolbarSize()
                    + i;
            }
        }

        return -1;
    }
}