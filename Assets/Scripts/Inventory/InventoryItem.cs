using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public ItemData item;

    [Header("UI")]
    public Image image;

    [HideInInspector] public Transform parentAfterDrag;


    private void Start()
    {
        InitialiseItem(item);
    }

    public void InitialiseItem(ItemData item)
    {
        image.sprite = item.icon;
    }
    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
    }

    public void SetItem(ItemData item)
    {
        if (image == null)
            image = GetComponent<Image>();

        if (image == null)
            return;

        image.sprite = item != null ? item.icon : null;
        image.enabled = item != null && item.icon != null;
    }

    public void ClearItem()
    {
        SetItem(null);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (image == null || image.sprite == null)
            return;
        image.raycastTarget = false;
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
      //  transform.SetAsLastSibling();
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (image == null || image.sprite == null)
            return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (image == null)
            return;

        image.raycastTarget = true;

       // if (parentAfterDrag != null)
            transform.SetParent(parentAfterDrag);
    }
}
