using System;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot(
        ItemData item = null,
        int quantity = 0)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public bool IsEmpty()
    {
        return item == null || quantity <= 0;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}