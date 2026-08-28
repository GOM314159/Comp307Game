using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public ItemData item;

    [Header("UI")]
    public Image image;

    [HideInInspector] public Transform parentAfterDrag;


    private void Start()
    {
        if (item != null)
        {
            InitialiseItem(item);
        }
        else
        {
            ClearItem();
        }
    }

    public void InitialiseItem(ItemData newItem)
    {
        item = newItem;

        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (image == null)
        {
            Debug.LogError(
                "[InventoryItem] No Image component found."
            );

            return;
        }

        if (item == null)
        {
            ClearItem();
            return;
        }

        image.sprite = item.icon;
        image.enabled = item.icon != null;
    }
    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
    }

    public void SetItem(ItemData newItem)
    {
        item = newItem;

        if (image == null)
            image = GetComponent<Image>();

        Debug.Log(
            $"[InventoryItem] SetItem called on {gameObject.name} | " +
            $"Item: {(newItem != null ? newItem.name : "NULL")} | " +
            $"Icon: {(newItem != null && newItem.icon != null ? newItem.icon.name : "NULL")}"
        );

        if (image == null)
        {
            Debug.LogError(
                $"[InventoryItem] {gameObject.name} has NO Image component!"
            );
            return;
        }

        if (newItem == null)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = newItem.icon;
        image.enabled = newItem.icon != null;

        Debug.Log(
            $"[InventoryItem] AFTER SET | " +
            $"UI Object: {gameObject.name} | " +
            $"Image: {image.name} | " +
            $"Sprite: {(image.sprite != null ? image.sprite.name : "NULL")} | " +
            $"Enabled: {image.enabled}"
        );
}

    public void ClearItem()
    {
        item = null;

        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (image == null || image.sprite == null)
            return;
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (image == null || image.sprite == null)
            return;
      
        transform.position = eventData.position;
    }

    public void OnEndDrag(
    PointerEventData eventData)
{
    if (parentAfterDrag != null)
    {
            transform.SetParent(
                parentAfterDrag
            );
         

        transform.localPosition =
            Vector3.zero;
    }

    if (image != null)
    {
        image.raycastTarget = true;
    }
       
}
}
