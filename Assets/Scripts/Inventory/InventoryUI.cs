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

    private void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError(
                "[Inventory UI] PlayerInventory has not been assigned."
            );

            return;
        }

        RefreshUI();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void RefreshUI()
    {
        if (playerInventory == null)
        {
            return;
        }

        Debug.Log("[InventoryUI] RefreshUI() started.");
        RefreshToolbar();

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
            }
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
    InventoryItem targetItem)
    {
        if (draggedItem == null ||
            targetItem == null)
        {
            return;
        }

        int sourceIndex =
            GetInventoryIndex(
                draggedItem
            );

        int targetIndex =
            GetInventoryIndex(
                targetItem
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

        Debug.Log(
            $"[InventoryUI] Moving item " +
            $"from {sourceIndex} to {targetIndex}"
        );

        playerInventory.SwapSlots(
            sourceIndex,
            targetIndex
        );
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
}