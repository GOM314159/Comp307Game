using UnityEngine;
using UnityEngine.UI;

public class InventoryCloseButton : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerInventory playerInventory;

    [SerializeField]
    private Button closeButton;


    private void Awake()
    {
        if (closeButton == null)
        {
            closeButton = GetComponent<Button>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
    }


    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseInventory);
        }
    }


    public void CloseInventory()
    {
        if (playerInventory == null)
        {
            Debug.LogError(
                "[Inventory Close] PlayerInventory "
                + "has not been assigned."
            );

            return;
        }

        playerInventory.CloseInventory();
    }
}