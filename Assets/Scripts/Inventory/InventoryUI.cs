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


        RefreshToolbar();

        RefreshMainInventory();
    }


    // =========================================================
    // REFRESH TOOLBAR
    // =========================================================

    private void RefreshToolbar()
    {
        int toolbarSize =
            playerInventory.GetToolbarSize();


        for (int i = 0;
            i < toolbarItems.Count;
            i++)
        {
            InventoryItem uiItem =
                toolbarItems[i];


            if (uiItem == null)
            {
                continue;
            }


            if (i >= toolbarSize)
            {
                uiItem.ClearItem();
                continue;
            }


            InventorySlot slot =
                playerInventory.GetToolbarSlot(i);


            if (slot == null ||
                slot.IsEmpty())
            {
                uiItem.ClearItem();
            }
            else
            {
                uiItem.SetItem(
                    slot.item
                );
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
                uiItem.SetItem(
                    slot.item
                );
            }
        }
    }
}