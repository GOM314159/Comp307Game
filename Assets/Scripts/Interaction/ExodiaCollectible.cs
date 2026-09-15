using UnityEngine;

public class ExodiaCollectible : WorldInteractable
{
    // =========================================================
    // EXODIA
    // =========================================================

    [Header("Exodia Collectible")]

    [SerializeField]
    private ExodiaPieceItem exodiaPiece;


    [SerializeField]
    private PlayerInventory playerInventory;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        UpdateWorldState();
    }


    // =========================================================
    // INTERACT
    // =========================================================

    public override void Interact()
    {
        if (!CanInteract())
        {
            return;
        }


        if (exodiaPiece == null)
        {
            Debug.LogError(
                "[Exodia] Exodia Piece is missing on "
                + gameObject.name
            );

            return;
        }


        if (playerInventory == null)
        {
            Debug.LogError(
                "[Exodia] Player Inventory is missing on "
                + gameObject.name
            );

            return;
        }


        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Exodia] GameStateManager is missing."
            );

            return;
        }


        GameStateManager.ExodiaPiece piece =
            ConvertPieceType(
                exodiaPiece.GetPieceType()
            );


        // -----------------------------------------------------
        // ALREADY COLLECTED
        // -----------------------------------------------------

        if (
            GameStateManager.Instance
                .HasCollectedExodiaPiece(piece)
        )
        {
            gameObject.SetActive(false);

            return;
        }


        // -----------------------------------------------------
        // ADD TO INVENTORY
        // -----------------------------------------------------

        bool added =
            playerInventory.AddItem(
                exodiaPiece,
                1
            );


        // -----------------------------------------------------
        // INVENTORY FULL
        // -----------------------------------------------------

        if (!added)
        {
            Debug.Log(
                "[Exodia] Inventory full. "
                + "Cannot collect "
                + exodiaPiece.itemName
            );

            return;
        }


        // -----------------------------------------------------
        // REGISTER
        // -----------------------------------------------------

        GameStateManager.Instance
            .RegisterExodiaPiece(piece);


        // -----------------------------------------------------
        // BASE INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // -----------------------------------------------------
        // REMOVE WORLD OBJECT
        // -----------------------------------------------------

        gameObject.SetActive(false);


        Debug.Log(
            "[Exodia] Picked up "
            + exodiaPiece.itemName
        );
    }


    // =========================================================
    // WORLD RESET
    // =========================================================

    protected override void OnResetState()
    {
        UpdateWorldState();
    }


    // =========================================================
    // UPDATE WORLD STATE
    // =========================================================

    private void UpdateWorldState()
    {
        if (exodiaPiece == null)
        {
            return;
        }


        if (GameStateManager.Instance == null)
        {
            return;
        }


        GameStateManager.ExodiaPiece piece =
            ConvertPieceType(
                exodiaPiece.GetPieceType()
            );


        bool collected =
            GameStateManager.Instance
                .HasCollectedExodiaPiece(piece);


        if (collected)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }


    // =========================================================
    // CONVERT PIECE TYPE
    // =========================================================

    private GameStateManager.ExodiaPiece ConvertPieceType(
        ExodiaPieceItem.PieceType piece
    )
    {
        switch (piece)
        {
            case ExodiaPieceItem.PieceType.RightLeg:
                return GameStateManager.ExodiaPiece.RightLeg;

            case ExodiaPieceItem.PieceType.LeftLeg:
                return GameStateManager.ExodiaPiece.LeftLeg;

            case ExodiaPieceItem.PieceType.RightArm:
                return GameStateManager.ExodiaPiece.RightArm;

            case ExodiaPieceItem.PieceType.LeftArm:
                return GameStateManager.ExodiaPiece.LeftArm;

            case ExodiaPieceItem.PieceType.Head:
                return GameStateManager.ExodiaPiece.Head;
        }


        return GameStateManager.ExodiaPiece.RightLeg;
    }


    // =========================================================
    // INTERACTION PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        if (exodiaPiece == null)
        {
            return "Pick Up Exodia Piece";
        }


        return "Pick Up "
            + exodiaPiece.itemName;
    }
}