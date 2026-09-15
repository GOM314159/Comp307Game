using UnityEngine;

[CreateAssetMenu(
    fileName = "New Exodia Piece",
    menuName = "Inventory/Items/Interactable/Exodia Piece"
)]
public class ExodiaPieceItem : InteractableItem
{
    // =========================================================
    // EXODIA PIECE TYPE
    // =========================================================

    public enum PieceType
    {
        RightLeg,
        LeftLeg,
        RightArm,
        LeftArm,
        Head
    }


    // =========================================================
    // PIECE
    // =========================================================

    [Header("Exodia Piece")]

    [SerializeField]
    private PieceType pieceType;


    // =========================================================
    // INITIALIZE
    // =========================================================

    protected override void OnEnable()
    {
        base.OnEnable();

        itemType = ItemType.Interactable;
        stackable = false;
        maxStackSize = 1;
    }


    // =========================================================
    // GET PIECE TYPE
    // =========================================================

    public PieceType GetPieceType()
    {
        return pieceType;
    }


    // =========================================================
    // USE
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Exodia] "
            + itemName
            + " does not currently have a use."
        );
    }
}