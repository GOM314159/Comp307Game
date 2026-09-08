using UnityEngine;

[CreateAssetMenu(
    fileName = "New Letter",
    menuName = "Inventory/Items/Interactable/Letter"
)]
public class Letter : ItemData
{
    // =========================================================
    // LETTER CONTENT
    // =========================================================

    [Header("Letter Content")]

    [Tooltip("The text that will appear when the player reads this letter.")]
    [TextArea(10, 30)]
    [SerializeField]
    private string letterText;


    // =========================================================
    // SETUP
    // =========================================================

    protected virtual void OnEnable()
    {
        itemType = ItemType.Interactable;
    }


    // =========================================================
    // RIGHT CLICK / USE
    // =========================================================

    public override void Use()
    {
        if (LetterUIManager.Instance == null)
        {
            Debug.LogError(
                "[Letter] LetterUIManager could not be found."
            );

            return;
        }

        LetterUIManager.Instance.OpenLetter(
            this,
            letterText
        );
    }


    // =========================================================
    // GET LETTER TEXT
    // =========================================================

    public string GetLetterText()
    {
        return letterText;
    }
}