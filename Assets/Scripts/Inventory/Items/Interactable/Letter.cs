using UnityEngine;

[CreateAssetMenu(
    fileName = "New Letter",
    menuName = "Inventory/Items/Interactable/Letter"
)]
public class Letter : ItemData
{
    // =========================================================
    // LETTER IMAGES
    // =========================================================

    [Header("Letter Images")]

    [Tooltip("The full letter shown when the player is male.")]
    [SerializeField]
    private Sprite nephewLetterImage;

    [Tooltip("The full letter shown when the player is female.")]
    [SerializeField]
    private Sprite nieceLetterImage;


    // =========================================================
    // SETUP
    // =========================================================

    protected virtual void OnEnable()
    {
        itemType = ItemType.Interactable;

        stackable = false;
        maxStackSize = 1;
    }


    // =========================================================
    // USE LETTER
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

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Letter] GameStateManager could not be found."
            );

            return;
        }

        GameStateManager.PlayerGender gender =
            GameStateManager.Instance.GetPlayerGender();

        Sprite selectedLetter = null;

        if (gender ==
            GameStateManager.PlayerGender.Male)
        {
            selectedLetter = nephewLetterImage;
        }
        else if (gender ==
                 GameStateManager.PlayerGender.Female)
        {
            selectedLetter = nieceLetterImage;
        }

        if (selectedLetter == null)
        {
            Debug.LogError(
                "[Letter] No letter image has been assigned "
                + "for the selected gender."
            );

            return;
        }

        LetterUIManager.Instance.OpenLetter(
            this,
            selectedLetter
        );
    }


    // =========================================================
    // GET CORRECT LETTER IMAGE
    // =========================================================

    public Sprite GetLetterImage()
    {
        if (GameStateManager.Instance == null)
        {
            return null;
        }

        GameStateManager.PlayerGender gender =
            GameStateManager.Instance.GetPlayerGender();

        if (gender ==
            GameStateManager.PlayerGender.Male)
        {
            return nephewLetterImage;
        }

        if (gender ==
            GameStateManager.PlayerGender.Female)
        {
            return nieceLetterImage;
        }

        return null;
    }
}