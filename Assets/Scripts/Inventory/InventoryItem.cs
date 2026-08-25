using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private Image image;

    [HideInInspector] public Transform parentAfterDrag;

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

        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root, true);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
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

        if (parentAfterDrag != null)
            transform.SetParent(parentAfterDrag, false);
    }
}
