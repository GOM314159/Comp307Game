using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public abstract class ItemData : ScriptableObject , IBeginDragHandler , IDragHandler , IEndDragHandler
{
   
    public enum ItemType
    {
        Weapon,
        Consumable,
        Interactable
    }

    [Header("Item Information")]

    // Name displayed in the inventory
    public string itemName;

    // Description displayed in the inventory
    [TextArea(2, 5)]
    public string description;

    // Icon displayed in the inventory
    public Sprite icon;

    // Determines how the item behaves
    public ItemType itemType;

    public Image Image;


    [Header("Attack Settings")]

    // Base damage dealt by attacking with this item.
    public float baseDamage = 1f;



    [Header("Inventory Settings")]

    // Can multiple copies of this item occupy one slot?
    public bool stackable = true;

    // Maximum number of this item in one slot.
    public int maxStackSize = 64; // iykyk

    // RIGHT CLICK BEHAVIOUR
    // This is intentionally virtual. Please don't change
    // Different item types can override this behaviour. This makes the system expandable later.
    public virtual void Use()
    {
        Debug.Log(
            "[Inventory] Used item: "
            + itemName);
    }


    // ATTACK BEHAVIOUR

    // Every item can be attacked with. By default this deals the item's base damage.
    public virtual float GetAttackDamage()
    {
        return baseDamage;
    }


    // INVENTORY DRAG AND DROP
    public void OnEndDrag(PointerEventData eventData)
    {
        Image.raycastTarget = true;
        
    }

    public void OnDrag(PointerEventData eventData)
    {
       // transform.position = Input.mousePosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Image.raycastTarget = false;
       // parentAfterDrag = transform.parent;
       // transform.SetParent(Transform.root);
    }
}